namespace Smidge.Nuglify
{
    public sealed class NuglifySettings
    {
        public NuglifySettings(INuglifyCodeSettings jsCodeSettings, INuglifyCodeSettings cssCodeSettings)
        {
            JsCodeSettings = jsCodeSettings;
            CssCodeSettings = cssCodeSettings;
        }

        public INuglifyCodeSettings JsCodeSettings { get; }
        public INuglifyCodeSettings CssCodeSettings { get; }
    }
}