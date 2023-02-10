using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Smidge.CompositeFiles;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// This performs the pre-processing on an <see cref="IWebFile"/> based on it's pipeline and writes the processed output to file cache
    /// </summary>
    public class PreProcessManager : IPreProcessManager
    {
        private readonly ISmidgeFileSystem _fileSystem;
        private readonly IBundleManager _bundleManager;
        private readonly ILogger<PreProcessManager> _logger;
        private readonly SemaphoreSlim _processFileSemaphore = new SemaphoreSlim(1, 1);
        private readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

        public PreProcessManager(ISmidgeFileSystem fileSystem, IBundleManager bundleManager, ILogger<PreProcessManager> logger)
        {
            _fileSystem = fileSystem;
            _bundleManager = bundleManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ProcessAndCacheFileAsync(IWebFile file, BundleOptions bundleOptions, BundleContext bundleContext)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Pipeline == null) throw new ArgumentNullException($"{nameof(file)}.Pipeline");

            await ProcessFile(file, _bundleManager.GetAvailableOrDefaultBundleOptions(bundleOptions, false), bundleContext);
        }

        private async Task ProcessFile(IWebFile file, BundleOptions bundleOptions, BundleContext bundleContext)
        {
            //If Its external throw an exception this is not allowed. 
            if (file.FilePath.Contains(SmidgeConstants.SchemeDelimiter))
            {
                throw new InvalidOperationException("Cannot process an external file as part of a bundle");
            }

            if (file.FilePath.IndexOfAny(_invalidPathChars) != -1)
            {
                throw new InvalidOperationException("Cannot process paths with invalid chars");
            }

            await _processFileSemaphore.WaitAsync();

            try
            {
                await ProcessFileImpl(file, bundleOptions, bundleContext);
            }
            finally
            {
                _processFileSemaphore.Release();
            }
        }

        private async Task ProcessFileImpl(IWebFile file, BundleOptions bundleOptions, BundleContext bundleContext)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (bundleOptions == null) throw new ArgumentNullException(nameof(bundleOptions));
            if (bundleContext == null) throw new ArgumentNullException(nameof(bundleContext));

            var extension = Path.GetExtension(file.FilePath);

            var fileWatchEnabled = bundleOptions.FileWatchOptions.Enabled;

            var cacheBusterValue = bundleContext.CacheBusterValue;

            //we're making this lazy since we don't always want to resolve it
            var sourceFile = new Lazy<IFileInfo>(() => _fileSystem.GetRequiredFileInfo(file), LazyThreadSafetyMode.None);

            var cacheFile = _fileSystem.CacheFileSystem.GetCacheFile(file, () => sourceFile.Value, fileWatchEnabled, extension, cacheBusterValue, out var filePath);

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
                    await _fileSystem.CacheFileSystem.WriteFileAsync(filePath, processed);
                }
                else
                {
                    // we can just write the the file as-is to the cache file
                    var contents = await _fileSystem.ReadContentsAsync(sourceFile.Value);
                    await _fileSystem.CacheFileSystem.WriteFileAsync(filePath, contents);
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
        private static void FileModified(WatchedFile file)
        {
            //Raise the event on the file watch options
            file.BundleOptions.FileWatchOptions.Changed(new FileWatchEventArgs(file));
        }
    }
}