using System;

namespace Smidge
{
    public interface IWebsiteInfo
    {
        /// <summary>
        /// returns the base URI of the website
        /// </summary>
        Uri GetBaseUrl();

        /// <summary>
        /// returns the base path of the website (i.e. virtual directory path)
        /// </summary>
        string GetBasePath();
    }
}