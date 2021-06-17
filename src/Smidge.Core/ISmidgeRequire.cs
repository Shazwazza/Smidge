using Smidge.Models;

namespace Smidge
{
    public interface ISmidgeRequire
    {
        ISmidgeRequire RequiresJs(JavaScriptFile file);
        ISmidgeRequire RequiresJs(params string[] paths);
        ISmidgeRequire RequiresCss(CssFile file);
        ISmidgeRequire RequiresCss(params string[] paths);
    }
}