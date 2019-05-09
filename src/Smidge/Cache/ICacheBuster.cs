using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smidge.Cache
{

    /// <summary>
    /// Returns the value to cache bust the request
    /// </summary>
    public interface ICacheBuster
    {
        /// <summary>
        /// Returns the string value used to cache bust the client request
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// this is also used to store the processed files on the server if PersistProcessedFiles = true
        /// </remarks>
        string GetValue();


        //TODO: This doesn't do anything!! In order for this setting to have any affect a pretty big overhaul of everything has to take place.
        // The problem is that Smidge relies heavily on a file system cache. For example, the PreProcessManager specifically writes the output
        // of processing to cache files, not returning the result so we can't not have a cache. In theory perhaps the 'easiest' way is to remove
        // this property and add a virtual cache, one that is a physical disk, one that is memory, etc... and one that is Request based. But this 
        // is still ugly! Since there would be not 'pass through' type. I think the whole processing needs to be re-designed.

        /// <summary>
        /// Returns true if the server will persist the processed files to disk so that the processing survives app restarts
        /// </summary>
        /// <remarks>
        /// If the value returned for the cache buster is not fairly static and this is set to true then you may end up with a lot
        /// of old persisted processed files on disk.
        /// </remarks>
        bool PersistProcessedFiles { get; }
    }
}
