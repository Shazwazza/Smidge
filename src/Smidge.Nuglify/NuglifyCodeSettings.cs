using NUglify.JavaScript;

namespace Smidge.Nuglify
{
    /// <summary>
    /// Wrapper for code settings that are used for JS
    /// </summary>
    public class NuglifyCodeSettings : INuglifyCodeSettings
    {
        public CodeSettings CodeSettings { get; }

        /// <summary>
        /// The type of source map to create (if any)
        /// </summary>
        public SourceMapType SourceMapType { get; set; } = SourceMapType.Default;

        public NuglifyCodeSettings()
        {
            CodeSettings = new CodeSettings();
        }
        public NuglifyCodeSettings(CodeSettings codeSettings)
        {
            CodeSettings = codeSettings ?? new CodeSettings();
        }
    }
}