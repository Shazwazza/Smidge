using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        private readonly ISmidgeFileSystem _fileSystem;
        private readonly CacheBusterResolver _cacheBusterResolver;
        private readonly IBundleManager _bundleManager;
        private readonly ILogger<PreProcessManager> _logger;

        public PreProcessManager(ISmidgeFileSystem fileSystem, CacheBusterResolver cacheBusterResolver, IBundleManager bundleManager, ILogger<PreProcessManager> logger)
        {
            _fileSystem = fileSystem;
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

            await ProcessFile(file, _bundleManager.GetAvailableOrDefaultBundleOptions(bundleOptions, false), bundleContext);
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
            if (bundleOptions == null) throw new ArgumentNullException(nameof(bundleOptions));
            if (bundleContext == null) throw new ArgumentNullException(nameof(bundleContext));

            var extension = Path.GetExtension(file.FilePath);

            var fileWatchEnabled = bundleOptions?.FileWatchOptions.Enabled ?? false;

            var cacheBuster = _cacheBusterResolver.GetCacheBuster(bundleOptions.GetCacheBusterType());

            //we're making this lazy since we don't always want to resolve it
            var sourceFile = new Lazy<IFileInfo>(() => _fileSystem.SourceFileProvider.GetRequiredFileInfo(file), LazyThreadSafetyMode.None);

            var cacheFile = _fileSystem.CacheFileSystem.GetCacheFile(file, () => sourceFile.Value, fileWatchEnabled, extension, cacheBuster);

            //check if it's in cache
            if (cacheFile.Exists)
            {
                _logger.LogDebug($"File already in cache '{file.FilePath}', type: {file.DependencyType}, cacheFile: {cacheFile}, watching? {fileWatchEnabled}");
            }
            else
            {
                if (file.Pipeline.Processors.Count > 0)
                {
                    _logger.LogDebug($"Processing file '{file.FilePath}', type: {file.DependencyType}, cacheFile: {cacheFile}, watching? {fileWatchEnabled} ...");
                    var contents = await _fileSystem.ReadContentsAsync(sourceFile.Value);
                    var watch = new Stopwatch();
                    watch.Start();
                    //process the file
                    var processed = await file.Pipeline.ProcessAsync(new FileProcessContext(contents, file, bundleContext));
                    watch.Stop();
                    _logger.LogDebug($"Processed file '{file.FilePath}' in {watch.ElapsedMilliseconds}ms");
                    //save it to the cache path
                    await _fileSystem.CacheFileSystem.WriteFileAsync(cacheFile, processed);
                }
                else
                {
                    // we can just write the the file as-is to the cache file
                    var contents = await _fileSystem.ReadContentsAsync(sourceFile.Value);
                    await _fileSystem.CacheFileSystem.WriteFileAsync(cacheFile, contents);
                }
            }

            //If file watching is enabled, then watch it - this is regardless of whether the cache file exists or not
            // since after app restart if there's already a cache file, we still want to watch the file set
            if (fileWatchEnabled)
            {
                // watch this file for changes, if the file is already watched this will do nothing
                _fileSystem.Watch(file, sourceFile.Value, bundleOptions, FileModified);
            }
        }

        /// <summary>
        /// Executed when a processed file is modified
        /// </summary>
        /// <param name="file"></param>
        private void FileModified(WatchedFile file)
        {
            //Raise the event on the file watch options
            file.BundleOptions.FileWatchOptions.Changed(new FileWatchEventArgs(file));
        }
    }
}