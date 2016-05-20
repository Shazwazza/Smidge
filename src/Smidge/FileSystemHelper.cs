using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Smidge
{
    public sealed class FileSystemHelper
    {
        private IHostingEnvironment _appEnv;
        private ISmidgeConfig _config;
        private readonly IUrlHelper _urlHelper;
        private IHostingEnvironment _hostingEnv;
        private ConcurrentDictionary<string, SemaphoreSlim> _fileLocker = new ConcurrentDictionary<string, SemaphoreSlim>();
        private IFileProvider _fileProvider;

        public FileSystemHelper(IHostingEnvironment appEnv, IHostingEnvironment hostingEnv, ISmidgeConfig config, IUrlHelper urlHelper, IFileProvider fileProvider)
        {
            _appEnv = appEnv;
            _config = config;
            _urlHelper = urlHelper;
            _hostingEnv = hostingEnv;
            _fileProvider = fileProvider;

        }

        public static bool IsExternalRequestPath(string path)
        {
            if ((path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("//", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Takes in a given path and returns it's normalized result, either as a relative path for local files or an absolute web path with a host
        /// </summary>
        /// <param name="path"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public string NormalizeWebPath(string path, HttpRequest request)
        {
            if (path.StartsWith("~/"))
            {
                return _urlHelper.Content(path);
            }

            //if this is a protocol-relative/protocol-less uri, then we need to add the protocol for the remaining
            // logic to work properly
            if (path.StartsWith("//"))
            {
                return Regex.Replace(path, @"^\/\/", string.Format("{0}{1}", request.Scheme, Constants.SchemeDelimiter));
            }

            return _urlHelper.Content(path);
        }

        /// <summary>
        /// Rudimentary check to see if the path is a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsFolder(string path)
        {
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
                        (a) => !a.IsDirectory && a.Exists && Path.GetExtension(a.PhysicalPath) == string.Format(".{0}", extensionFilter));
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
        /// A rudimentary map path function
        /// </summary>
        /// <param name="contentFile"></param>
        /// <returns></returns>
        public string MapPath(string contentFile)
        {
            var content = _urlHelper.Content(contentFile);

            var fileInfo = _hostingEnv.ContentRootFileProvider.GetFileInfo(content);
            if (fileInfo.Exists)
                return fileInfo.PhysicalPath;

            throw new FileNotFoundException($"No such file exists {fileInfo.PhysicalPath} (mapped from {contentFile})", fileInfo.PhysicalPath);            
        }

        /// <summary>
        /// A rudimentary reverse map path function
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public string ReverseMapPath(string subPath, IFileInfo fileInfo)
        {
            var subPathDir = subPath.Replace("/", "\\");
            var subPathIndex = fileInfo.PhysicalPath.IndexOf(subPathDir, StringComparison.OrdinalIgnoreCase);
            var fileSubPath = fileInfo.PhysicalPath.Substring(subPathIndex);

            var reversed = fileSubPath.Replace("\\", "/");
            if (!reversed.StartsWith("/"))
            {
                reversed = "/" + reversed;
            }
            return "~" + reversed;
        }


        internal async Task<string> ReadContentsAsync(string filePath)
        {
            using (var fileStream = File.OpenRead(filePath))
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
                return Path.Combine(_appEnv.ContentRootPath, _config.DataFolder, "Cache", _config.ServerName, _config.Version);
            }
        }

    }
}