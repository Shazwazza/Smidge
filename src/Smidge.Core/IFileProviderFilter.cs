using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Smidge
{
    public interface IFileProviderFilter
    {
        IEnumerable<string> GetMatchingFiles(IFileProvider fileProvider, string filePattern);
    }
}
