using Smidge.Cache;

namespace Smidge.Options
{
    public sealed class CacheControlOptions
    {
        public CacheControlOptions()
        {
            EnableETag = true;
            CacheControlMaxAge = 10 * 24; //10 days
        }

        public bool EnableETag { get; set; }
        public int CacheControlMaxAge { get; set; }
    }
}