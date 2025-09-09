using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Smidge.Hashing;
using Smidge.Models;

namespace Smidge.Cache
{
    public class PhysicalFileCacheFileSystem : ICacheFileSystem
    {
        public static PhysicalFileCacheFileSystem CreatePhysicalFileCacheFileSystem(
            IHasher hasher,
            ISmidgeConfig config,
            IHostEnvironment hosting)
        {
            string cacheFolder = Path.Combine(hosting.ContentRootPath, config.DataFolder, "Cache", Environment.MachineName.ReplaceNonAlphanumericChars('-'));

            //ensure it exists
            Directory.CreateDirectory(cacheFolder);

            var cacheFileProvider = new PhysicalFileProvider(cacheFolder);

            return new PhysicalFileCacheFileSystem(cacheFileProvider, hasher);
        }

        private readonly IHasher _hasher;
        private readonly PhysicalFileProvider _fileProvider;

        public PhysicalFileCacheFileSystem(
            PhysicalFileProvider cacheFileProvider,
            IHasher hasher)
        {
            _fileProvider = cacheFileProvider;
            _hasher = hasher;
        }

        public IFileInfo GetRequiredFileInfo(string filePath)
        {
            IFileInfo fileInfo = _fileProvider.GetFileInfo(filePath);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"No such file exists {fileInfo.PhysicalPath ?? fileInfo.Name} (mapped from {filePath})", fileInfo.PhysicalPath ?? fileInfo.Name);
            }

            return fileInfo;
        }

        private string GetCompositeFilePath(string cacheBusterValue, CompressionType type, string filesetKey) => $"{cacheBusterValue}/{type}/{filesetKey}.s";

        public Task ClearCachedCompositeFileAsync(string cacheBusterValue, CompressionType type, string filesetKey)
        {
            string path = GetCompositeFilePath(cacheBusterValue, type, filesetKey);
            IFileInfo file = _fileProvider.GetFileInfo(path);

            if (file.PhysicalPath == null)
            {
                throw new InvalidOperationException("The IFileInfo object supplied is not compatible with this provider.");
            }

            if (file.IsDirectory)
            {
                throw new InvalidOperationException("The IFileInfo object supplied is a directory, not a file.");
            }

            if (file.Exists)
            {
                File.Delete(file.PhysicalPath);
            }

            return Task.CompletedTask;
        }

        public IFileInfo GetCachedCompositeFile(string cacheBusterValue, CompressionType type, string filesetKey, out string filePath)
        {
            filePath = GetCompositeFilePath(cacheBusterValue, type, filesetKey);
            return _fileProvider.GetFileInfo(filePath);
        }

        public Task WriteFileAsync(string filePath, string contents)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
            {
                throw new InvalidOperationException("The path supplied must contain a file extension.");
            }

            if (filePath.Contains(SmidgeConstants.SchemeDelimiter) || Path.IsPathRooted(filePath))
            {
                throw new InvalidOperationException("Illegal characters found in path.");
            }

            filePath = GetFullFilePathForWriting(filePath);

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!directoryPath.InvariantIgnoreCaseStartsWith(_fileProvider.Root))
            {
                throw new InvalidOperationException("Illegal characters found in path.");
            }

            Directory.CreateDirectory(directoryPath);
            using (StreamWriter writer = File.CreateText(filePath))
            {
                writer.Write(contents);
            }
            return Task.CompletedTask;
        }

        public async Task WriteFileAsync(string filePath, Stream contents)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
            {
                throw new InvalidOperationException("The path supplied must contain a file extension.");
            }

            if (filePath.Contains(SmidgeConstants.SchemeDelimiter) || Path.IsPathRooted(filePath))
            {
                throw new InvalidOperationException("Illegal characters found in path.");
            }

            filePath = GetFullFilePathForWriting(filePath);

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!directoryPath.InvariantIgnoreCaseStartsWith(_fileProvider.Root))
            {
                throw new InvalidOperationException("Illegal characters found in path.");
            }

            Directory.CreateDirectory(directoryPath);
            using (FileStream newFile = File.Create(filePath))
            {
                await contents.CopyToAsync(newFile);
            }
        }

        private string GetFullFilePathForWriting(string filePath) => Path.Combine(_fileProvider.Root, filePath.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>
        /// This will return the cache file path for a given IWebFile depending on if it's being watched
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileWatchEnabled"></param>
        /// <param name="extension"></param>
        /// <param name="cacheBuster"></param>
        /// <returns></returns>
        public IFileInfo GetCacheFile(IWebFile file, Func<IFileInfo> sourceFile, bool fileWatchEnabled, string extension, string cacheBusterValue, out string filePath)
        {
            // TODO: Here is where we are returning an invalidated path

            IFileInfo cacheFile;

            if (fileWatchEnabled)
            {
                //When file watching, the file path will be different since we'll hash twice:
                // * Hash normally, since this will be a static hash of the file name
                // * Hash with timestamp since this will be how we change it
                // This allows us to lookup the file's folder to store it's timestamped processed files

                //get the file hash without the extension
                string fileHash = _hasher.GetFileHash(file, string.Empty);
                string timestampedHash = _hasher.GetFileHash(file, sourceFile(), extension);

                filePath = $"{cacheBusterValue}/{fileHash}/{timestampedHash}";
                cacheFile = _fileProvider.GetFileInfo(filePath);
            }
            else
            {
                string fileHash = _hasher.GetFileHash(file, extension);

                filePath = $"{cacheBusterValue}/{fileHash}";
                cacheFile = _fileProvider.GetFileInfo(filePath);
            }

            return cacheFile;
        }
    }
}
