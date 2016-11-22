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
