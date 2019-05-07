//TODO: This cannot work! This is due to the behavior of Smidge relying so heavily on cached files and not input/output.
// this keeps getting called over and over to re-resolve a file path but this will change every time and not be consistent
// within the request which it would need to be. 

//using System;

//namespace Smidge.Cache
//{
//    /// <summary>
//    /// Cache bust on every request
//    /// </summary>
//    public class NoLifetimeCacheBuster : ICacheBuster
//    {
//        public string GetValue()
//        {
//            return DateTime.UtcNow.Ticks.ToString();
//        }

//        /// <summary>
//        /// Since the cache will be busted on every restart we don't want to persist the files to the server, they will just be stored in memory
//        /// </summary>
//        public bool PersistProcessedFiles => false;
//    }
//}