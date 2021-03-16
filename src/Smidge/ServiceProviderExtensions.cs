using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Smidge.Cache;
using Smidge.Hashing;
using System;
using System.IO;

namespace Smidge
{
    public static class ServiceProviderExtensions
    {
        public static PhysicalFileCacheFileSystem CreatePhysicalFileCacheFileSystem(this IServiceProvider services)
        {
            var hasher = services.GetRequiredService<IHasher>();
#if NETCORE3_0
            var hosting = services.GetRequiredService<IWebHostEnvironment>();
#else
            var hosting = services.GetRequiredService<IHostingEnvironment>();
#endif

            var config = services.GetRequiredService<ISmidgeConfig>();
            var cacheFolder = Path.Combine(hosting.ContentRootPath, config.DataFolder, "Cache", Environment.MachineName.ReplaceNonAlphanumericChars('-'));

            //ensure it exists
            Directory.CreateDirectory(cacheFolder);

            var cacheFileProvider = new PhysicalFileProvider(cacheFolder);

            return new PhysicalFileCacheFileSystem(cacheFileProvider, hasher);
        }
    }
}