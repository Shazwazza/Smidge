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

        /// <summary>
        /// Used to synchronize mutation/enumeration of <see cref="Files"/> since a <see cref="Bundle"/> instance
        /// is stored as shared/singleton state within the <see cref="IBundleManager"/> and can be mutated
        /// concurrently from multiple requests (e.g. via <see cref="ISmidgeRequire"/>).
        /// </summary>
        private readonly object _filesLock = new object();

        public string Name { get; }

        /// <summary>
        /// Gets the list of files in this bundle
        /// </summary>
        /// <remarks>
        /// This collection is not thread-safe on its own. Use <see cref="AddFile"/> to mutate it and
        /// <see cref="GetFilesSnapshot"/> to safely enumerate it when the bundle may be mutated concurrently.
        /// </remarks>
        public List<IWebFile> Files { get; }

        /// <summary>
        /// Adds a file to this bundle in a thread-safe manner
        /// </summary>
        /// <param name="file"></param>
        public void AddFile(IWebFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            lock (_filesLock)
            {
                Files.Add(file);
            }
        }

        /// <summary>
        /// Returns a thread-safe snapshot of the current files in this bundle
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IWebFile> GetFilesSnapshot()
        {
            lock (_filesLock)
            {
                return Files.ToList();
            }
        }

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