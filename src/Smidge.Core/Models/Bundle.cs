using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            _files = ImmutableList.CreateRange(files);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="files"></param>
        /// <param name="bundleOptions"></param>
        public Bundle(string name, List<IWebFile> files, BundleEnvironmentOptions bundleOptions)
        {
            Name = name;
            _files = ImmutableList.CreateRange(files);
            BundleOptions = bundleOptions;
        }

        /// <summary>
        /// Backing store for <see cref="Files"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="Bundle"/> instance is stored as shared/singleton state within the <see cref="IBundleManager"/>
        /// and can be mutated concurrently from multiple requests (e.g. via <see cref="ISmidgeRequire"/>). Bundle
        /// registration is comparatively rare (typically happening once at startup, or a handful of times per view
        /// render) while reads of the file list happen on almost every request. An immutable list updated via a
        /// lock-free compare-and-swap (<see cref="ImmutableInterlocked.Update{T}(ref ImmutableList{T}, Func{ImmutableList{T}, ImmutableList{T}})"/>)
        /// suits this read-heavy/write-light pattern well: readers always observe a complete, unchanging snapshot
        /// with zero synchronization cost, and writers never block readers or each other for long since collisions
        /// only cause a cheap retry rather than a lock wait.
        /// </remarks>
        private ImmutableList<IWebFile> _files;

        public string Name { get; }

        /// <summary>
        /// Gets a thread-safe, point-in-time snapshot of the files in this bundle
        /// </summary>
        /// <remarks>
        /// This is always a complete, immutable snapshot - it is safe to enumerate even while other threads
        /// are concurrently adding files via <see cref="AddFile"/>. Use <see cref="AddFile"/> to add files;
        /// this collection cannot be mutated directly.
        /// </remarks>
        public IReadOnlyList<IWebFile> Files => _files;

        /// <summary>
        /// Adds a file to this bundle in a thread-safe manner
        /// </summary>
        /// <param name="file"></param>
        public void AddFile(IWebFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            ImmutableInterlocked.Update(ref _files, (current, f) => current.Add(f), file);
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