using System;
using System.IO;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.PlatformAbstractions;

namespace Smidge
{
    /// <summary>
    /// Smidge configuration
    /// </summary>
    public class SmidgeConfig : ISmidgeConfig
    {
        public SmidgeConfig(IConfiguration configuration)
        {
            _config = configuration;
        }

        public SmidgeConfig(IApplicationEnvironment env)
        {

            //  use smidge.json file if it exists for backwards compatibility.
            var smidgeConfigFilePath = Path.Combine(env.ApplicationBasePath, "smidge.json");
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