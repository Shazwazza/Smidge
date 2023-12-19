using Dazinator.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;

        public MemoryCacheFileSystem(IHasher hasher, ILogger logger)
        {
            _directory = new InMemoryDirectory();
            _fileProvider = new InMemoryFileProvider(_directory);
            _hasher = hasher;
            _logger = logger;
        }

        public IFileInfo GetRequiredFileInfo(string filePath)
        {
            var fileInfo = _fileProvider.GetFileInfo(filePath);

            if (!fileInfo.Exists)
            {
                _logger.LogError("No such file exists {FileName} (mapped from {FilePath})", fileInfo.PhysicalPath ?? fileInfo.Name, filePath);
            }

            return fileInfo;
        }

        private string GetCompositeFilePath(string cacheBusterValue, CompressionType type, string filesetKey) 
            => $"{cacheBusterValue}/{type}/{filesetKey + ".s"}";

        public Task ClearCachedCompositeFileAsync(string cacheBusterValue, CompressionType type, string filesetKey)
        {
            var path = GetCompositeFilePath(cacheBusterValue, type, filesetKey);

            var f = _directory.GetFile(path);
            if (f != null && !f.FileInfo.IsDirectory && f.FileInfo.Exists)
            {
                f.Delete();
            }

            return Task.CompletedTask;
        }

        public IFileInfo GetCachedCompositeFile(string cacheBusterValue, CompressionType type, string filesetKey, out string filePath)
        {
            filePath = GetCompositeFilePath(cacheBusterValue, type, filesetKey);
            return _fileProvider.GetFileInfo(filePath);
        }

        public IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, string cacheBusterValue, out string filePath)
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

                filePath = $"{cacheBusterValue}/{fileHash}/{timestampedHash}";
                cacheFile = _fileProvider.GetFileInfo(filePath);
            }
            else
            {
                var fileHash = _hasher.GetFileHash(file, extension);

                filePath = $"{cacheBusterValue}/{fileHash}";
                cacheFile = _fileProvider.GetFileInfo(filePath);
            }

            return cacheFile;
        }

        public Task WriteFileAsync(string filePath, string contents)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
                throw new InvalidOperationException("The path supplied must contain a file extension.");

            var segments = PathUtils.SplitPathIntoSegments(filePath);
            var dir = string.Join("/", segments.Take(segments.Length - 1));

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
            var dir = string.Join("/", segments.Take(segments.Length - 1));

            ClearStaleFiles(filePath);

            var f = _directory.AddFile(dir, new MemoryStreamFileInfo(memStream, segments[segments.Length - 1]));
        }

        private void ClearStaleFiles(string filePath)
        {
            // trim the first part (cache buster value)
            // and then clear all matching files for the last parts
            var parts = filePath.Split('/');
            var fileName = string.Join("/", parts.Skip(1));

            // clean out stale references for this file name
            var found = _directory.Search($"**/{fileName}");
            foreach (var existing in found)
            {
                existing.Delete();
            }
        }
    }
}
