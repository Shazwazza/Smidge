using System;
using Microsoft.Framework.ConfigurationModel;

namespace Singularity
{
    public class SingularityConfig : Configuration, IConfiguration
    {
        //TODO: Surely there is a better way to do this??
        private string _serverName;
        public string ServerName
        {
            get
            {
                return this.Get("COMPUTERNAME") ?? "Default";
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