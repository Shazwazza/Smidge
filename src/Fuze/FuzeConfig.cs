using System;
using Microsoft.Framework.ConfigurationModel;

namespace Fuze
{
    /// <summary>
    /// Fuze configuration
    /// </summary>
    public class FuzeConfig 
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
                return (_config.Get("dataFolder") ?? "App_Data/Fuze").Replace("/", "\\");
            }
        }

        /// <summary>
        /// The internal configuration class that reads from the underlying files/environment
        /// </summary>
        private class InternalConfig : Configuration, IConfiguration
        {
            public InternalConfig()
            {
                this.AddJsonFile("fuze.json");
                this.AddEnvironmentVariables();
            }
        }        

        private string GetFileSafeMachineName(string name)
        {
            return name.ReplaceNonAlphanumericChars('-');
        }
    }
}