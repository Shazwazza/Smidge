using Microsoft.AspNetCore.Http;

namespace Smidge
{
    /// <summary>
    /// Utility class for working with current requests and path info
    /// </summary>
    public interface IRequestHelper
    {
        /// <summary>
        /// Converts a virtual (relative) path to an application absolute path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string Content(string path);

        /// <summary>
        /// Returns the compression type for the current request
        /// </summary>
        /// <returns></returns>
        CompressionType GetClientCompression(IHeaderDictionary headers);

        bool IsExternalRequestPath(string path);
    }
}