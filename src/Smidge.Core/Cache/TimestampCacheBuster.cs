using System;
using Microsoft.Extensions.Hosting;

namespace Smidge.Cache
{
    /// <summary>
    /// When in DEBUG mode, cache bust for every request, else cache bust for the lifetime of the AppDomain.
    /// </summary>
    public class TimestampCacheBuster : ICacheBuster
    {
        private readonly AppDomainLifetimeCacheBuster _appDomainLifetimeCacheBuster = new AppDomainLifetimeCacheBuster();
        private readonly IHostEnvironment _hostEnvironment;

        public bool TimestampBased => _hostEnvironment.IsDevelopment();

        public TimestampCacheBuster(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public string GetValue()
        {
            if (_hostEnvironment.IsDevelopment())
            {
                // round to the nearest 5 seconds
                long roundedTicks = (DateTime.UtcNow.Ticks + 25000000) / 50000000 * 50000000;
                return roundedTicks.ToString();
            }
            else
            {
                return _appDomainLifetimeCacheBuster.GetValue();
            }
        }
    }
}
