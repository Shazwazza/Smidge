using NUglify.Css;
using NUglify.JavaScript;
using System;
using System.ComponentModel;

namespace Smidge.Nuglify
{
    /// <summary>
    /// Wrapper for code settings that are used for JS
    /// </summary>
    public class NuglifyCodeSettings : INuglifyCodeSettings
    {
        public CodeSettings CodeSettings { get; }

        [Obsolete("This is not used and will be removed in future versions")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CssSettings CssSettings { get; }

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