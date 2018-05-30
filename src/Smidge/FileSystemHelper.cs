using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.Hashing;
using Smidge.Models;
using Smidge.Options;

namespace Smidge
{
    /// <summary>
    /// Singleton class that exposes methods for dealing with the file system
    /// </summary>
    public sealed class FileSystemHelper
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocker = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly IFileProvider _fileProvider;
        private readonly IHasher _hasher;
        private readonly IFileProvider _cacheFileProvider;

        public FileSystemHelper(IFileProvider sourceFileProvider, IFileProvider cacheFileProvider, IHasher hasher)
        {
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _fileProvider = sourceFileProvider ?? throw new ArgumentNullException(nameof(sourceFileProvider));
            _cacheFileProvider = cacheFileProvider ?? throw new ArgumentNullException(nameof(cacheFileProvider));
        }

        public IFileInfo GetFileInfo(IWebFile webfile)
        {
            var path = webfile.FilePath.TrimStart(new[] { '~' });
            var fileInfo = _fileProvider.GetFileInfo(path);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"No such file exists {fileInfo.PhysicalPath} (mapped from {path})", fileInfo.PhysicalPath);
            }

            return fileInfo;
        }

        public IFileInfo GetFileInfo(string filePath)
        {
            var path = filePath.TrimStart(new[] { '~' });
            var fileInfo = _fileProvider.GetFileInfo(path);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"No such file exists {fileInfo.PhysicalPath ?? fileInfo.Name} (mapped from {filePath})", fileInfo.PhysicalPath);
            }

            return fileInfo;
        }

        /// <summary>
        /// Rudimentary check to see if the path is a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsFolder(string path)
        {

            var fileInfo = _fileProvider.GetFileInfo(path);
            if (fileInfo.IsDirectory)
            {
                return true;
            }

            if (path.EndsWith("/"))
            {
                return true;
            }

            //the last part doesn't contain a '.'
            var parts = path.Split('/');
            var lastPart = parts[parts.Length - 1];
            if (!lastPart.Contains("."))
            {
                return true;
            }

            //TODO: Use globbing...
            ////This is used when specifying extensions in a folder
            //if (lastPart.Contains("*"))
            //{
            //    return true;
            //}

            return false;
        }

        public IEnumerable<string> GetPathsForFilesInFolder(string folderPath)
        {
            //TODO: Use globbing...

            ////parse out the folder if it contains an asterisk
            //var parts = folderPath.Split('*');
            //var folderPart = parts[0];
            //var extensionFilter = parts.Length > 1 ? parts[1] : null;

            var folderContents = _fileProvider.GetDirectoryContents(folderPart);

            if (folderContents.Exists)
            {

                var files = string.IsNullOrWhiteSpace(extensionFilter)
                    ? folderContents
                    : folderContents.Where(
                        (a) => !a.IsDirectory && a.Exists && Path.GetExtension(a.Name) == string.Format(".{0}", extensionFilter));
                return files.Select(x => ReverseMapPath(folderPart, x));
            }
            else
            {
                throw new DirectoryNotFoundException($"The directory specified {folderPart} does not exist");
            }
        }

        /// <summary>
        /// Returns a file from the composite file storage
        /// </summary>
        /// <param name="cacheFilePath"></param>
        /// <returns></returns>
        public IFileInfo GetCompositeFileInfo(string cacheFilePath)
        {
            return _cacheFileProvider.GetFileInfo(cacheFilePath);
        }

        /// <summary>
        /// Returns a file from the composite file storage for a cachebuester/compressiontype/fileset combination
        /// </summary>
        /// <param name="cacheBuster"></param>
        /// <param name="type"></param>
        /// <param name="filesetKey"></param>
        /// <returns></returns>
        public IFileInfo GetCompositeFileInfo(ICacheBuster cacheBuster, CompressionType type, string filesetKey)
        {
            return GetCompositeFileInfo(Path.Combine(cacheBuster.GetValue(), type.ToString(),  filesetKey + ".s"));
        }

        /// <summary>
        /// A rudimentary reverse map path function
        /// </summary>
        /// <param name="subPath"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public string ReverseMapPath(string subPath, IFileInfo fileInfo)
        {
            var reversed = subPath.Replace("\\", "/");
            if (!reversed.StartsWith("/"))
            {
                reversed = $"/{reversed}";
            }
            //if (!reversed.StartsWith("~"))
            //{
            //    reversed = $"~{reversed}";
            //}
            if (!reversed.EndsWith(fileInfo.Name))
            {
                return $"~{reversed}/{fileInfo.Name}";
            }
            else
            {
                return $"~{reversed}";
            }
           
        }

        internal async Task<string> ReadContentsAsync(IFileInfo fileInfo)
        {
            using (var fileStream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(fileStream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        internal async Task WriteCacheFileAsync(IFileInfo fileInfo, string contents)
        {            
            var locker = _fileLocker.GetOrAdd(fileInfo.PhysicalPath, x => new SemaphoreSlim(1));
            
            await locker.WaitAsync();
            try
            {
                //ensure dir
                Directory.CreateDirectory(Path.GetDirectoryName(fileInfo.PhysicalPath));
                using (var writer = File.CreateText(fileInfo.PhysicalPath))
                {
                    await writer.WriteAsync(contents);
                }
            }
            finally
            {
                locker.Release();
            }
        }

        /// <summary>
        /// This will return the cache file path for a given IWebFile depending on if it's being watched
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileWatchEnabled"></param>
        /// <param name="extension"></param>
        /// <param name="cacheBuster"></param>
        /// <param name="fileInfo">
        /// A getter to the underlying IFileInfo, this is lazy because when file watching is not enabled we do not want to resolve
        /// this if the cache file already exists
        /// </param>
        /// <returns></returns>
        public IFileInfo GetCacheFile(IWebFile file, bool fileWatchEnabled, string extension, ICacheBuster cacheBuster, out Lazy<IFileInfo> fileInfo)
        {
            IFileInfo cacheFile;
            if (fileWatchEnabled)
            {
                //When file watching, the file path will be different since we'll hash twice:
                // * Hash normally, since this will be a static hash of the file name
                // * Hash with timestamp since this will be how we change it
                // This allows us to lookup the file's folder to store it's timestamped processed files

                var fi = GetFileInfo(file);

                //get the file hash without the extension
                var fileHash = GetFileHash(file, string.Empty);
                var timestampedHash = GetFileHash(file, fi, extension);
                
                cacheFile = _cacheFileProvider.GetFileInfo(Path.Combine(cacheBuster.GetValue(), fileHash, timestampedHash));
                fileInfo = new Lazy<IFileInfo>(() => fi, LazyThreadSafetyMode.None);
            }
            else
            {
                var fileHash = GetFileHash(file, extension);

                cacheFile = _cacheFileProvider.GetFileInfo(Path.Combine(cacheBuster.GetValue(), fileHash));
                fileInfo = new Lazy<IFileInfo>(() => GetFileInfo(file), LazyThreadSafetyMode.None);
            }
            
            return cacheFile;
        }
        
        /// <summary>
        /// Returns a file's hash
        /// </summary>
        /// <param name="file"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public string GetFileHash(IWebFile file, string extension)
        {
            var hashName = _hasher.Hash(file.FilePath) + extension;
            return hashName;
        }

        /// <summary>
        /// Returns a file's hash which includes it's timestamp
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileInfo"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public string GetFileHash(IWebFile file, IFileInfo fileInfo, string extension)
        {
            var lastWrite = fileInfo.LastModified;
            var hashName = _hasher.Hash(file.FilePath + lastWrite) + extension;
            return hashName;
        }

        /// <summary>
        /// Registers a file to be watched with a callback for when it is modified
        /// </summary>
        /// <param name="webFile"></param>
        /// <param name="fileInfo"></param>
        /// <param name="bundleOptions"></param>
        /// <param name="fileModifiedCallback"></param>
        /// <returns>
        /// Returns true if a watcher was added, false if the file is already being watched
        /// </returns>
        public bool Watch(IWebFile webFile, IFileInfo fileInfo, BundleOptions bundleOptions, Action<WatchedFile> fileModifiedCallback)
        {
            var path = webFile.FilePath.TrimStart(new[] { '~' }).ToLowerInvariant();

            //don't double watch if there's already a watcher for this file
            if (_fileWatchers.ContainsKey(path)) return false;

            var watchedFile = new WatchedFile(webFile, fileInfo, bundleOptions);

            var changeToken = _fileProvider.Watch(path);
            _fileWatchers.TryAdd(path, changeToken.RegisterChangeCallback(o =>
            {
                //try to remove the item from the dictionary so it can be added again
                IDisposable watcher;
                _fileWatchers.TryRemove(path, out watcher);

                //call the callback with the strongly typed object
                fileModifiedCallback((WatchedFile)o);
            }, watchedFile));

            return true;
        }

        //TODO: We need an unwatch
        private readonly ConcurrentDictionary<string, IDisposable> _fileWatchers = new ConcurrentDictionary<string, IDisposable>();

    }
}
