using Microsoft.Extensions.FileProviders;
using Smidge.CompositeFiles;
using Smidge.Options;

namespace Smidge.Models
{
    public class WatchedFile
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public WatchedFile(IWebFile webFile, IFileInfo fileInfo, BundleOptions bundleOptions)
        {
            WebFile = webFile;
            FileInfo = fileInfo;
            BundleOptions = bundleOptions;
        }

        public IWebFile WebFile { get; }
        public IFileInfo FileInfo { get; }
        public BundleOptions BundleOptions { get; }
        
    }
}