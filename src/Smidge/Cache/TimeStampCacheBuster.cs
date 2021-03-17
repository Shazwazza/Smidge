using System;
using System.Globalization;

namespace Smidge.Cache
{
    /// <summary>
    /// Cache bust for every request
    /// </summary>
    public class TimeStampCacheBuster : ICacheBuster
    {
        public string GetValue()
        {
            // round to the nearest 5 seconds
            long roundedTicks = ((DateTime.UtcNow.Ticks + 25000000) / 50000000) * 50000000;
            return roundedTicks.ToString();
        }
    }
}