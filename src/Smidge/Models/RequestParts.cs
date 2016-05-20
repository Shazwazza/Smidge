using Microsoft.AspNetCore.Http;

namespace Smidge.Models
{
    public class RequestParts
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
    }
}