using System;

namespace Smidge.Files
{
    public class WebFilePair
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