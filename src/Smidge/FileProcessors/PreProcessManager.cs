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

            //var extension = Path.GetExtension(file.WebFile.FilePath);

            //Refreshing the IFileInfo object so we can get it's latest metadata
            //TODO: Do we need this? Does the file watch get re-created?
            //file.RefreshFileInfo(_fileSystemHelper);

            //Raise event
            file.BundleOptions.FileWatchOptions.Changed(new FileWatchEventArgs(file, _fileSystemHelper));

            ////TODO: We need to figure out how to call another callback on the Actual Bundle that 
            //// issued the file watching, this is so we can delete (or rename/invalidate) the persistent processed/combined/compressed
            //// composite file when one of the bundle files changes.
            ////var filesetPath = _fileSystemHelper.GetCurrentCompositeFilePath(CompressionType.none, file.);

            //var hashName = _fileSystemHelper.GetFileHash(file.WebFile, file.FileInfo, extension);

            //var cacheDir = _fileSystemHelper.CurrentCacheFolder;
            //var cacheFile = Path.Combine(cacheDir, hashName);

            //Directory.CreateDirectory(cacheDir);

            //if (!File.Exists(cacheFile))
            //{
            //    var contents = await _fileSystemHelper.ReadContentsAsync(file.FileInfo);

            //    //process the file
            //    var processed = await file.WebFile.Pipeline.ProcessAsync(new FileProcessContext(contents, file.WebFile));

            //    //save it to the cache path
            //    await _fileSystemHelper.WriteContentsAsync(cacheFile, processed);                
            //}

            //// keep watching this file for changes:
            //_fileSystemHelper.Watch(file.WebFile, file.FileInfo, file.BundleOptions, FileModified);
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