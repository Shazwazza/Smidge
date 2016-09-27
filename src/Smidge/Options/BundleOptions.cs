namespace Smidge.Options
{
    /// <summary>
    /// Defines options for a particular bundle
    /// </summary>
    public sealed class BundleOptions
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public BundleOptions()
        {
            FileWatchOptions = new FileWatchOptions();
            CacheControlOptions = new CacheControlOptions();
            ProcessAsCompositeFile = true;
            CompressResult = true;
        }

        /// <summary>
        /// If set to true, will process the bundle as composite files and combine them into a single file
        /// </summary>
        /// <remarks>
        /// Generally when using normal css or js and in debug mode this will be set to false and Smidge will
        /// just set the URLs for these files as their normal static file locations, however when processing scripts and
        /// styles such as TypeScript or Sass, then even in debug mode this would need to be set to true otherwise 
        /// the pre-processors wont execute
        /// </remarks>
        public bool ProcessAsCompositeFile { get; set; }

        /// <summary>
        /// Whether to add compression to the response
        /// </summary>
        public bool CompressResult { get; set; }

        /// <summary>
        /// Used to control the caching of the bundle
        /// </summary>
        public CacheControlOptions CacheControlOptions { get; set; }

        /// <summary>
        /// Options for file watching to re-process them if they are modified on disk
        /// </summary>
        public FileWatchOptions FileWatchOptions { get; set; }
    }
}