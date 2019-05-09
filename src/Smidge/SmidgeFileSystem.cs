using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Smidge.Hashing;
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
        public SmidgeFileSystem(IFileProvider sourceFileProvider, ICacheFileSystem cacheFileProvider)
        {
            SourceFileProvider = sourceFileProvider;
            CacheFileSystem = cacheFileProvider;
        }

        public IFileProvider SourceFileProvider { get; }
        public ICacheFileSystem CacheFileSystem { get; }


        /// <summary>
        /// Rudimentary check to see if the path is a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsFolder(string path)
        {
            var fileInfo = SourceFileProvider.GetFileInfo(path);
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
            if (!lastPart.Contains("."))
            {
                return true;
            }

            //This is used when specifying extensions in a folder
            if (lastPart.Contains("*"))
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

            var folderContents = SourceFileProvider.GetDirectoryContents(folderPart);

            if (folderContents.Exists)
            {

                var files = string.IsNullOrWhiteSpace(extensionFilter)
                    ? folderContents
                    : folderContents.Where(
                        (a) => !a.IsDirectory && a.Exists && Path.GetExtension(a.Name) == string.Format(".{0}", extensionFilter));
                return files.Select(x => ReverseMapPath(folderPart, x));
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
            var path = webFile.FilePath.TrimStart(new[] { '~' }).ToLowerInvariant();

            //don't double watch if there's already a watcher for this file
            if (_fileWatchers.ContainsKey(path)) return false;

            var watchedFile = new WatchedFile(webFile, fileInfo, bundleOptions);

            var changeToken = SourceFileProvider.Watch(path);
            _fileWatchers.TryAdd(path, changeToken.RegisterChangeCallback(o =>
            {
                //try to remove the item from the dictionary so it can be added again
                _fileWatchers.TryRemove(path, out IDisposable watcher);

                //call the callback with the strongly typed object
                fileModifiedCallback((WatchedFile)o);
            }, watchedFile));

            return true;
        }

        //TODO: We need an unwatch
        private readonly ConcurrentDictionary<string, IDisposable> _fileWatchers = new ConcurrentDictionary<string, IDisposable>();


    }
}
