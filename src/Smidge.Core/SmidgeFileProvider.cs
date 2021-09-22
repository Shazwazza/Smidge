using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Smidge.Tests")]

namespace Smidge
{
    public class SmidgeFileProvider : ISmidgeFileProvider
    {
        private readonly CompositeFileProvider _compositeFileProvider;

        public SmidgeFileProvider(params IFileProvider[] fileProviders)
        {
            _compositeFileProvider = new CompositeFileProvider(fileProviders);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
            => _compositeFileProvider.GetDirectoryContents(subpath);

        public IFileInfo GetFileInfo(string subpath)
            => _compositeFileProvider.GetFileInfo(subpath);

        public IChangeToken Watch(string filter)
            => _compositeFileProvider.Watch(filter);
    }
}
