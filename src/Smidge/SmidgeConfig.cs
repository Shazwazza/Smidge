using System;
using Microsoft.Framework.ConfigurationModel;

namespace Smidge
{
    /// <summary>
    /// Smidge configuration
    /// </summary>
    public class SmidgeConfig 
    {
        private InternalConfig _config = new InternalConfig();

        public string ServerName
        {
            get
            {
                return GetFileSafeMachineName(_config.Get("COMPUTERNAME") ?? "Default");
            }           
        }

        public bool IsDebug
        {
            get
            {
                return _config.Get<bool>("debug");
            }
        }

        public string Version
        {
            get
            {
                return _config.Get("version") ?? "1";
            }
        }

        public string DataFolder
        {
            get
            {
                return (_config.Get("dataFolder") ?? "App_Data/Smidge").Replace("/", "\\");
            }
        }

        /// <summary>
        /// The internal configuration class that reads from the underlying files/environment
        /// </summary>
        private class InternalConfig : Configuration, IConfiguration
        {
            public InternalConfig()
            {
                this.AddJsonFile("Smidge.json");
                this.AddEnvironmentVariables();
            }
        }        

        private string GetFileSafeMachineName(string name)
        {
            return name.ReplaceNonAlphanumericChars('-');
        }
    }
}