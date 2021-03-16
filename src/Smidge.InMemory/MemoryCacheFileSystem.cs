using Dazinator.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.InMemory;
using Dazinator.Extensions.FileProviders.InMemory.Directory;
using Microsoft.Extensions.FileProviders;
using Smidge;
using Smidge.Cache;
using Smidge.Hashing;
using Smidge.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smidge.InMemory
{
    public class MemoryCacheFileSystem : ICacheFileSystem
    {
        private readonly IDirectory _directory;
        private readonly IHasher _hasher;

        public MemoryCacheFileSystem(IHasher hasher)
        {
            _directory = new InMemoryDirectory();
            FileProvider = new InMemoryFileProvider(_directory);
            _hasher = hasher;
        }

        public IFileProvider FileProvider { get; }

        public Task ClearCachedCompositeFile(IFileInfo file)
        {
            if (file.IsDirectory)
                throw new InvalidOperationException("The IFileInfo object supplied is a directory, not a file.");

            var f = _directory.GetFile(file.Name);
            if (f != null)
            {
                f.Delete();
            }

            return Task.CompletedTask;
        }

        public IFileInfo GetCachedCompositeFile(ICacheBuster cacheBuster, CompressionType type, string filesetKey)
        {
            return FileProvider.GetFileInfo($"{cacheBuster.GetValue()}/{type.ToString()}/{filesetKey + ".s"}");
        }

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

        public Task WriteFileAsync(IFileInfo file, string contents)
        {
            if (file.IsDirectory)
                throw new InvalidOperationException("The IFileInfo object supplied is a directory, not a file.");

            var segments = PathUtils.SplitPathIntoSegments(file.Name);
            var dir = string.Join('/', segments.Take(segments.Length - 1));

            var f = _directory.AddFile(dir, new StringFileInfo(contents, segments[segments.Length - 1]));

            return Task.CompletedTask;
        }

        public async Task WriteFileAsync(IFileInfo file, Stream contents)
        {
            if (file.IsDirectory)
                throw new InvalidOperationException("The IFileInfo object supplied is a directory, not a file.");

            var memStream = new MemoryStream();
            await contents.CopyToAsync(memStream);

            var segments = PathUtils.SplitPathIntoSegments(file.Name);
            var dir = string.Join('/', segments.Take(segments.Length - 1));

            var f = _directory.AddFile(dir, new MemoryStreamFileInfo(memStream, segments[segments.Length - 1]));
        }
    }
}
