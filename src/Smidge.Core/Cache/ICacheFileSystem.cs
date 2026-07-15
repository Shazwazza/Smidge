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
        /// <summary>
        /// Gets the <see cref="IFileInfo"/> for a cached file, throwing a <see cref="System.IO.FileNotFoundException"/> if it does not exist.
        /// </summary>
        /// <remarks>
        /// Use this only when the file is expected to exist as an internal invariant. For request handling where the
        /// path is (or can be) client supplied, use <see cref="GetFileInfo(string)"/> and check <see cref="IFileInfo.Exists"/>
        /// so that a missing/stale/spoofed file results in a graceful 404 instead of an unhandled 500.
        /// </remarks>
        IFileInfo GetRequiredFileInfo(string filePath);

        /// <summary>
        /// Gets the <see cref="IFileInfo"/> for a cached file without throwing when it does not exist.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="IFileInfo"/> may have <see cref="IFileInfo.Exists"/> set to <c>false</c>; callers must check it.
        /// </remarks>
        IFileInfo GetFileInfo(string filePath);
        Task ClearCachedCompositeFileAsync(string cacheBusterValue, CompressionType type, string filesetKey);
        IFileInfo GetCachedCompositeFile(string cacheBusterValue, CompressionType type, string filesetKey, out string filePath);
        IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, string cacheBusterValue, out string filePath);
        Task WriteFileAsync(string filePath, string contents);
        Task WriteFileAsync(string filePath, Stream contents);
    }
}
