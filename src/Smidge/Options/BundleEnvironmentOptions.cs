namespace Smidge.Options
{
    /// <summary>
    /// Defines the different bundle options for Debug vs Production
    /// </summary>
    public sealed class BundleEnvironmentOptions
    {
        public BundleEnvironmentOptions()
        {
            DebugOptions = new BundleOptions
            {
                ProcessAsCompositeFile = false,
                CompressResult = false
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