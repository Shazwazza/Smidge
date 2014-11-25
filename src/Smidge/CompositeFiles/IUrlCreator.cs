using Smidge.Files;
using System;
using System.Collections.Generic;

namespace Smidge.CompositeFiles
{
    public interface IUrlCreator
    {
        IEnumerable<FileSetUrl> GetUrls(
            WebFileType type,
            IEnumerable<IWebFile> dependencies);
    }
}