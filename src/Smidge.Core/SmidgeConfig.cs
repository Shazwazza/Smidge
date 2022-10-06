using Microsoft.Extensions.Configuration;
using System.IO;

namespace Smidge
{
    /// <summary>
    /// Smidge configuration
    /// </summary>
    public class SmidgeConfig : ISmidgeConfig
    {
        /// <summary>
        /// Constructor that will use an IConfiguration instance to fetch configuration values from
        /// </summary>
        /// <param name="configuration"></param>
        public SmidgeConfig(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>
        /// Constructor that will use a smidge.json configuration file in env.ApplicationBasePath
        /// </summary>
        /// <param name="env"></param>
        public SmidgeConfig()
        {
            //  use smidge.json file if it exists for backwards compatibility.
            var cfg = new ConfigurationBuilder()
              .AddJsonFile("smidge.json");
            _config = cfg.Build();
        }

        private readonly IConfiguration _config;
        
        public string Version => _config["version"] ?? "1";

        public string DataFolder => (_config["dataFolder"] ?? "Smidge").Replace('/', Path.DirectorySeparatorChar);

        public bool KeepFileExtensions => bool.Parse((_config["keepFileExtensions"] ?? "false"));

    }
}