using System;

namespace Smidge
{
    public class AutoWebsiteInfo : IWebsiteInfo
    {
        public bool IsConfigured { get; private set; } = false;
        public string BasePath { get; private set; }
        public Uri BaseUrl { get; private set; }

        /// <summary>
        /// Configures the instance one time
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="baseUrl"></param>
        public void ConfigureOnce(string basePath, Uri baseUrl)
        {
            if (IsConfigured) return;
            BasePath = basePath;
            BaseUrl = baseUrl;
            IsConfigured = true;
        }
    }

    public interface IWebsiteInfo
    {
        /// <summary>
        /// returns the base URI of the website
        /// </summary>
        Uri BaseUrl { get; }

        /// <summary>
        /// returns the base path of the website (i.e. virtual directory path)
        /// </summary>
        string BasePath { get; }
    }
}