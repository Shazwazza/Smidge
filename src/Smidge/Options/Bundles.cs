using Smidge.FileProcessors;
using Smidge.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Smidge.Options
{
    public class Bundles
    {
        private readonly ConcurrentDictionary<string, BundleFileCollection> _bundles = new ConcurrentDictionary<string, BundleFileCollection>();

        public IEnumerable<string> GetBundleNames(WebFileType type)
        {
            return _bundles.Where(x => x.Value.Files.Any(f => f.DependencyType == type)).Select(x => x.Key);
        }

        /// <summary>
        /// Gets/sets the pipeline factory
        /// </summary>
        /// <remarks>
        /// This will be set with the BundlesSetup class
        /// </remarks>
        public PreProcessPipelineFactory PipelineFactory { get; set; }

        public BundleFileCollection Create(string bundleName, params JavaScriptFile[] jsFiles)
        {
            var collection = new BundleFileCollection(new List<IWebFile>(jsFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public BundleFileCollection Create(string bundleName, params CssFile[] cssFiles)
        {
            var collection = new BundleFileCollection(new List<IWebFile>(cssFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public BundleFileCollection Create(string bundleName, WebFileType type, params string[] paths)
        {
            var collection = type == WebFileType.Css
                ? new BundleFileCollection(paths.Select(x => (IWebFile) new CssFile(x)).ToList())
                : new BundleFileCollection(paths.Select(x => (IWebFile) new JavaScriptFile(x)).ToList());
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public BundleFileCollection Create(string bundleName, PreProcessPipeline pipeline, params JavaScriptFile[] jsFiles)
        {
            foreach (var file in jsFiles)
            {
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline;
                }
            }
            var collection = new BundleFileCollection(new List<IWebFile>(jsFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public BundleFileCollection Create(string bundleName, PreProcessPipeline pipeline, params CssFile[] cssFiles)
        {
            foreach (var file in cssFiles)
            {
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline;
                }
            }
            var collection = new BundleFileCollection(new List<IWebFile>(cssFiles));
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public BundleFileCollection Create(string bundleName, PreProcessPipeline pipeline, WebFileType type, params string[] paths)
        {
            var collection = type == WebFileType.Css
                ? new BundleFileCollection(paths.Select(x => (IWebFile)new CssFile(x) { Pipeline = pipeline }).ToList())
                : new BundleFileCollection(paths.Select(x => (IWebFile)new JavaScriptFile(x) { Pipeline = pipeline }).ToList());
            _bundles.TryAdd(bundleName, collection);
            return collection;
        }

        public bool TryGetValue(string key, out BundleFileCollection value)
        {
            BundleFileCollection collection;
            var val = _bundles.TryGetValue(key, out collection);
            value = val ? collection : null;
            return val;
        }
    }
}