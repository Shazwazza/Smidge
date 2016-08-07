using Microsoft.Extensions.FileProviders;

namespace Smidge.Models
{
    public class WatchedFile
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public WatchedFile(IWebFile webFile, IFileInfo fileInfo)
        {
            WebFile = webFile;
            FileInfo = fileInfo;
        }

        public IWebFile WebFile { get; }
        public IFileInfo FileInfo { get; }
    }
}