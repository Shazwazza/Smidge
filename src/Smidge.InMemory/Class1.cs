using Dazinator.Extensions.FileProviders.InMemory;
using Microsoft.Extensions.FileProviders;
using Smidge;
using Smidge.Cache;
using Smidge.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    // TODO: Make this happen

    public class MemoryCacheFileSystem : ICacheFileSystem
    {
        public IFileProvider FileProvider => new InMemoryFileProvider();

        public Task ClearFileAsync(IFileInfo file)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetCachedCompositeFile(ICacheBuster cacheBuster, CompressionType type, string filesetKey)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, ICacheBuster cacheBuster)
        {
            throw new NotImplementedException();
        }

        public Task WriteFileAsync(IFileInfo file, string contents)
        {
            throw new NotImplementedException();
        }

        public Task WriteFileAsync(IFileInfo file, Stream contents)
        {
            throw new NotImplementedException();
        }
    }
}
