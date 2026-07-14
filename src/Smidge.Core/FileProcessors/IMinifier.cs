using Smidge.Models;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// A marker interface for pre-processors that minify web file content.
    /// </summary>
    /// <remarks>
    /// This allows Smidge to remain agnostic of the actual minification engine used. The default
    /// pipeline and conventions resolve minifiers via this interface rather than concrete types.
    /// </remarks>
    public interface IMinifier : IPreProcessor
    {
        /// <summary>
        /// The type of web file this minifier can process.
        /// </summary>
        WebFileType FileType { get; }
    }
}
