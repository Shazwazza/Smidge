using Smidge.FileProcessors;
using Smidge.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Smidge.Options
{
    public class Bundles
    {

        private readonly ConcurrentDictionary<string, List<IWebFile>> _bundles = new ConcurrentDictionary<string, List<IWebFile>>();

        public IEnumerable<string> GetBundleNames(WebFileType type)
        {
            return _bundles.Where(x => x.Value.Any(f => f.DependencyType == type)).Select(x => x.Key);
        }

        /// <summary>
        /// Gets/sets the pipeline factory
        /// </summary>
        /// <remarks>
        /// This will be set with the BundlesSetup class
        /// </remarks>
        public PreProcessPipelineFactory PipelineFactory { get; set; }

        public void Create(string bundleName, params JavaScriptFile[] jsFiles)
        {
            _bundles.TryAdd(bundleName, new List<IWebFile>(jsFiles));
        }

        public void Create(string bundleName, params CssFile[] cssFiles)
        {
            _bundles.TryAdd(bundleName, new List<IWebFile>(cssFiles));
        }

        public void Create(string bundleName, WebFileType type, params string[] paths)
        {
            _bundles.TryAdd(
                bundleName,
                type == WebFileType.Css
                ? paths.Select(x => (IWebFile)new CssFile(x)).ToList()
                : paths.Select(x => (IWebFile)new JavaScriptFile(x)).ToList());
        }

        public void Create(string bundleName, PreProcessPipeline pipeline, params JavaScriptFile[] jsFiles)
        {
            foreach (var file in jsFiles)
            {
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline;
                }
            }
            _bundles.TryAdd(bundleName, new List<IWebFile>(jsFiles));
        }

        public void Create(string bundleName, PreProcessPipeline pipeline, params CssFile[] cssFiles)
        {
            foreach (var file in cssFiles)
            {
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline;
                }
            }
            _bundles.TryAdd(bundleName, new List<IWebFile>(cssFiles));
        }

        public void Create(string bundleName, PreProcessPipeline pipeline, WebFileType type, params string[] paths)
        {
            _bundles.TryAdd(
                bundleName,
                type == WebFileType.Css
                ? paths.Select(x => (IWebFile)new CssFile(x) { Pipeline = pipeline }).ToList()
                : paths.Select(x => (IWebFile)new JavaScriptFile(x) { Pipeline = pipeline }).ToList());
        }

        public bool TryGetValue(string key, out List<IWebFile> value)
        {
            return _bundles.TryGetValue(key, out value);
        }
    }
}