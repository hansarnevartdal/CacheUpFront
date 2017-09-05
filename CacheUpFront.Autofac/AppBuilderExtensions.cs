using Autofac;
using CacheUpFront.Models;
using CacheUpFront.Services;
using Microsoft.AspNetCore.Builder;

namespace CacheUpFront.Autofac
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseEntityCache<TEntity>(this IApplicationBuilder app, IContainer container) where TEntity : Entity
        {

            var localCacheService = container.Resolve<ILocalCacheService<TEntity>>();
            localCacheService.LoadAndSubscribe().GetAwaiter().GetResult();
            return app;
        }
    }
}
