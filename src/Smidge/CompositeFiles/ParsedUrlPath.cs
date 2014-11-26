using Smidge.Files;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Smidge.CompositeFiles
{
    public class ParsedUrlPath
    {
        public WebFileType WebType { get; set; }
        public IEnumerable<string> Names { get; set; }
        public string Version { get; set; }
    }
}