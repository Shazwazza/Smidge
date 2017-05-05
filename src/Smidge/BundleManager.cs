using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Smidge.Models;
using Smidge.Options;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Smidge.FileProcessors;

namespace Smidge
{
    /// <summary>
    /// Used to read and modify bundles
    /// </summary>
    public class BundleManager : IBundleManager
    {
        public BundleManager(IOptions<SmidgeOptions> smidgeOptions, ILogger<BundleManager> logger)
        {
            if (smidgeOptions == null) throw new ArgumentNullException(nameof(smidgeOptions));
            _smidgeOptions = smidgeOptions;
            _logger = logger;
        }

        private readonly IOptions<SmidgeOptions> _smidgeOptions;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, Bundle> _bundles = new ConcurrentDictionary<string, Bundle>();

        public BundleEnvironmentOptions DefaultBundleOptions => _smidgeOptions.Value.DefaultBundleOptions;

        public PreProcessPipelineFactory PipelineFactory => _smidgeOptions.Value.PipelineFactory;

        public Bundle Create(string bundleName, params JavaScriptFile[] jsFiles)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(bundleName));
            if (bundleName.Contains('.')) throw new ArgumentException("A bundle name cannot contain a '.' character");

            _logger.LogDebug($"Creating {WebFileType.Js} bundle '{bundleName}' with {jsFiles.Length} files");

            var collection = new Bundle(new List<IWebFile>(jsFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public Bundle Create(string bundleName, params CssFile[] cssFiles)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(bundleName));
            if (bundleName.Contains('.')) throw new ArgumentException("A bundle name cannot contain a '.' character");

            _logger.LogDebug($"Creating {WebFileType.Css} bundle '{bundleName}' with {cssFiles.Length} files");

            var collection = new Bundle(new List<IWebFile>(cssFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public Bundle Create(string bundleName, WebFileType type, params string[] paths)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(bundleName));
            if (bundleName.Contains('.')) throw new ArgumentException("A bundle name cannot contain a '.' character");

            _logger.LogDebug($"Creating {type} bundle '{bundleName}' with {paths.Length} files");

            var collection = type == WebFileType.Css
                ? new Bundle(paths.Select(x => (IWebFile)new CssFile(x)).ToList())
                : new Bundle(paths.Select(x => (IWebFile)new JavaScriptFile(x)).ToList());
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public Bundle Create(string bundleName, PreProcessPipeline pipeline, params JavaScriptFile[] jsFiles)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(bundleName));
            if (bundleName.Contains('.')) throw new ArgumentException("A bundle name cannot contain a '.' character");

            _logger.LogDebug($"Creating {WebFileType.Js} bundle '{bundleName}' with {jsFiles.Length} files and a custom pipeline");

            foreach (var file in jsFiles)
            {
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline;
                }
            }
            var collection = new Bundle(new List<IWebFile>(jsFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public Bundle Create(string bundleName, PreProcessPipeline pipeline, params CssFile[] cssFiles)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(bundleName));
            if (bundleName.Contains('.')) throw new ArgumentException("A bundle name cannot contain a '.' character");

            _logger.LogDebug($"Creating {WebFileType.Css} bundle '{bundleName}' with {cssFiles.Length} files and a custom pipeline");

            foreach (var file in cssFiles)
            {
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline;
                }
            }
            var collection = new Bundle(new List<IWebFile>(cssFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public Bundle Create(string bundleName, PreProcessPipeline pipeline, WebFileType type, params string[] paths)
        {
            if (string.IsNullOrWhiteSpace(bundleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(bundleName));
            if (bundleName.Contains('.')) throw new ArgumentException("A bundle name cannot contain a '.' character");

            _logger.LogDebug($"Creating {type} bundle '{bundleName}' with {paths.Length} files and a custom pipeline");

            var collection = type == WebFileType.Css
                ? new Bundle(paths.Select(x => (IWebFile)new CssFile(x) { Pipeline = pipeline }).ToList())
                : new Bundle(paths.Select(x => (IWebFile)new JavaScriptFile(x) { Pipeline = pipeline }).ToList());
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public bool TryGetValue(string key, out Bundle value)
        {
            Bundle collection;
            var val = _bundles.TryGetValue(key, out collection);
            value = val ? collection : null;
            return val;
        }

        /// <summary>
        /// Returns all bundle names registered
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<string> GetBundleNames(WebFileType type)
        {
            return _bundles.Where(x => x.Value.Files.Any(f => f.DependencyType == type)).Select(x => x.Key);
        }

        /// <summary>
        /// Checks if the bundle exists by name
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public bool Exists(string bundleName)
        {
            Bundle collection;
            return TryGetValue(bundleName, out collection);
        }

        /// <summary>
        /// Adds an item to the bundle, if the bundle doesn't exist it will be created
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="file"></param>
        public void AddToBundle(string bundleName, CssFile file)
        {
            Bundle collection;
            if (TryGetValue(bundleName, out collection))
            {
                _logger.LogDebug($"Adding {WebFileType.Css} file '{file.FilePath}' to bundle '{bundleName}'");
                collection.Files.Add(file);
            }
            else
            {
                Create(bundleName, file);
            }
        }

        /// <summary>
        /// Adds an item to the bundle, if the bundle doesn't exist it will be created
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="file"></param>
        public void AddToBundle(string bundleName, JavaScriptFile file)
        {
            Bundle collection;
            if (TryGetValue(bundleName, out collection))
            {
                _logger.LogDebug($"Adding {WebFileType.Js} file '{file.FilePath}' to bundle '{bundleName}'");
                collection.Files.Add(file);
            }
            else
            {
                Create(bundleName, file);
            }
        }

        /// <summary>
        /// Returns the bundle by name
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public Bundle GetBundle(string bundleName)
        {
            Bundle collection;
            if (!TryGetValue(bundleName, out collection))
                return null;
            return collection;
        }
    }
}