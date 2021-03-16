using Microsoft.Extensions.DependencyInjection;
using Smidge.Cache;
using Smidge.Options;

namespace Smidge.InMemory
{
    public static class SmidgeInMemoryStartup
    {
        /// <summary>
        /// Adds the ability to have in-memory caches for Smidge
        /// </summary>
        /// <param name="services"></param>
        /// <param name="enable">The default is true which will ensure that <see cref="SmidgeCacheOptions.UseInMemoryCache"/> is configured to true</param>
        /// <returns></returns>
        public static IServiceCollection AddSmidgeInMemory(this IServiceCollection services, bool enable = true)
        {
            services.AddTransient<ICacheFileSystem, ConfiguredCacheFileSystem>();
            if (enable)
            {
                services.Configure<SmidgeOptions>(o =>
                {
                    o.CacheOptions.UseInMemoryCache = true;
                });
            }
            return services;
        }
    }
}
