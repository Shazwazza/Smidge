using Smidge.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Smidge.CompositeFiles
{
    public interface IUrlManager
    {
        string GetUrl(string bundleName, string fileExtension, bool debug);

        IEnumerable<FileSetUrl> GetUrls(IEnumerable<IWebFile> dependencies, string fileExtension);

        ParsedUrlPath ParsePath(string input);
    }

}