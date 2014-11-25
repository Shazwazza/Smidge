using Smidge.Files;
using System;
using System.Collections.Generic;

namespace Smidge.CompositeFiles
{
    public interface IUrlCreator
    {
        string GetUrl(string bundleName, WebFileType type);

        IEnumerable<FileSetUrl> GetUrls(
            WebFileType type,
            IEnumerable<IWebFile> dependencies);
    }
}