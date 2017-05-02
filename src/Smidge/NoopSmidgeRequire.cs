using Smidge.Models;

namespace Smidge
{
    internal class NoopSmidgeRequire : ISmidgeRequire
    {
        public ISmidgeRequire RequiresJs(JavaScriptFile file)
        {
            return this;
        }

        public ISmidgeRequire RequiresJs(params string[] paths)
        {
            return this;
        }

        public ISmidgeRequire RequiresCss(CssFile file)
        {
            return this;
        }

        public ISmidgeRequire RequiresCss(params string[] paths)
        {
            return this;
        }
    }
}