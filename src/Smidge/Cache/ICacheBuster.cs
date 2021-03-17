namespace Smidge.Cache
{

    /// <summary>
    /// Returns the value to cache bust the request
    /// </summary>
    public interface ICacheBuster
    {
        /// <summary>
        /// Returns the string value used to cache bust the client request
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// this is also used to store the processed files on the server if PersistProcessedFiles = true
        /// </remarks>
        string GetValue();
    }
}
