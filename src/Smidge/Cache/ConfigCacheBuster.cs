using System;
using System.Collections;

namespace Smidge.Cache
{
    /// <summary>
    /// Based on a static string specified in config
    /// </summary>
    public class ConfigCacheBuster : ICacheBuster
    {
        private readonly ISmidgeConfig _config;

        public ConfigCacheBuster(ISmidgeConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _config = config;
        }
        public string GetValue()
        {
            return _config.Version;
        }
    }
}