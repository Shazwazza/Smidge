using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;

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
        public SmidgeConfig(IApplicationEnvironment env)
        {
            //  use smidge.json file if it exists for backwards compatibility.
            var cfg = new ConfigurationBuilder()
              .SetBasePath(env.ApplicationBasePath)
              //.AddEnvironmentVariables()                    
              .AddJsonFile("smidge.json");
            _config = cfg.Build();
        }

        private readonly IConfiguration _config;

        public string ServerName
        {
            get
            {
                return GetFileSafeMachineName(_config["COMPUTERNAME"] ?? "Default");
            }
        }

        public string Version
        {
            get
            {
                return _config["version"] ?? "1";
            }
        }

        public string DataFolder
        {
            get
            {
                return (_config["dataFolder"] ?? "App_Data/Smidge").Replace("/", "\\");
            }
        }

        private string GetFileSafeMachineName(string name)
        {
            return name.ReplaceNonAlphanumericChars('-');
        }
    }
}