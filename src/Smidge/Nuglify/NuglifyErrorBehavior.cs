namespace Smidge.Nuglify
{
    /// <summary>
    /// Defines what happens when NUglify reports errors while minifying a file
    /// </summary>
    public enum NuglifyErrorBehavior
    {
        /// <summary>
        /// Logs a warning with the error details and uses the original (unminified) file content
        /// </summary>
        UseOriginal,

        /// <summary>
        /// Throws an exception with the error details, failing the bundle request
        /// </summary>
        Throw
    }
}
