using Singularity.Files;
using System;
using System.Collections.Generic;

namespace Singularity.CompositeFiles
{
    public interface IUrlCreator
    {
        IEnumerable<string> GetUrls(
            IDependentFileType type,
            IEnumerable<IDependentFile> dependencies);
    }
}