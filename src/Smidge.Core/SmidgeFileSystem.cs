using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Smidge.Models;
using Smidge.Options;
using Smidge.Cache;

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

        public SmidgeFileSystem(IFileProvider sourceFileProvider, ICacheFileSystem cacheFileProvider, IWebsiteInfo siteInfo)
        {
            _sourceFileProvider = sourceFileProvider;
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

        /// <summary>
        /// Rudimentary check to see if the path is a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsFolder(string path)
        {
            var fileInfo = _sourceFileProvider.GetFileInfo(ConvertToFileProviderPath(path));
            if (fileInfo == null) return false;

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
            if (!lastPart.Contains('.'))
            {
                return true;
            }

            //This is used when specifying extensions in a folder
            if (lastPart.Contains('*'))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<string> GetPathsForFilesInFolder(string folderPath)
        {
            //parse out the folder if it contains an asterisk
            var parts = folderPath.Split('*');
            var folderPart = parts[0];
            var extensionFilter = parts.Length > 1 ? parts[1] : null;
            var fileProviderFolderPath = ConvertToFileProviderPath(folderPart);
            var folderContents = _sourceFileProvider.GetDirectoryContents(fileProviderFolderPath);

            if (folderContents.Exists)
            {

                var files = string.IsNullOrWhiteSpace(extensionFilter)
                    ? folderContents
                    : folderContents.Where(
                        (a) => !a.IsDirectory && a.Exists && Path.GetExtension(a.Name) == string.Format(".{0}", extensionFilter));
                return files.Select(x => ReverseMapPath(fileProviderFolderPath, x));
            }

            return Enumerable.Empty<string>();
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
            if (_fileWatchers.ContainsKey(path)) return false;

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
        /// <returns></returns>
        /// <remarks>
        /// This will handle virtual paths like ~/myfile.js
        /// This will handle absolute paths like /myfile.js
        /// This will handle absolute paths like /myvirtualapp/myfile.js - where myvirtualapp is a virtual application (Path Base)
        /// </remarks>
        public string ConvertToFileProviderPath(string path)
        {
            if (path.StartsWith("~"))
            {
                return path.TrimStart('~');
            }


            string pathBase = _siteInfo.GetBasePath();
            if (pathBase == null)
            {
                pathBase = string.Empty;
            }

            if (pathBase.Length > 0 && path.StartsWith(pathBase))
                return path.Substring(pathBase.Length);

            return path;
        }

        
    }
}
