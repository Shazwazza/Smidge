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
using Smidge.Models;

namespace Smidge
{

    public sealed class FileWatcher
    {

        private readonly IFileProvider _fileProvider;

        public FileWatcher(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }



    }

    /// <summary>
    /// Singleton class that exposes methods for dealing with the file system
    /// </summary>
    public sealed class FileSystemHelper
    {
        private readonly ISmidgeConfig _config;
        private readonly IHostingEnvironment _hostingEnv;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocker = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly IFileProvider _fileProvider;

        public FileSystemHelper(IHostingEnvironment hostingEnv, ISmidgeConfig config, IFileProvider fileProvider)
        {
            _config = config;
            _hostingEnv = hostingEnv;
            _fileProvider = fileProvider;
        }

        public FileSystemHelper(IHostingEnvironment hostingEnv, ISmidgeConfig config)
        {
            _config = config;
            _hostingEnv = hostingEnv;
            _fileProvider = hostingEnv.WebRootFileProvider;
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
        /// Returns the cache folder for composite files for the current compression supported
        /// </summary>
        /// <returns></returns>
        public string GetCurrentCompositeFolder(CompressionType type)
        {
            return Path.Combine(CurrentCacheFolder, type.ToString());
        }

        public string GetCurrentCompositeFilePath(CompressionType type, string filesetKey)
        {
            return Path.Combine(GetCurrentCompositeFolder(type), filesetKey + ".s");
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

        internal async Task WriteContentsAsync(string filePath, string contents)
        {
            var locker = _fileLocker.GetOrAdd(filePath, x => new SemaphoreSlim(1));

            //TODO: Need try/catch and maybe a nice solution for locking.
            // We could keep a concurrent dictionary of file paths and objects to lock so that 
            // we only lock for a specific path. The dictionary should remain quite small since 
            // when we write to files it's only when they haven't already been cached.
            // we could also store this dictionary in an http cache so that it expires.

            await locker.WaitAsync();
            try
            {
                using (var writer = File.CreateText(filePath))
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
        /// The current cache folder for the current version
        /// </summary>
        /// <returns></returns>
        public string CurrentCacheFolder
        {
            get
            {
                return Path.Combine(_hostingEnv.ContentRootPath, _config.DataFolder, "Cache", _config.ServerName, _config.Version);
            }
        }

        public void Watch(IWebFile webFile, Action<object> action)
        {
            var path = webFile.FilePath.TrimStart(new[] { '~' });
            var changeToken = _fileProvider.Watch(path);
            changeToken.RegisterChangeCallback(action, webFile);
        }
    }
}
