using Microsoft.Extensions.FileProviders;
using Smidge.Hashing;
using Smidge.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Smidge.Cache
{
    

    public class PhysicalFileCacheFileSystem : ICacheFileSystem
    {
        private readonly IHasher _hasher;

        public PhysicalFileCacheFileSystem(IFileProvider cacheFileProvider, IHasher hasher)
        {
            FileProvider = cacheFileProvider;
            _hasher = hasher;
        }

        public IFileProvider FileProvider { get; }

        public Task ClearFileAsync(IFileInfo file)
        {
            if (file.PhysicalPath == null)
                throw new InvalidOperationException("The IFileInfo object supplied is not compatible with this provider");

            File.Delete(file.PhysicalPath);
            return Task.FromResult(0);
        }

        public Task WriteFileAsync(IFileInfo file, string contents)
        {
            if (file.PhysicalPath == null)
                throw new InvalidOperationException("The IFileInfo object supplied is not compatible with this provider");
            Directory.CreateDirectory(Path.GetDirectoryName(file.PhysicalPath));
            using (var writer = File.CreateText(file.PhysicalPath))
            {
                writer.Write(contents);
            }
            return Task.FromResult(0);
        }

        public async Task WriteFileAsync(IFileInfo file, Stream contents)
        {
            if (file.PhysicalPath == null)
                throw new InvalidOperationException("The IFileInfo object supplied is not compatible with this provider");
            Directory.CreateDirectory(Path.GetDirectoryName(file.PhysicalPath));
            using (var newFile = File.Create(file.PhysicalPath))
            {
                await contents.CopyToAsync(newFile);
            }
        }

        public IFileInfo GetCachedCompositeFile(ICacheBuster cacheBuster, CompressionType type, string filesetKey)
        {
            return FileProvider.GetFileInfo($"{cacheBuster.GetValue()}/{type.ToString()}/{filesetKey + ".s"}");
        }

        /// <summary>
        /// This will return the cache file path for a given IWebFile depending on if it's being watched
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileWatchEnabled"></param>
        /// <param name="extension"></param>
        /// <param name="cacheBuster"></param>
        /// <returns></returns>
        public IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, ICacheBuster cacheBuster)
        {
            IFileInfo cacheFile;
            if (fileWatchEnabled)
            {
                //When file watching, the file path will be different since we'll hash twice:
                // * Hash normally, since this will be a static hash of the file name
                // * Hash with timestamp since this will be how we change it
                // This allows us to lookup the file's folder to store it's timestamped processed files

                //get the file hash without the extension
                var fileHash = _hasher.GetFileHash(file, string.Empty);
                var timestampedHash = _hasher.GetFileHash(file, sourceFile(), extension);

                cacheFile = FileProvider.GetFileInfo($"{cacheBuster.GetValue()}/{fileHash}/{timestampedHash}");
            }
            else
            {
                var fileHash = _hasher.GetFileHash(file, extension);

                cacheFile = FileProvider.GetFileInfo($"{cacheBuster.GetValue()}/{fileHash}");
            }

            return cacheFile;
        }
    }
}
