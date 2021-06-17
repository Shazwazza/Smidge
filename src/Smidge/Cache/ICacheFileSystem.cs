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
        IFileInfo GetRequiredFileInfo(string filePath);
        Task ClearCachedCompositeFileAsync(string cacheBusterValue, CompressionType type, string filesetKey);
        IFileInfo GetCachedCompositeFile(string cacheBusterValue, CompressionType type, string filesetKey, out string filePath);
        IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, string cacheBusterValue, out string filePath);
        Task WriteFileAsync(string filePath, string contents);
        Task WriteFileAsync(string filePath, Stream contents);
    }
}
