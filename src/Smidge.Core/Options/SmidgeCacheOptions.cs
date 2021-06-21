namespace Smidge.Options
{
    public sealed class SmidgeCacheOptions
    {
        /// <summary>
        /// If true, the smidge cache will be in-memory only and not persisted to disk
        /// </summary>
        public bool UseInMemoryCache { get; set; }
    }
}