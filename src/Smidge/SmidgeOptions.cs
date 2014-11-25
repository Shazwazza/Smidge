using System;

namespace Smidge
{
    public sealed class SmidgeOptions
    {
        /// <summary>
        /// Constructor sets defaults
        /// </summary>
        public SmidgeOptions()
        {
            DefaultCssMinifier = new CssHelper();
            DefaultJavaScriptMinifier = new JsMin();
            Hasher = new Crc32Hasher();
        }

        public IMinifier DefaultCssMinifier { get; set; }
        public IMinifier DefaultJavaScriptMinifier { get; set; }

        public IHasher Hasher { get; set; }
    }
}