using Smidge.Models;
using System.Collections.Generic;

namespace Smidge.CompositeFiles
{
    public interface IUrlManager
    {
        string GetUrl(string bundleName, string fileExtension, bool debug, string cacheBusterValue);

        IEnumerable<FileSetUrl> GetUrls(IEnumerable<IWebFile> dependencies, string fileExtension, string cacheBusterValue);

        ParsedUrlPath ParsePath(string input);
    }

}