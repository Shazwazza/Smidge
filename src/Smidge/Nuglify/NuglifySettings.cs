using NUglify.Css;

namespace Smidge.Nuglify
{
    public sealed class NuglifySettings
    {
        /// <summary>
        /// Creates settings using the default JS and CSS settings
        /// </summary>
        public NuglifySettings()
            : this(new NuglifyCodeSettings(), new CssSettings())
        {
        }

        public NuglifySettings(INuglifyCodeSettings jsCodeSettings, CssSettings cssSettings)
        {
            JsCodeSettings = jsCodeSettings ?? new NuglifyCodeSettings();
            CssCodeSettings = cssSettings ?? new CssSettings();
        }

        public INuglifyCodeSettings JsCodeSettings { get; set; }
        public CssSettings CssCodeSettings { get; set; }

        /// <summary>
        /// Gets/sets whether JS and CSS files are minified. Default is true.
        /// </summary>
        /// <remarks>
        /// When false, the NUglify pre-processors pass the file content through unchanged.
        /// </remarks>
        public bool EnableMinification { get; set; } = true;

        /// <summary>
        /// Gets/sets what happens when NUglify reports errors while minifying a file.
        /// Default is <see cref="NuglifyErrorBehavior.UseOriginal"/>.
        /// </summary>
        public NuglifyErrorBehavior ErrorBehavior { get; set; } = NuglifyErrorBehavior.UseOriginal;
    }
}
