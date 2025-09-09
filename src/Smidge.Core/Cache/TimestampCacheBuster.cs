using System;

namespace Smidge.Cache
{
    /// <summary>
    /// When in DEBUG mode, cache bust for every request, else in RELEASE mode, cache bust for the lifetime of the AppDomain.
    /// </summary>
    public class TimestampCacheBuster : ICacheBuster
    {
#if RELEASE
        private readonly AppDomainLifetimeCacheBuster _appDomainLifetimeCacheBuster = new AppDomainLifetimeCacheBuster();

        public string GetValue() => _appDomainLifetimeCacheBuster.GetValue();
#else
        public string GetValue()
        {
            // round to the nearest 5 seconds
            long roundedTicks = (DateTime.UtcNow.Ticks + 25000000) / 50000000 * 50000000;
            return roundedTicks.ToString();
        }
#endif

    }
}
