using System.Collections.Generic;

namespace CacheUpFront.Configuration
{
    public class RedisConfiguration : IRedisConfiguration
    {
        public IList<string> Endpoints { get; set; }
        public bool PreserveAsyncOrder { get; set; } = true;
        public bool AbortOnConnectFail { get; set; } = false;
        public int InitialLoadBatchSize { get; set; } = 100;
        public string CacheKeyPrefix { get; set; }
    }
}
