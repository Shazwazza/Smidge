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
        private ConcurrentDictionary<string, IEnumerable<IWebFile>> _bundles = new ConcurrentDictionary<string, IEnumerable<IWebFile>>();

        public void Create(string bundleName, params JavaScriptFile[] jsFiles)
        {
            _bundles.TryAdd(bundleName, jsFiles);
        }

        public void Create(string bundleName, params CssFile[] cssFiles)
        {
            _bundles.TryAdd(bundleName, cssFiles);
        }

        public void Create(string bundleName, WebFileType type, params string[] paths)
        {
            _bundles.TryAdd(
                bundleName,
                type == WebFileType.Css
                ? paths.Select(x => (IWebFile)new CssFile(x))
                : paths.Select(x => (IWebFile)new JavaScriptFile(x)));
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
            _bundles.TryAdd(bundleName, jsFiles);
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
            _bundles.TryAdd(bundleName, cssFiles);
        }

        public void Create(string bundleName, PreProcessPipeline pipeline, WebFileType type, params string[] paths)
        {
            _bundles.TryAdd(
                bundleName,
                type == WebFileType.Css
                ? paths.Select(x => (IWebFile)new CssFile(x) { Pipeline = pipeline })
                : paths.Select(x => (IWebFile)new JavaScriptFile(x) { Pipeline = pipeline }));
        }

        public bool TryGetValue(string key, out IEnumerable<IWebFile> value)
        {
            return _bundles.TryGetValue(key, out value);
        }
    }
}