using NUglify.Css;

namespace Smidge.Nuglify
{
    public sealed class NuglifySettings
    {
        public NuglifySettings(INuglifyCodeSettings jsCodeSettings, CssSettings cssSettings)
        {
            JsCodeSettings = jsCodeSettings;
            CssCodeSettings = cssSettings;
        }

        public INuglifyCodeSettings JsCodeSettings { get; }
        public CssSettings CssCodeSettings { get; }
    }
}