using Smidge.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Smidge.CompositeFiles
{
    /// <summary>
    /// The parsed result of the request path made when retrieving a bundle
    /// </summary>
    public class ParsedUrlPath
    {
        public WebFileType WebType { get; set; }
        public IEnumerable<string> Names { get; set; }
        public string Version { get; set; }

        /// <summary>
        /// If the request is in debug mode
        /// </summary>
        public bool Debug { get; set; }
    }
}