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
        /// <param name="bundleOptions"></param>
        /// <returns></returns>
        public async Task ProcessAndCacheFileAsync(IWebFile file, BundleOptions bundleOptions)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Pipeline == null) throw new ArgumentNullException(string.Format("{0}.Pipeline", nameof(file)));

            await ProcessFile(file, bundleOptions);
        }

        private async Task ProcessFile(IWebFile file, BundleOptions bundleOptions)
        {
            //If Its external throw an exception this is not allowed. 
            if (file.FilePath.Contains(Constants.SchemeDelimiter))
            {
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            };

            await ProcessFileImpl(file, bundleOptions);
        }

        private async Task ProcessFileImpl(IWebFile file, BundleOptions bundleOptions)
        {
            var extension = Path.GetExtension(file.FilePath);

            var fileWatchEnabled = bundleOptions?.FileWatchOptions.Enabled ?? false;

            Lazy<IFileInfo> fileInfo;
            var cacheFile = _fileSystemHelper.GetCacheFilePath(file, fileWatchEnabled, extension, out fileInfo);

            //check if it's in cache
            if (!File.Exists(cacheFile))
            {
                var contents = await _fileSystemHelper.ReadContentsAsync(fileInfo.Value);

                //process the file
                var processed = await file.Pipeline.ProcessAsync(new FileProcessContext(contents, file));

                //save it to the cache path
                await _fileSystemHelper.WriteContentsAsync(cacheFile, processed);
            }

            //If file watching is enabled, then watch it - this is regardless of whether the cache file exists or not
            // since after app restart if there's already a cache file, we still want to watch the file set
            if (fileWatchEnabled)
            {
                // watch this file for changes:
                _fileSystemHelper.Watch(file, fileInfo.Value, bundleOptions, FileModified);
            }
        }

        private async Task ReProcessFile(WatchedFile file)
        {
            await ProcessFileImpl(file.WebFile, file.BundleOptions);

            //Raise event
            file.BundleOptions.FileWatchOptions.Changed(new FileWatchEventArgs(file, _fileSystemHelper));
        }

        /// <summary>
        /// Executed when a processed file is modified
        /// </summary>
        /// <param name="file"></param>
        private void FileModified(WatchedFile file)
        {
            //TODO: Surely we need to unwatch this now?

            ReProcessFile(file).Wait();
        }
    }
}