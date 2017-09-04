using CacheUpFront.Configuration;
using CacheUpFront.Models;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheUpFront.Services
{
    public class LocalCacheService<TEntity> : CacheServiceBase<TEntity>, ILocalCacheService<TEntity> where TEntity : Entity
    {
        protected readonly IConnectionMultiplexer ConnectionMultiplexer;
        protected readonly IDatabase Db;
        protected readonly RedisChannel SubscribtionChannel;

        private readonly IEntityCache<TEntity> _entityCache;
        private readonly ILogger _logger;
        private readonly IRedisConfiguration _redisConfiguration;

        public LocalCacheService(IConnectionMultiplexer connectionMultiplexer, IEntityCache<TEntity> entityCache, ILogger logger, IRedisConfiguration redisConfiguration) : base(redisConfiguration)
        {
            _entityCache = entityCache;
            _logger = logger;
            _redisConfiguration = redisConfiguration;

            // Configure redis things
            ConnectionMultiplexer = connectionMultiplexer;
            Db = connectionMultiplexer.GetDatabase();
            SubscribtionChannel = new RedisChannel($"__keyspace@0__:{KeyPrefix}:*", RedisChannel.PatternMode.Pattern);
        }

        public async Task LoadAndSubscribe()
        {
            await Load().ConfigureAwait(false);
            await Subscribe().ConfigureAwait(false);
        }

        protected async Task Load()
        {
            var entityIds = await GetAllKeys().ConfigureAwait(false);

            if (entityIds == null || !entityIds.Any())
            {
                return;
            }
            var batchNumber = 0;
            var batchSize = _redisConfiguration.InitialLoadBatchSize;

            while (batchNumber * batchSize <= entityIds.Count)
            {
                var batchIds = entityIds.Skip(batchNumber * batchSize).Take(batchSize).ToList();
                Get(batchIds)
                    .AsParallel()
                    .ForAll(OnEntityLoaded);

                batchNumber++;
            }

            _logger.Information($"Loaded {entityIds.Count} {EntityName} into local cache.");
        }

        protected virtual void OnEntityLoaded(TEntity entity)
        {
            _entityCache.AddOrUpdate(entity);
        }

        protected async Task<List<RedisValue>> GetAllKeys()
        {
            var keys = await Db.SetMembersAsync(TrackingSetKey).ConfigureAwait(false);
            return keys.ToList();
        }

        private async Task Subscribe()
        {
            var subscriber = ConnectionMultiplexer.GetSubscriber();
            await subscriber.SubscribeAsync(SubscribtionChannel, (channel, value) =>
            {
                switch (value)
                {
                    case "set":
                        var entity = Get(GetIdFromChannel(channel));
                        OnEntityAddedOrUpdated(entity);
                        _logger.Information($"Handled set event for {channel}.");
                        break;
                    case "del":
                        OnEntityRemoved(GetIdFromChannel(channel));
                        _logger.Information($"Handled delete event for {channel}.");
                        break;
                }
            }).ConfigureAwait(false);
            _logger.Information($"Subscribed to redis cache events with pattern {SubscribtionChannel}.");
        }

        protected virtual void OnEntityAddedOrUpdated(TEntity entity)
        {
            _entityCache.AddOrUpdate(entity);
        }

        protected virtual void OnEntityRemoved(string entityId)
        {
            _entityCache.Remove(entityId);
        }

        protected TEntity Get(string id)
        {
            var redisKey = GetKey(id);
            var serializedObject = Db.StringGet(redisKey);

            if (!serializedObject.HasValue)
            {
                _logger.Warning($"Did not find document with key: {redisKey}");
                return default(TEntity);
            }

            var jsonString = Encoding.UTF8.GetString(serializedObject);
            return JsonConvert.DeserializeObject<TEntity>(jsonString);
        }

        protected IList<TEntity> Get(IEnumerable<RedisValue> ids)
        {
            var redisKeys = ids.Select(id => (RedisKey)GetKey(id)).ToArray();
            var serializedObjects = Db.StringGet(redisKeys);
            var entities = new List<TEntity>();
            foreach (var serializedObject in serializedObjects)
            {
                if (serializedObject == "nil") continue;

                var jsonString = Encoding.UTF8.GetString(serializedObject);
                entities.Add(JsonConvert.DeserializeObject<TEntity>(jsonString));
            }

            return entities;
        }
    }
}
