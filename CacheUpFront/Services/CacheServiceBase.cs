using CacheUpFront.Configuration;
using CacheUpFront.Models;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Text;

namespace CacheUpFront.Services
{
    public abstract class CacheServiceBase<TEntity> where TEntity : Entity
    {
        protected readonly string KeyPrefix;
        protected readonly string EntityName;
        protected readonly string TrackingSetKey;

        protected CacheServiceBase(IRedisConfiguration redisConfiguration)
        {
            // Set naming
            var componentPrefix = typeof(TEntity).Assembly.GetName().Name.ToLowerInvariant().Replace('.', ':');
            var cachePrefix = string.IsNullOrEmpty(redisConfiguration.CacheKeyPrefix) ? componentPrefix : redisConfiguration.CacheKeyPrefix + ":" + componentPrefix;
            EntityName = typeof(TEntity).Name;

            KeyPrefix = $"{cachePrefix}:Entity:{EntityName}";
            TrackingSetKey = $"{cachePrefix}:Tracker:{EntityName}";
        }

        protected RedisKey GetKey(string id)
        {
            return $"{KeyPrefix}:{id}";
        }

        protected string GetIdFromKey(RedisKey key)
        {
            return GetIdFromString(key.ToString());
        }

        protected string GetIdFromChannel(RedisChannel channel)
        {
            return GetIdFromString(channel.ToString());
        }

        private string GetIdFromString(string redisString)
        {
            return redisString.Substring(redisString.LastIndexOf(":", StringComparison.InvariantCultureIgnoreCase) + 1);
        }

        protected RedisValue SerializeEntity(TEntity entity)
        {
            var jsonString = JsonConvert.SerializeObject(entity);
            return Encoding.UTF8.GetBytes(jsonString);
        }
    }
}
