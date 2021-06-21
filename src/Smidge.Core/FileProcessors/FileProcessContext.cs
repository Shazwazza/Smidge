using Smidge.Models;
using System;
using Smidge.CompositeFiles;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// The context operated on by a <see cref="IPreProcessor"/>
    /// </summary>
    public class FileProcessContext
    {
        public FileProcessContext(string fileContent, IWebFile webFile, BundleContext bundleContext)
        {
            if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));
            if (webFile == null) throw new ArgumentNullException(nameof(webFile));

            FileContent = fileContent;
            WebFile = webFile;
            BundleContext = bundleContext;
        }

        /// <summary>
        /// Updates the file content to be bundled
        /// </summary>
        /// <param name="fileContent"></param>
        public void Update(string fileContent)
        {
            FileContent = fileContent;
        }

        /// <summary>
        /// Gets the current processed file content
        /// </summary>
        public string FileContent { get; private set; }

        /// <summary>
        /// Gets the <see cref="IWebFile"/>
        /// </summary>
        public IWebFile WebFile { get; }

        /// <summary>
        /// Gets the <see cref="IBundleContext"/>
        /// </summary>
        public BundleContext BundleContext { get; }
        
    }
}