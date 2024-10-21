using System;
using System.Collections.Generic;

namespace Smidge.Options
{
    /// <summary>
    /// Used to build up bundle options in fluent syntax
    /// </summary>
    public sealed class BundleEnvironmentOptionsBuilder
    {
        private readonly BundleEnvironmentOptions _bundleEnvironmentOptions;

        private readonly Dictionary<string, Action<BundleOptionsBuilder>> _profileBuilders;
        private readonly bool _built = false;

        public BundleEnvironmentOptionsBuilder(BundleEnvironmentOptions bundleEnvironmentOptions)
        {
            _profileBuilders = new Dictionary<string, Action<BundleOptionsBuilder>>();

            _bundleEnvironmentOptions = bundleEnvironmentOptions;
        }

        public BundleEnvironmentOptionsBuilder ForDebug(Action<BundleOptionsBuilder> debugBuilder)
        {
            return ForProfile(SmidgeOptionsProfile.Debug, debugBuilder);
        }

        public BundleEnvironmentOptionsBuilder ForProduction(Action<BundleOptionsBuilder> productionBuilder)
        {
            return ForProfile(SmidgeOptionsProfile.Production, productionBuilder);
        }

        public BundleEnvironmentOptionsBuilder ForProfile(string profileName, Action<BundleOptionsBuilder> profileOptionsBuilder)
        {
            if (string.IsNullOrEmpty(profileName))
                throw new ArgumentNullException(nameof(profileName));

            if (profileOptionsBuilder == null)
                throw new ArgumentNullException(nameof(profileOptionsBuilder));

            _profileBuilders.Add(profileName, profileOptionsBuilder);
            return this;
        }
        
        /// <summary>
        /// Builds the bundle environment options based on the callbacks specified
        /// </summary>
        /// <returns></returns>
        public BundleEnvironmentOptions Build()
        {
            if (!_built)
            {
                foreach (var (profileName, profileBuilder) in _profileBuilders)
                {
                    BundleOptions options = _bundleEnvironmentOptions[profileName];
                    profileBuilder.Invoke(new BundleOptionsBuilder(options));
                }
            }
            return _bundleEnvironmentOptions;
        }
    }
}
