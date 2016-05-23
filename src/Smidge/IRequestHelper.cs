namespace Smidge
{
    /// <summary>
    /// Used to transform a virtual path to an absolute path
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
        CompressionType GetClientCompression();
    }
}