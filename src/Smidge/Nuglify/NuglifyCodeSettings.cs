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

        public NuglifyCodeSettings() : this(null)
        {
        }

        public NuglifyCodeSettings(CodeSettings codeSettings)
        {
            CodeSettings = codeSettings ?? new CodeSettings();
            if (codeSettings == null)
            {
                // NUglify incorrectly reports valid JavaScript regex literals such as the
                // escape pattern used by Ace's Verilog mode as JS1013.
                CodeSettings.SetIgnoreErrors(new[] { "JS1013" });
            }
        }
    }
}