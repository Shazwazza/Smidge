using System;

namespace Smidge.Options
{
    /// <summary>
    /// Used to build up bundle options in fluent syntax
    /// </summary>
    public sealed class BundleEnvironmentOptionsBuilder
    {
        private readonly BundleEnvironmentOptions _bundleEnvironmentOptions;
        private Action<BundleOptionsBuilder> _debugBuilder;
        private Action<BundleOptionsBuilder> _productionBuilder;
        private bool _built = false;

        public BundleEnvironmentOptionsBuilder(BundleEnvironmentOptions bundleEnvironmentOptions)
        {
            _bundleEnvironmentOptions = bundleEnvironmentOptions;
        }

        public BundleEnvironmentOptionsBuilder ForDebug(Action<BundleOptionsBuilder> debugBuilder)
        {
            if (debugBuilder == null) throw new ArgumentNullException(nameof(debugBuilder));
            _debugBuilder = debugBuilder;
            return this;
        }

        public BundleEnvironmentOptionsBuilder ForProduction(Action<BundleOptionsBuilder> productionBuilder)
        {
            if (productionBuilder == null) throw new ArgumentNullException(nameof(productionBuilder));
            _productionBuilder = productionBuilder;
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
                if (_debugBuilder != null)
                {
                    _debugBuilder(new BundleOptionsBuilder(_bundleEnvironmentOptions.DebugOptions));
                }
                if (_productionBuilder != null)
                {
                    _productionBuilder(new BundleOptionsBuilder(_bundleEnvironmentOptions.ProductionOptions));
                }
            }
            return _bundleEnvironmentOptions;
        }
    }
}