using Microsoft.Extensions.FileProviders;
using Smidge.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Smidge.Cache
{

    /// <summary>
    /// The cache provider for caching files
    /// </summary>
    public interface ICacheFileSystem
    {
        IFileProvider FileProvider { get; }
        Task ClearCachedCompositeFile(IFileInfo file);
        IFileInfo GetCachedCompositeFile(ICacheBuster cacheBuster, CompressionType type, string filesetKey);
        IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, ICacheBuster cacheBuster);
        Task WriteFileAsync(IFileInfo file, string contents);
        Task WriteFileAsync(IFileInfo file, Stream contents);
    }
}
