using Smidge.CompositeFiles;
using System;

namespace Smidge.Options
{
    /// <summary>
    /// Allows developers to specify custom options on startup
    /// </summary>
    public sealed class SmidgeOptions
    {
        /// <summary>
        /// Constructor sets defaults
        /// </summary>
        public SmidgeOptions()
        {
            DefaultCssMinifier = typeof(CssHelper);
            DefaultJavaScriptMinifier = typeof(JsMin);
            UrlOptions = new UrlManagerOptions();
        }

        private Type _defaultCssMinifier;
        private Type _defaultJavaScriptMinifier;

        public Type DefaultCssMinifier
        {
            get
            {
                return _defaultCssMinifier;
            }
            set
            {
                if (!typeof(IMinifier).IsAssignableFrom(value))
                {
                    throw new InvalidOperationException("The type specified is not convertable to " + typeof(IMinifier));
                }
                _defaultCssMinifier = value;
            }
        }
        public Type DefaultJavaScriptMinifier
        {
            get
            {
                return _defaultJavaScriptMinifier;
            }
            set
            {
                if (!typeof(IMinifier).IsAssignableFrom(value))
                {
                    throw new InvalidOperationException("The type specified is not convertable to " + typeof(IMinifier));
                }
                _defaultJavaScriptMinifier = value;
            }
        }

        public UrlManagerOptions UrlOptions { get; set; }
    }
}