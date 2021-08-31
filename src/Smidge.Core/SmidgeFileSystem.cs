using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Smidge.Models;
using Smidge.Options;
using Smidge.Cache;
using System.Linq;

namespace Smidge
{
    /// <summary>
    /// Singleton class that exposes methods for dealing with the file system
    /// </summary>
    public sealed class SmidgeFileSystem : ISmidgeFileSystem
    {
        //TODO: We need an unwatch
        private readonly ConcurrentDictionary<string, IDisposable> _fileWatchers = new ConcurrentDictionary<string, IDisposable>();
        private readonly IWebsiteInfo _siteInfo;
        private readonly IFileProvider _sourceFileProvider;
        private readonly IFileProviderFilter _fileProviderFilter;

        public SmidgeFileSystem(
            IFileProvider sourceFileProvider,
            IFileProviderFilter fileProviderFilter,
            ICacheFileSystem cacheFileProvider,
            IWebsiteInfo siteInfo)
        {
            _sourceFileProvider = sourceFileProvider;
            _fileProviderFilter = fileProviderFilter;
            CacheFileSystem = cacheFileProvider;
            _siteInfo = siteInfo;
        }

        public ICacheFileSystem CacheFileSystem { get; }

        public IFileInfo GetRequiredFileInfo(IWebFile webfile)
        {
            var path = ConvertToFileProviderPath(webfile.FilePath);
            var fileInfo = _sourceFileProvider.GetFileInfo(path);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"No such file exists {fileInfo.PhysicalPath ?? fileInfo.Name} (mapped from {path})", fileInfo.PhysicalPath ?? fileInfo.Name);
            }

            return fileInfo;
        }

        public IFileInfo GetRequiredFileInfo(string filePath)
        {
            var path = ConvertToFileProviderPath(filePath);
            var fileInfo = _sourceFileProvider.GetFileInfo(path);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"No such file exists {fileInfo.PhysicalPath ?? fileInfo.Name} (mapped from {filePath})", fileInfo.PhysicalPath ?? fileInfo.Name);
            }

            return fileInfo;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetMatchingFiles(string filePattern)
        {
            var ext = Path.GetExtension(filePattern);
            if (string.IsNullOrWhiteSpace(ext))
            {
                // if there's no extention we can assume it's a directory, so normalize
                filePattern = $"{filePattern}/*.*";
            }

            // normalize for virtual paths
            filePattern = ConvertToFileProviderPath(filePattern);
            return _fileProviderFilter.GetMatchingFiles(_sourceFileProvider, filePattern)
                .Select(x => $"~{x}"); // back to virtual path
        }

        /// <summary>
        /// A rudimentary reverse map path function
        /// </summary>
        /// <param name="subPath"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public string ReverseMapPath(string subPath, IFileInfo fileInfo)
        {
            var reversed = subPath.Replace('\\', '/');

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

        public async Task<string> ReadContentsAsync(IFileInfo fileInfo)
        {
            using (var fileStream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(fileStream))
            {
                return await reader.ReadToEndAsync();
            }
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
            var path = ConvertToFileProviderPath(webFile.FilePath).ToLowerInvariant();

            //don't double watch if there's already a watcher for this file
            if (_fileWatchers.ContainsKey(path))
                return false;

            var watchedFile = new WatchedFile(webFile, fileInfo, bundleOptions);

            var changeToken = _sourceFileProvider.Watch(path);
            _fileWatchers.TryAdd(path, changeToken.RegisterChangeCallback(o =>
            {
                //try to remove the item from the dictionary so it can be added again
                _fileWatchers.TryRemove(path, out IDisposable watcher);

                //call the callback with the strongly typed object
                fileModifiedCallback((WatchedFile)o);
            }, watchedFile));

            return true;
        }

        /// <summary>
        /// Formats a file path into a compatible path for use with the file provider
        /// </summary>
        /// <param name="path"></param>
        /// <returns>
        /// A path compatible with IFileProvider which must start with a forward slash.
        /// </returns>
        /// <remarks>
        /// This will handle virtual paths like ~/myfile.js
        /// This will handle absolute paths like /myfile.js
        /// This will handle absolute paths like /myvirtualapp/myfile.js - where myvirtualapp is a virtual application (Path Base)
        /// </remarks>
        public string ConvertToFileProviderPath(string path)
        {
            if (path.StartsWith('~'))
            {
                return path.TrimStart('~');
            }

            if (path.StartsWith('/'))
            {
                path = path.TrimStart('/');
            }

            string pathBase = _siteInfo.GetBasePath()?.TrimStart('/');
            if (pathBase == null)
            {
                pathBase = string.Empty;
            }

            if (pathBase.Length > 0 && path.StartsWith(pathBase))
            {
                path = path.Substring(pathBase.Length);
            }

            return path.EnsureStartsWith('/');
        }


    }
}
