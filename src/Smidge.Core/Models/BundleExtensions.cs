using System;
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
        [Obsolete("Use GetBundleOptions(IBundleManager, string) and specify a configuration profile name.")]
        public static BundleOptions GetBundleOptions(this Bundle bundle, IBundleManager bundleMgr, bool debug)
        {
            var bundleOptions = debug
                ? GetBundleOptions(bundle, bundleMgr, SmidgeOptionsProfile.Debug)
                : GetBundleOptions(bundle, bundleMgr, SmidgeOptionsProfile.Default);

            return bundleOptions;
        }
        
        /// <summary>
        /// Get the bundle options from the bundle if they have been set otherwise with the defaults
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="bundleMgr"></param>
        /// <param name="profileName"></param>
        /// <returns></returns>
        public static BundleOptions GetBundleOptions(this Bundle bundle, IBundleManager bundleMgr, string profileName)
        {
            var bundleOptions = bundle.BundleOptions == null
                ? bundleMgr.DefaultBundleOptions[profileName]
                : bundle.BundleOptions[profileName];

            return bundleOptions;
        }




        /// <summary>
        /// Gets the default bundle options based on whether we're in debug or not
        /// </summary>
        /// <param name="bundleMgr"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        [Obsolete("Use GetDefaultBundleOptions(IBundleManager, string) and specify a configuration profile name.")]
        public static BundleOptions GetDefaultBundleOptions(this IBundleManager bundleMgr, bool debug)
        {
            var bundleOptions = debug
                ? bundleMgr.DefaultBundleOptions.DebugOptions
                : bundleMgr.DefaultBundleOptions.ProductionOptions;

            return bundleOptions;
        }


        /// <summary>
        /// Gets the default bundle options for a particular configuration profile.
        /// </summary>
        /// <param name="bundleMgr"></param>
        /// <param name="profileName">The name of a configuration profile.</param>
        /// <returns></returns>
        public static BundleOptions GetDefaultBundleOptions(this IBundleManager bundleMgr, string profileName)
        {
            var bundleOptions = bundleMgr.DefaultBundleOptions[profileName];

            return bundleOptions;
        }





        [Obsolete("Use GetAvailableOrDefaultBundleOptions(BundleOptions, string) and specify a configuration profile name.")]
        public static BundleOptions GetAvailableOrDefaultBundleOptions(this IBundleManager bundleMgr, BundleOptions options, bool debug)
        {
            return GetAvailableOrDefaultBundleOptions(bundleMgr, options, debug ? SmidgeOptionsProfile.Debug : SmidgeOptionsProfile.Default);
        }


        public static BundleOptions GetAvailableOrDefaultBundleOptions(this IBundleManager bundleMgr, BundleOptions options)
        {
            return GetAvailableOrDefaultBundleOptions(bundleMgr, options, SmidgeOptionsProfile.Default);
        }

        public static BundleOptions GetAvailableOrDefaultBundleOptions(this IBundleManager bundleMgr, BundleOptions options, string profileName)
        {
            return options != null
                ? options
                : bundleMgr.GetDefaultBundleOptions(profileName);
        }


    }
}
