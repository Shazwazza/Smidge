using Smidge.CompositeFiles;
using Smidge.FileProcessors;
using System;

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
        }

        public UrlManagerOptions UrlOptions { get; set; }
    }
}