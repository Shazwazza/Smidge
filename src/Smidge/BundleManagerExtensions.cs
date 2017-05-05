using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge
{
    /// <summary>
    /// Helpful extension methods to make creating bundles a bit simpler
    /// </summary>
    public static class BundleManagerExtensions
    {
        /// <summary>
        /// Create a JS bundle
        /// </summary>
        /// <param name="bundleManager"></param>
        /// <param name="bundleName"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static Bundle CreateJs(this IBundleManager bundleManager, string bundleName, params string[] paths)
        {
            return bundleManager.Create(bundleName, WebFileType.Js, paths);
        }

        /// <summary>
        /// Create a JS bundle
        /// </summary>
        /// <param name="bundleManager"></param>
        /// <param name="bundleName"></param>
        /// <param name="pipeline"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static Bundle CreateJs(this IBundleManager bundleManager, string bundleName, PreProcessPipeline pipeline, params string[] paths)
        {
            return bundleManager.Create(bundleName, pipeline, WebFileType.Js, paths);
        }

        /// <summary>
        /// Create a CSS bundle
        /// </summary>
        /// <param name="bundleManager"></param>
        /// <param name="bundleName"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static Bundle CreateCss(this IBundleManager bundleManager, string bundleName, params string[] paths)
        {
            return bundleManager.Create(bundleName, WebFileType.Css, paths);
        }

        /// <summary>
        /// Create a CSS bundle
        /// </summary>
        /// <param name="bundleManager"></param>
        /// <param name="bundleName"></param>
        /// <param name="pipeline"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static Bundle CreateCss(this IBundleManager bundleManager, string bundleName, PreProcessPipeline pipeline, params string[] paths)
        {
            return bundleManager.Create(bundleName, pipeline, WebFileType.Css, paths);
        }
    }
}