using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.Hashing;
using Smidge.Models;
using Smidge.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Smidge.InMemory
{

    /// <summary>
    /// Uses either an <see cref="MemoryCacheFileSystem"/> or a <see cref="PhysicalFileCacheFileSystem"/> depending on what is
    /// configured in <see cref="SmidgeCacheOptions"/>
    /// </summary>
    public class ConfiguredCacheFileSystem : ICacheFileSystem
    {
        private readonly SmidgeOptions _options;
        private readonly ICacheFileSystem _wrapped;

        public ConfiguredCacheFileSystem(IOptions<SmidgeOptions> options, IServiceProvider services)
        {
            _options = options.Value;

            var hasher = services.GetRequiredService<IHasher>();

            if (_options.CacheOptions.UseInMemoryCache)
            {
                var logger = services.GetRequiredService<ILogger<MemoryCacheFileSystem>>();

                _wrapped = new MemoryCacheFileSystem(hasher, logger);
            }
            else
            {
                var logger = services.GetRequiredService<ILogger<PhysicalFileCacheFileSystem>>();

                _wrapped = PhysicalFileCacheFileSystem.CreatePhysicalFileCacheFileSystem(
                    hasher,
                    services.GetRequiredService<ISmidgeConfig>(),
                    services.GetRequiredService<IHostEnvironment>(),
                    logger);
            }
        }

        public Task ClearCachedCompositeFileAsync(string cacheBusterValue, CompressionType type, string filesetKey)
            => _wrapped.ClearCachedCompositeFileAsync(cacheBusterValue, type, filesetKey);

        public IFileInfo GetCachedCompositeFile(string cacheBusterValue, CompressionType type, string filesetKey, out string filePath)
            => _wrapped.GetCachedCompositeFile(cacheBusterValue, type, filesetKey, out filePath);

        public IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, string cacheBusterValue, out string filePath)
            => _wrapped.GetCacheFile(file, sourceFile, fileWatchEnabled, extension, cacheBusterValue, out filePath);

        public IFileInfo GetRequiredFileInfo(string filePath)
            => _wrapped.GetRequiredFileInfo(filePath);

        public Task WriteFileAsync(string filePath, string contents)
            => _wrapped.WriteFileAsync(filePath, contents);

        public Task WriteFileAsync(string filePath, Stream contents)
            => _wrapped.WriteFileAsync(filePath, contents);
    }
}
