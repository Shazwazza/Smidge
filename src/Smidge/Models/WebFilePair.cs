using System;

namespace Smidge.Models
{
    internal class WebFilePair
    {
        public WebFilePair(IWebFile original, IWebFile hashed)
        {
            Original = original;
            Hashed = hashed;
        }
        public IWebFile Original { get; private set; }
        public IWebFile Hashed { get; private set; }
    }
}