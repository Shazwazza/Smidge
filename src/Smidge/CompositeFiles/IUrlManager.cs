using Smidge.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Smidge.Cache;

namespace Smidge.CompositeFiles
{
    public interface IUrlManager
    {
        string GetUrl(string bundleName, string fileExtension, bool debug, ICacheBuster cacheBuster);

        IEnumerable<FileSetUrl> GetUrls(IEnumerable<IWebFile> dependencies, string fileExtension, ICacheBuster cacheBuster);

        ParsedUrlPath ParsePath(string input);
    }

}