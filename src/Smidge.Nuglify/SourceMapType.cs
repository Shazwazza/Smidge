namespace Smidge.Nuglify
{
    public enum SourceMapType
    {
        /// <summary>
        /// Will not produce source maps
        /// </summary>
        None,

        /// <summary>
        /// Will produce an external source map file
        /// </summary>
        Default,

        /// <summary>
        /// Will produce inline source maps
        /// </summary>
        Inline
    }
}