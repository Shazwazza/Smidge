using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using System;
using System.Collections.Generic;

namespace Smidge.Options
{
    /// <summary>
    /// Allows developers to specify custom options on startup
    /// </summary>
    public sealed class SmidgeOptions
    {
        /// <summary>
        /// Constructor sets defaults
        /// </summary>
        public SmidgeOptions()
        {
            UrlOptions = new UrlManagerOptions();
            FileProcessingConventions = new FileProcessingConventionsCollection
            {
                typeof(MinifiedFilePathConvention)
            };
        }

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
        public ICollection<Type> FileProcessingConventions { get; private set; }
    }
}