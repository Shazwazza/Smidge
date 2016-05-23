using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Smidge.Models
{
    public class RequestParts : IVirtualPathTranslator
    {
        public RequestParts(HttpRequest request)
        {
            Path = request.Path;
            Scheme = request.Scheme;
            Host = request.Host;
            PathBase = request.PathBase;
            QueryString = request.QueryString;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public RequestParts(PathString path, string scheme, HostString host, PathString pathBase, QueryString queryString)
        {
            Path = path;
            Scheme = scheme;
            Host = host;
            PathBase = pathBase;
            QueryString = queryString;
        }

        public PathString Path { get; }
        public string Scheme { get; }
        public HostString Host { get; }
        public PathString PathBase { get; }
        public QueryString QueryString { get; }

        /// <summary>
        /// This will normalize the web path - synonymous with IUrlHelper.Content method but does not require IUrlHelper
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string Content(string path)
        {
            //if this is a protocol-relative/protocol-less uri, then we need to add the protocol for the remaining
            // logic to work properly
            if (path.StartsWith("//"))
            {
                return Regex.Replace(path, @"^\/\/", string.Format("{0}{1}", Scheme, Constants.SchemeDelimiter));
            }

            //This code is taken from the UrlHelper code ... which shouldn't need to be tucked away in there
            // since it is not dependent on the ActionContext
            if (string.IsNullOrEmpty(path))
                return (string)null;
            if ((int)path[0] == 126)
                return PathBase.Add(new PathString(path.Substring(1))).Value;
            return path;
        }
    }
}