using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.Hashing;
using Smidge.Models;
using Smidge.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Smidge.InMemory
{

    /// <summary>
    /// Uses either an <see cref="MemoryCacheFileSystem"/> or a <see cref="PhysicalFileCacheFileSystem"/> depending on what is
    /// configured in <see cref="SmidgeCacheOptions"/>
    /// </summary>
    public class ConfiguredCacheFileSystem : ICacheFileSystem
    {
        private SmidgeOptions _options;
        private ICacheFileSystem _wrapped;

        public ConfiguredCacheFileSystem(IOptions<SmidgeOptions> options, IServiceProvider services)
        {
            _options = options.Value;
            var hasher = services.GetRequiredService<IHasher>();
#if NETCORE3_0
            var hosting = services.GetRequiredService<IWebHostEnvironment>();
#else
            var hosting = services.GetRequiredService<IHostingEnvironment>();
#endif
            if (_options.CacheOptions.UseInMemoryCache)
            {
                _wrapped = new MemoryCacheFileSystem(hasher);
            }
            else
            {
                _wrapped = services.CreatePhysicalFileCacheFileSystem();
            }
        }

        public Task ClearCachedCompositeFileAsync(ICacheBuster cacheBuster, CompressionType type, string filesetKey)
            => _wrapped.ClearCachedCompositeFileAsync(cacheBuster, type, filesetKey);

        public IFileInfo GetCachedCompositeFile(ICacheBuster cacheBuster, CompressionType type, string filesetKey, out string filePath)
            => _wrapped.GetCachedCompositeFile(cacheBuster, type, filesetKey, out filePath);

        public IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, ICacheBuster cacheBuster, out string filePath)
            => _wrapped.GetCacheFile(file, sourceFile, fileWatchEnabled, extension, cacheBuster, out filePath);

        public IFileInfo GetRequiredFileInfo(string filePath)
            => _wrapped.GetRequiredFileInfo(filePath);

        public Task WriteFileAsync(string filePath, string contents)
            => _wrapped.WriteFileAsync(filePath, contents);

        public Task WriteFileAsync(string filePath, Stream contents)
            => _wrapped.WriteFileAsync(filePath, contents);
    }
}
