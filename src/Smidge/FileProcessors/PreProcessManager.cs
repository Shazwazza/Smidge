using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// This performs the pre-processing on an <see cref="IWebFile"/> based on it's pipeline and writes the processed output to file cache
    /// </summary>
    public sealed class PreProcessManager
    {
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly CacheBusterResolver _cacheBusterResolver;
        private readonly IBundleManager _bundleManager;
        private readonly ILogger<PreProcessManager> _logger;

        public PreProcessManager(FileSystemHelper fileSystemHelper, CacheBusterResolver cacheBusterResolver, IBundleManager bundleManager, ILogger<PreProcessManager> logger)
        {
            _fileSystemHelper = fileSystemHelper;
            _cacheBusterResolver = cacheBusterResolver;
            _bundleManager = bundleManager;
            _logger = logger;
        }

        /// <summary>
        /// This will first check if the file is in cache and if not it will 
        /// run all pre-processors assigned to the file and store the output in a persisted file cache.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bundleOptions"></param>
        /// <param name="bundleContext"></param>
        /// <returns></returns>
        public async Task ProcessAndCacheFileAsync(IWebFile file, BundleOptions bundleOptions, BundleContext bundleContext)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Pipeline == null) throw new ArgumentNullException(string.Format("{0}.Pipeline", nameof(file)));

            await ProcessFile(file, bundleOptions, bundleContext);
        }

        private async Task ProcessFile(IWebFile file, BundleOptions bundleOptions, BundleContext bundleContext)
        {
            //If Its external throw an exception this is not allowed. 
            if (file.FilePath.Contains(Constants.SchemeDelimiter))
            {
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            };

            await ProcessFileImpl(file, bundleOptions, bundleContext);
        }

        private async Task ProcessFileImpl(IWebFile file, BundleOptions bundleOptions, BundleContext bundleContext)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            
            var extension = Path.GetExtension(file.FilePath);

            var fileWatchEnabled = bundleOptions?.FileWatchOptions.Enabled ?? false;

            Lazy<IFileInfo> fileInfo;
            var cacheBuster = bundleOptions != null
                ? _cacheBusterResolver.GetCacheBuster(bundleOptions.GetCacheBusterType())
                : _cacheBusterResolver.GetCacheBuster(_bundleManager.GetDefaultBundleOptions(false).GetCacheBusterType()); //the default for any dynamically (non bundle) file is the default bundle options in production
            
            var cacheFile = _fileSystemHelper.GetCacheFilePath(file, fileWatchEnabled, extension, cacheBuster, out fileInfo);

            var exists = File.Exists(cacheFile);            

            //check if it's in cache
            if (exists)
            {
                _logger.LogDebug($"File already in cache '{file.FilePath}', type: {file.DependencyType}, cacheFile: {cacheFile}, watching? {fileWatchEnabled}");
            }
            else
            {
                _logger.LogDebug($"Processing file '{file.FilePath}', type: {file.DependencyType}, cacheFile: {cacheFile}, watching? {fileWatchEnabled} ...");

                var contents = await _fileSystemHelper.ReadContentsAsync(fileInfo.Value);

                var watch = new Stopwatch();
                watch.Start();
                //process the file
                var processed = await file.Pipeline.ProcessAsync(new FileProcessContext(contents, file, bundleContext));
                watch.Stop();

                _logger.LogDebug($"Processed file '{file.FilePath}' in {watch.ElapsedMilliseconds}ms");

                //save it to the cache path
                await _fileSystemHelper.WriteContentsAsync(cacheFile, processed);
            }

            //If file watching is enabled, then watch it - this is regardless of whether the cache file exists or not
            // since after app restart if there's already a cache file, we still want to watch the file set
            if (fileWatchEnabled)
            {
                // watch this file for changes, if the file is already watched this will do nothing
                _fileSystemHelper.Watch(file, fileInfo.Value, bundleOptions, FileModified);
            }
        }
        
        /// <summary>
        /// Executed when a processed file is modified
        /// </summary>
        /// <param name="file"></param>
        private void FileModified(WatchedFile file)
        {
            //Raise the event on the file watch options
            file.BundleOptions.FileWatchOptions.Changed(new FileWatchEventArgs(file, _fileSystemHelper));            
        }
    }
}