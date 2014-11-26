using Smidge.Files;
using System;
using System.Collections.Generic;

namespace Smidge.CompositeFiles
{
    public interface IUrlManager
    {
        string GetUrl(string bundleName, string fileExtension);

        IEnumerable<FileSetUrl> GetUrls(IEnumerable<IWebFile> dependencies, string fileExtension);

        ParsedUrlPath ParsePath(string input);
    }

}