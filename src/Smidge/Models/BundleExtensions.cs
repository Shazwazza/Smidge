using Smidge.Options;

namespace Smidge.Models
{
    public static class BundleExtensions
    {
        /// <summary>
        /// Get the bundle options from the bundle if they have been set otherwise with the defaults
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="bundleMgr"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        public static BundleOptions GetBundleOptions(this Bundle bundle, IBundleManager bundleMgr, bool debug)
        {
            var bundleOptions = debug
                ? (bundle.BundleOptions == null ? bundleMgr.DefaultBundleOptions.DebugOptions : bundle.BundleOptions.DebugOptions)
                : (bundle.BundleOptions == null ? bundleMgr.DefaultBundleOptions.ProductionOptions : bundle.BundleOptions.ProductionOptions);

            return bundleOptions;
        }

        /// <summary>
        /// Gets the default bundle options based on whether we're in debug or not
        /// </summary>
        /// <param name="bundleMgr"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        public static BundleOptions GetDefaultBundleOptions(this IBundleManager bundleMgr, bool debug)
        {
            var bundleOptions = debug
                ? bundleMgr.DefaultBundleOptions.DebugOptions
                : bundleMgr.DefaultBundleOptions.ProductionOptions;

            return bundleOptions;
        }
    }
}