using System;
using System.Collections.Generic;
using Smidge.Models;

namespace Smidge.Models
{
    /// <summary>
    /// Represents a list of files for a bundle and actions the can be executed against the collection before pre-processing
    /// </summary>
    public class BundleFileCollection
    {
        public List<IWebFile> Files { get; }
        public Func<IEnumerable<IWebFile>, IEnumerable<IWebFile>> OrderingCallback { get; private set; }

        public BundleFileCollection(List<IWebFile> files)
        {
            Files = files;
        }

        /// <summary>
        /// A callback that can be specified 
        /// </summary>
        public void OnOrdering(Func<IEnumerable<IWebFile>, IEnumerable<IWebFile>> callback)
        {
            OrderingCallback = callback;
        }
    }
}