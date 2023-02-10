using System;
using System.IO.Compression;
using Smidge.Cache;

namespace Smidge.Options
{
    

    /// <summary>
    /// Defines options for a particular bundle
    /// </summary>
    public sealed class BundleOptions
    {
        /// <summary>
        /// Constructor with default values
        /// </summary>
        public BundleOptions()
        {
            FileWatchOptions = new FileWatchOptions();
            CacheControlOptions = new CacheControlOptions();
            ProcessAsCompositeFile = true;
            CompressResult = true;
            CompressionLevel = CompressionLevel.Optimal;
        }

        private Type _defaultCacheBuster;

        /// <summary>
        /// Sets the default cache buster type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>
        /// This instance will be resolved from IoC at runtime
        /// </remarks>
        public void SetCacheBusterType<T>()
            where T: ICacheBuster
        {
            _defaultCacheBuster = typeof(T);
        }

        /// <summary>
        /// Sets the default cache buster type
        /// </summary>
        /// <remarks>
        /// This instance will be resolved from IoC at runtime
        /// </remarks>
        public void SetCacheBusterType(Type t)
        {
            if (!typeof(ICacheBuster).IsAssignableFrom(t))
            {
                throw new InvalidOperationException($"The type {t} is not of type {typeof(ICacheBuster)}");
            }
            _defaultCacheBuster = t;
        }

        /// <summary>
        /// Returns the default cache buster type
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// By default this is the ConfigCacheBuster
        /// </remarks>
        public Type GetCacheBusterType()
        {
            return _defaultCacheBuster ?? typeof(ConfigCacheBuster);
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
        /// The compression level of the bundle
        /// </summary>
        public CompressionLevel CompressionLevel {get; set; }

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