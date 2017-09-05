using Autofac;
using CacheUpFront.Configuration;
using CacheUpFront.Services;
using StackExchange.Redis;

namespace CacheUpFront.Autofac
{
    public static class ContainerBuilderExtensions
    { 

        public static void RegisterEntityCacheDataConsumer(this ContainerBuilder containerBuilder, IRedisConfiguration redisConfiguration)
        {
            containerBuilder.RegisterGeneric(typeof(EntityCache<>)).As(typeof(IEntityCache<>)).SingleInstance();
            containerBuilder.RegisterGeneric(typeof(LocalCacheService<>)).As(typeof(ILocalCacheService<>)).SingleInstance();
            containerBuilder.RegisterEntityCacheConfiguration(redisConfiguration);
        }

        public static void RegisterEntityCacheDataProvider(this ContainerBuilder containerBuilder, IRedisConfiguration redisConfiguration)
        {
            containerBuilder.RegisterGeneric(typeof(CentralizedCacheService<>)).As(typeof(ICentralizedCacheService<>)).SingleInstance();
            containerBuilder.RegisterEntityCacheConfiguration(redisConfiguration);
        }

        private static void RegisterEntityCacheConfiguration(this ContainerBuilder containerBuilder, IRedisConfiguration redisConfiguration)
        {
            containerBuilder.RegisterType<RedisConfiguration>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.Register(c =>
            {
                var configurationSettings = c.Resolve<IRedisConfiguration>();
                var redisOptions = new ConfigurationOptions
                {
                    AllowAdmin = false,
                    Ssl = false,
                    AbortOnConnectFail = configurationSettings.AbortOnConnectFail
                };
                foreach (var endpoint in configurationSettings.Endpoints)
                {
                    redisOptions.EndPoints.Add(endpoint);
                }

                var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
                multiplexer.PreserveAsyncOrder = configurationSettings.PreserveAsyncOrder;
                return multiplexer;
            }).As<IConnectionMultiplexer>().SingleInstance();
        }
    }
}
