using System.Collections.Generic;
using Smidge.Models;

namespace Smidge.CompositeFiles
{
    public interface IUrlManager
    {
        /// <summary>
        /// Appends the cache buster value to a non-bundle request
        /// </summary>
        string AppendCacheBuster(string url, bool debug, string cacheBusterValue);

        string GetUrl(string bundleName, string fileExtension, bool debug, string cacheBusterValue);

        IEnumerable<FileSetUrl> GetUrls(IEnumerable<IWebFile> dependencies, string fileExtension, string cacheBusterValue);

        ParsedUrlPath ParsePath(string input);
    }
}
