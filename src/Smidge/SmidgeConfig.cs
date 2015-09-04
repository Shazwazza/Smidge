using System;
using Microsoft.Framework.Configuration;
using Microsoft.Dnx.Runtime;

namespace Smidge
{
    public static class ConfigurationExtensions
    {
        public static bool GetBool(this IConfiguration config, string key)
        {
            var val = config[key];
            bool output;
            if (bool.TryParse(val, out output))
            {
                return output;
            }
            throw new InvalidCastException("Could not parse value " + val + " to a boolean");
        }
    }

    /// <summary>
    /// Smidge configuration
    /// </summary>
    public class SmidgeConfig : ISmidgeConfig
    {
        public SmidgeConfig(IApplicationEnvironment appEnv)
        {
            var cfg = new ConfigurationBuilder(appEnv.ApplicationBasePath)
                 //.AddEnvironmentVariables()                    
                .AddJsonFile("Smidge.json");

            _config = cfg.Build();
        }

        private readonly IConfigurationRoot _config;

        public string ServerName
        {
            get
            {
                return GetFileSafeMachineName(_config["COMPUTERNAME"] ?? "Default");
            }           
        }

        public bool IsDebug
        {
            get
            {
				return _config.GetBool("debug");
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