namespace Smidge.Options
{
    /// <summary>
    /// Defines the different bundle options for Debug vs Production
    /// </summary>
    public sealed class BundleEnvironmentOptions
    {
        /// <summary>
        /// Creates a new Options Builder
        /// </summary>
        /// <returns></returns>
        public static BundleEnvironmentOptionsBuilder Create()
        {
            var options = new BundleEnvironmentOptions();
            return new BundleEnvironmentOptionsBuilder(options);
        }

        /// <summary>
        /// Constructor, sets default options
        /// </summary>
        public BundleEnvironmentOptions()
        {
            DebugOptions = new BundleOptions
            {
                ProcessAsCompositeFile = false,
                CompressResult = false,
                CacheControlOptions = new CacheControlOptions
                {
                    EnableETag = false,
                    CacheControlMaxAge = 0
                }
            };
            ProductionOptions = new BundleOptions();    
        }

        /// <summary>
        /// The options for debug mode
        /// </summary>
        public BundleOptions DebugOptions { get; set; }

        /// <summary>
        /// The options for production mode
        /// </summary>
        public BundleOptions ProductionOptions { get; set; }
    }
}