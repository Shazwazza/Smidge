using System;
using System.Globalization;

namespace Smidge.Cache
{
    /// <summary>
    /// Creates a cache bust value for the lifetime of the app domain
    /// </summary>
    /// <remarks>
    /// Essentially means that all caches will be busted when the app restarts
    /// </remarks>
    public class AppDomainLifetimeCacheBuster : ICacheBuster
    {
        public AppDomainLifetimeCacheBuster()
        {
            _value = new Lazy<string>(() => DateTime.UtcNow.Ticks.ToString(NumberFormatInfo.InvariantInfo));
        }

        private static Lazy<string> _value;

        public string GetValue()
        {
            return _value.Value;
        }

        /// <summary>
        /// Since the cache will be busted on every restart we don't want to persist the files to the server, they will just be stored in memory
        /// </summary>
        public bool PersistProcessedFiles => false;
    }
}