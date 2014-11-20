using Singularity.Files;
using System;
using System.Collections.Generic;

namespace Singularity.CompositeFiles
{
    public interface IUrlCreator
    {
        IEnumerable<FileSetUrl> GetUrls(
            WebFileType type,
            IEnumerable<IWebFile> dependencies);
    }
}