using System;
using System.Collections.Generic;
using System.Linq;
using Smidge.Models;
using Smidge.Options;

namespace Smidge.Models
{
    /// <summary>
    /// Defines a bundle, its list of files and actions the can be executed against the collection before pre-processing
    /// </summary>
    public class Bundle
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="files"></param>
        public Bundle(string name, List<IWebFile> files)
        {
            Name = name;
            Files = files;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="files"></param>
        /// <param name="bundleOptions"></param>
        public Bundle(string name, List<IWebFile> files, BundleEnvironmentOptions bundleOptions)
        {
            Name = name;
            Files = files;
            BundleOptions = bundleOptions;
        }

        public string Name { get; }

        /// <summary>
        /// Gets the list of files in this bundle
        /// </summary>
        public List<IWebFile> Files { get; }

        /// <summary>
        /// An optional callback used to do custom ordering
        /// </summary>
        public Func<IEnumerable<IWebFile>, IEnumerable<IWebFile>> OrderingCallback { get; private set; }

        /// <summary>
        /// Defines the options for this bundle
        /// </summary>
        public BundleEnvironmentOptions BundleOptions { get; private set; }        

        /// <summary>
        /// Sets the options for the bundle
        /// </summary>
        /// <param name="bundleOptions"></param>
        public Bundle WithEnvironmentOptions(BundleEnvironmentOptions bundleOptions)
        {
            BundleOptions = bundleOptions;
            return this;
        }
        

        /// <summary>
        /// A callback that can be specified 
        /// </summary>
        public Bundle OnOrdering(Func<IEnumerable<IWebFile>, IEnumerable<IWebFile>> callback)
        {
            OrderingCallback = callback;
            return this;
        }
    }

    
}