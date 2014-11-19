using System;
using Microsoft.Framework.ConfigurationModel;

namespace Singularity
{
    public class SingularityConfig : Configuration, IConfiguration
    {
        public string ServerName
        {
            get
            {
                return GetFileSafeMachineName(Get("COMPUTERNAME") ?? "Default");
            }           
        }

        public bool IsDebug
        {
            get
            {
                return this.Get<bool>("debug");
            }
        }

        public string Version
        {
            get
            {
                return Get("version") ?? "1";
            }
        }

        public string DataFolder
        {
            get
            {
                return (Get("dataFolder") ?? "App_Data/Singularity").Replace("/", "\\");
            }
        }

        public SingularityConfig()
        {
            this.AddJsonFile("singularity.json");
            this.AddEnvironmentVariables();
        }

        private string GetFileSafeMachineName(string name)
        {
            return name.ReplaceNonAlphanumericChars('-');
        }
    }
}