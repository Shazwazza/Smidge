using System.Collections.Generic;
using Smidge.FileProcessors;
using Smidge.Models;
using Smidge.Options;

namespace Smidge
{
    /// <summary>
    /// Used to read and modify bundles
    /// </summary>
    public interface IBundleManager
    {
        BundleEnvironmentOptions DefaultBundleOptions { get; }
        PreProcessPipelineFactory PipelineFactory { get; }

        Bundle Create(string bundleName, params JavaScriptFile[] jsFiles);
        Bundle Create(string bundleName, params CssFile[] cssFiles);
        Bundle Create(string bundleName, WebFileType type, params string[] paths);
        Bundle Create(string bundleName, PreProcessPipeline pipeline, params JavaScriptFile[] jsFiles);
        Bundle Create(string bundleName, PreProcessPipeline pipeline, params CssFile[] cssFiles);
        Bundle Create(string bundleName, PreProcessPipeline pipeline, WebFileType type, params string[] paths);
        bool TryGetValue(string key, out Bundle value);

        /// <summary>
        /// Returns all bundle names registered
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<string> GetBundleNames(WebFileType type);

        /// <summary>
        /// Checks if the bundle exists by name
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        bool Exists(string bundleName);

        /// <summary>
        /// Adds an item to the bundle, if the bundle doesn't exist it will be created
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="file"></param>
        void AddToBundle(string bundleName, CssFile file);

        /// <summary>
        /// Adds an item to the bundle, if the bundle doesn't exist it will be created
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="file"></param>
        void AddToBundle(string bundleName, JavaScriptFile file);

        /// <summary>
        /// Returns the bundle by name
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        Bundle GetBundle(string bundleName);
    }
}