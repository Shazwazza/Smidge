using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// This performs the pre-processing on an IWebFile based on it's pipeline and writes the processed output to file cache
    /// </summary>
    public sealed class PreProcessManager
    {
        private readonly FileSystemHelper _fileSystemHelper;
        
        public PreProcessManager(FileSystemHelper fileSystemHelper)
        {
            _fileSystemHelper = fileSystemHelper;
        }

        /// <summary>
        /// This will first check if the file is in cache and if not it will 
        /// run all pre-processors assigned to the file and store the output in a persisted file cache.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileWatchOptions"></param>
        /// <returns></returns>
        public async Task ProcessAndCacheFileAsync(IWebFile file, FileWatchOptions fileWatchOptions)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Pipeline == null) throw new ArgumentNullException(string.Format("{0}.Pipeline", nameof(file)));

            await ProcessFile(file, fileWatchOptions);
        }

        private async Task ProcessFile(IWebFile file, FileWatchOptions fileWatchOptions)
        {
            var extension = Path.GetExtension(file.FilePath);

            //If Its external throw an exception this is not allowed. 
            if (file.FilePath.Contains(Constants.SchemeDelimiter))
            {
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            };

            //if watching is enabled, need to include the timestamp in the hash
            IFileInfo fileInfo = null;
            string hashName;
            if (fileWatchOptions.Enabled)
            {
                fileInfo = _fileSystemHelper.GetFileInfo(file);
                hashName = _fileSystemHelper.GetFileHash(file, fileInfo, extension);
            }
            else
            {
                hashName = _fileSystemHelper.GetFileHash(file, extension);
            }

            //check if it's in cache
                        
            var cacheDir = _fileSystemHelper.CurrentCacheFolder;
            var cacheFile = Path.Combine(cacheDir, hashName);

            Directory.CreateDirectory(cacheDir);

            if (!File.Exists(cacheFile))
            {
                //look up the file info if it hasn't been done already
                fileInfo = fileInfo ?? _fileSystemHelper.GetFileInfo(file);

                var contents = await _fileSystemHelper.ReadContentsAsync(fileInfo);

                //process the file
                var processed = await file.Pipeline.ProcessAsync(new FileProcessContext(contents, file));

                //save it to the cache path
                await _fileSystemHelper.WriteContentsAsync(cacheFile, processed);

                if (fileWatchOptions.Enabled)
                {
                    // watch this file for changes:
                    _fileSystemHelper.Watch(new WatchedFile(file, fileInfo), FileModified);
                }                
            }
        }

        

        private async Task ReProcessFile(IWebFile file, IFileInfo fileInfo)
        {
            var extension = Path.GetExtension(file.FilePath);

            var hashName = _fileSystemHelper.GetFileHash(file, fileInfo, extension);

            var cacheDir = _fileSystemHelper.CurrentCacheFolder;
            var cacheFile = Path.Combine(cacheDir, hashName);

            Directory.CreateDirectory(cacheDir);

            if (!File.Exists(cacheFile))
            {
                var contents = await _fileSystemHelper.ReadContentsAsync(fileInfo);

                //process the file
                var processed = await file.Pipeline.ProcessAsync(new FileProcessContext(contents, file));

                //save it to the cache path
                await _fileSystemHelper.WriteContentsAsync(cacheFile, processed);

                // watch this file for changes:
                _fileSystemHelper.Watch(new WatchedFile(file, fileInfo), FileModified);
            }
        }

        /// <summary>
        /// Executed when a processed file is modified
        /// </summary>
        /// <param name="file"></param>
        private void FileModified(WatchedFile file)
        {
            ReProcessFile(file.WebFile, file.FileInfo).Wait();
        }
    }
}