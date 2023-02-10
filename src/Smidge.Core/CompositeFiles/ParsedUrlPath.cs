using System.Collections.Generic;
using Smidge.Models;

namespace Smidge.CompositeFiles
{
    /// <summary>
    /// The parsed result of the request path made when retrieving a bundle
    /// </summary>
    public class ParsedUrlPath
    {
        /// <summary>
        /// The cache buster value used when the URL was generated
        /// </summary>
        public string CacheBusterValue { get; set; }

        /// <summary>
        /// If the request is in debug mode
        /// </summary>
        public bool Debug { get; set; }

        public IEnumerable<string> Names { get; set; }

        public WebFileType WebType { get; set; }
    }
}
