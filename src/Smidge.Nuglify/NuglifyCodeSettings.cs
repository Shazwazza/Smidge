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
        /// Whether or not to generate inline source maps
        /// </summary>
        public bool EnableSourceMaps { get; set; } = true;

        public NuglifyCodeSettings(CodeSettings codeSettings)
        {
            CodeSettings = codeSettings ?? new CodeSettings();
        }
    }
}