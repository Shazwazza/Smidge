using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using System;
using System.Collections.Generic;
using Smidge.Cache;

namespace Smidge.Options
{
    /// <summary>
    /// The global options for Smidge
    /// </summary>
    public sealed class SmidgeOptions
    {
        public SmidgeOptions()
        {

        }
        
        /// <summary>
        /// Gets/sets the pipeline factory
        /// </summary>
        /// <remarks>
        /// This will be set with the BundlesSetup class
        /// </remarks>
        public PreProcessPipelineFactory PipelineFactory { get; set; }

        /// <summary>
        /// Gets/sets the default bundle options
        /// </summary>
        public BundleEnvironmentOptions DefaultBundleOptions { get; set; }

        /// <summary>
        /// Defines the URL options for Smidge bundle URLs
        /// </summary>
        public UrlManagerOptions UrlOptions { get; set; }

        /// <summary>
        /// Specifies the file processing conventions that Smidge will use
        /// </summary>
        /// <remarks>
        /// This acts like a filter, the actual instances of IFileProcessingConvention will be created via IoC
        /// </remarks>
        public ICollection<Type> FileProcessingConventions { get; set; }

        public SmidgeCacheOptions CacheOptions { get; set; }
    }
}