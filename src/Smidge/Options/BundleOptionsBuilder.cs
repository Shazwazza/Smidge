using Smidge.Cache;

namespace Smidge.Options
{
    /// <summary>
    /// Used to build up bundle options in fluent syntax
    /// </summary>
    public sealed class BundleOptionsBuilder
    {
        private readonly BundleOptions _options;

        public BundleOptionsBuilder(BundleOptions options)
        {
            _options = options;
        }

        public BundleOptionsBuilder SetCacheBusterType<T>()
            where T: ICacheBuster
        {
            _options.SetCacheBusterType<T>();
            return this;
        }

        public BundleOptionsBuilder EnableCompositeProcessing()
        {
            _options.ProcessAsCompositeFile = true;
            return this;
        }

        public BundleOptionsBuilder EnableFileWatcher()
        {
            _options.FileWatchOptions.Enabled = true;
            return this;
        }

        public BundleOptionsBuilder CacheControlOptions(bool enableEtag, int cacheControlMaxAge)
        {
            _options.CacheControlOptions.EnableETag = enableEtag;
            _options.CacheControlOptions.CacheControlMaxAge = cacheControlMaxAge;
            return this;
        }
    }
}