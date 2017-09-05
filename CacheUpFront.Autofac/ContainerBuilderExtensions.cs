using Autofac;
using CacheUpFront.Configuration;
using CacheUpFront.Services;
using Serilog;
using StackExchange.Redis;

namespace CacheUpFront.Autofac
{
    public static class ContainerBuilderExtensions
    { 

        public static void RegisterEntityCacheDataConsumer(this ContainerBuilder containerBuilder, IEntityCacheOptions entityCacheOptions)
        {
            containerBuilder.RegisterGeneric(typeof(EntityCache<>)).As(typeof(IEntityCache<>)).SingleInstance();
            containerBuilder.RegisterGeneric(typeof(LocalCacheService<>)).As(typeof(ILocalCacheService<>)).SingleInstance();
            containerBuilder.RegisterEntityCacheConfiguration(entityCacheOptions);
        }

        public static void RegisterEntityCacheDataProvider(this ContainerBuilder containerBuilder, IEntityCacheOptions entityCacheOptions)
        {
            containerBuilder.RegisterGeneric(typeof(CentralizedCacheService<>)).As(typeof(ICentralizedCacheService<>)).SingleInstance();
            containerBuilder.RegisterEntityCacheConfiguration(entityCacheOptions);
        }

        private static void RegisterEntityCacheConfiguration(this ContainerBuilder containerBuilder, IEntityCacheOptions entityCacheOptions)
        {
            containerBuilder.Register<ILogger>((c, p) =>
            {
                return new LoggerConfiguration().CreateLogger();
            }).SingleInstance();
            containerBuilder.RegisterInstance(entityCacheOptions).AsImplementedInterfaces().SingleInstance();
            containerBuilder.Register(c =>
            {
                var redisOptions = new ConfigurationOptions
                {
                    AllowAdmin = false,
                    Ssl = false,
                    AbortOnConnectFail = entityCacheOptions.AbortOnConnectFail
                };
                foreach (var endpoint in entityCacheOptions.Endpoints)
                {
                    redisOptions.EndPoints.Add(endpoint);
                }

                var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
                multiplexer.PreserveAsyncOrder = entityCacheOptions.PreserveAsyncOrder;
                return multiplexer;
            }).As<IConnectionMultiplexer>().SingleInstance();
        }
    }
}
