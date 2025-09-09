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
        public AppDomainLifetimeCacheBuster() => s_value = new Lazy<string>(() => DateTime.UtcNow.Ticks.ToString(NumberFormatInfo.InvariantInfo));

        private static Lazy<string> s_value;

        public string GetValue() => s_value.Value;
    }
}
