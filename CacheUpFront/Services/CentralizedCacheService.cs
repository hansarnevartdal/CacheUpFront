using CacheUpFront.Configuration;
using CacheUpFront.Models;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheUpFront.Services
{
    public class CentralizedCacheService<TEntity> : CacheServiceBase<TEntity>, ICentralizedCacheService<TEntity> where TEntity : Entity
    {
        private readonly IDatabase _db;
        private readonly ILogger _logger;

        public CentralizedCacheService(IConnectionMultiplexer connectionMultiplexer, IRedisConfiguration redisConfiguration, ILogger logger) : base(redisConfiguration)
        {
            _db = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task AddOrUpdate(TEntity entity)
        {
            if (entity?.Id == null)
            {
                throw new ArgumentNullException(nameof(entity), "entity or entity.Id cannot be null when adding to cache.");
            }

            var success = await _db.StringSetAsync(GetKey(entity.Id), SerializeEntity(entity)).ConfigureAwait(false);
            if (success)
            {
                _logger.Information($"Added {EntityName} with id {entity.Id} to redis cache.");

                var trackingAdded = await _db.SetAddAsync(TrackingSetKey, entity.Id).ConfigureAwait(false);
                if (trackingAdded)
                {
                    _logger.Information($"Added tracking of {EntityName} with id {entity.Id} in {TrackingSetKey}.");
                }
                else
                {
                    _logger.Information($"Already tracking {EntityName} with id {entity.Id} in {TrackingSetKey}.");
                }
            }
            else
            {
                var message = $"Failed to add {EntityName} with id {entity.Id} to redis cache.";
                _logger.Error(message);
                throw new Exception(message);
            }
        }

        public async Task AddOrUpdate(IList<TEntity> entities)
        {
            entities = entities.Where(e => !string.IsNullOrEmpty(e?.Id)).ToList();
            if (!entities.Any())
            {
                throw new ArgumentNullException(nameof(entities), "No entities, or no entities with entity.Id set. Unable to add batch to cache.");
            }

            var keyValuePairs = entities
                .Select(e => new KeyValuePair<RedisKey, RedisValue>(GetKey(e.Id), SerializeEntity(e)))
                .ToArray();

            var success = await _db.StringSetAsync(keyValuePairs).ConfigureAwait(false);
            if (success)
            {
                _logger.Information($"Added batch with {keyValuePairs.Length} new {EntityName}s to redis cache.");

                var numberOfmemebersAdded = await _db.SetAddAsync(TrackingSetKey, entities.Select(e => (RedisValue)e.Id).ToArray()).ConfigureAwait(false);
                if (numberOfmemebersAdded > 0)
                {
                    _logger.Information($"Added tracking of batch with {numberOfmemebersAdded} new {EntityName}s in {TrackingSetKey}.");
                }
            }
            else
            {
                var message = $"Failed to add batch of {EntityName} with to redis cache.";
                _logger.Error(message);
                throw new Exception(message);
            }
        }

        public async Task Remove(string id)
        {
            var success = await _db.KeyDeleteAsync(GetKey(id)).ConfigureAwait(false);
            if (success)
            {
                _logger.Information($"Removed {EntityName} with id {id} from redis cache.");

                var trackingRemoved = await _db.SetRemoveAsync(TrackingSetKey, id).ConfigureAwait(false);
                if (trackingRemoved)
                {
                    _logger.Information($"Removed tracking for {EntityName} with id {id}.");
                }
                else
                {
                    _logger.Information($"No tracking existed for {EntityName} with id {id}.");
                }
            }
            else
            {
                _logger.Information($"Failed to remove {EntityName} with id {id} from redis cache.");
            }
        }
    }
}
