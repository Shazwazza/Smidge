using Dazinator.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;
using Microsoft.Extensions.FileProviders;
using Smidge.Cache;
using Smidge.Hashing;
using Smidge.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Smidge.InMemory
{
    public class MemoryCacheFileSystem : ICacheFileSystem
    {
        private readonly IDirectory _directory;
        private readonly IHasher _hasher;
        private readonly IFileProvider _fileProvider;

        public MemoryCacheFileSystem(IHasher hasher)
        {
            _directory = new InMemoryDirectory();
            _fileProvider = new InMemoryFileProvider(_directory);
            _hasher = hasher;
        }

        public IFileInfo GetRequiredFileInfo(string filePath)
        {
            var fileInfo = _fileProvider.GetFileInfo(filePath);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"No such file exists {fileInfo.PhysicalPath ?? fileInfo.Name} (mapped from {filePath})", fileInfo.PhysicalPath ?? fileInfo.Name);
            }

            return fileInfo;
        }

        private string GetCompositeFilePath(ICacheBuster cacheBuster, CompressionType type, string filesetKey) 
            => $"{cacheBuster.GetValue()}/{type.ToString()}/{filesetKey + ".s"}";

        public Task ClearCachedCompositeFileAsync(ICacheBuster cacheBuster, CompressionType type, string filesetKey)
        {
            var path = GetCompositeFilePath(cacheBuster, type, filesetKey);

            var f = _directory.GetFile(path);
            if (f != null && !f.FileInfo.IsDirectory && f.FileInfo.Exists)
            {
                f.Delete();
            }

            return Task.CompletedTask;
        }

        public IFileInfo GetCachedCompositeFile(ICacheBuster cacheBuster, CompressionType type, string filesetKey, out string filePath)
        {
            filePath = GetCompositeFilePath(cacheBuster, type, filesetKey);
            return _fileProvider.GetFileInfo(filePath);
        }

        public IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, ICacheBuster cacheBuster, out string filePath)
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

                filePath = $"{cacheBuster.GetValue()}/{fileHash}/{timestampedHash}";
                cacheFile = _fileProvider.GetFileInfo(filePath);
            }
            else
            {
                var fileHash = _hasher.GetFileHash(file, extension);

                filePath = $"{cacheBuster.GetValue()}/{fileHash}";
                cacheFile = _fileProvider.GetFileInfo(filePath);
            }

            return cacheFile;
        }

        public Task WriteFileAsync(string filePath, string contents)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
                throw new InvalidOperationException("The path supplied must contain a file extension.");

            var segments = PathUtils.SplitPathIntoSegments(filePath);
#if NETCORE3_0
            var dir = string.Join('/', segments.Take(segments.Length - 1));
#else
            var dir = string.Join("/", segments.Take(segments.Length - 1));
#endif

            ClearStaleFiles(filePath);

            var f = _directory.AddFile(dir, new StringFileInfo(contents, segments[segments.Length - 1]));
            
            return Task.CompletedTask;
        }

        public async Task WriteFileAsync(string filePath, Stream contents)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
                throw new InvalidOperationException("The path supplied must contain a file extension.");

            var memStream = new MemoryStream();
            await contents.CopyToAsync(memStream);

            var segments = PathUtils.SplitPathIntoSegments(filePath);
#if NETCORE3_0
            var dir = string.Join('/', segments.Take(segments.Length - 1));
#else
            var dir = string.Join("/", segments.Take(segments.Length - 1));
#endif

            ClearStaleFiles(filePath);

            var f = _directory.AddFile(dir, new MemoryStreamFileInfo(memStream, segments[segments.Length - 1]));
        }

        private void ClearStaleFiles(string filePath)
        {
            // trim the first part (cache buster value)
            // and then clear all matching files for the last parts
#if NETCORE3_0
            var parts = filePath.Split('/');
            var fileName = string.Join('/', parts.Skip(1));
#else
            var parts = filePath.Split('/');
            var fileName = string.Join("/", parts.Skip(1));
#endif

            // clean out stale references for this file name
            var found = _directory.Search($"**/{fileName}");
            foreach (var existing in found)
            {
                existing.Delete();
            }
        }
    }
}
