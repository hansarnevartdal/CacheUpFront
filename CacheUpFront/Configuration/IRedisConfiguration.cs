using System.Collections.Generic;

namespace CacheUpFront.Configuration
{
    public interface IRedisConfiguration
    {
        IList<string> Endpoints { get; set; }
        bool PreserveAsyncOrder { get; set; }
        bool AbortOnConnectFail { get; set; }
        int InitialLoadBatchSize { get; set; }
        string CacheKeyPrefix { get; set; }
    }
}
