using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Smidge.Models
{
    public class RequestHelper : IRequestHelper
    {
        private readonly string _scheme;
        private readonly PathString _pathBase;
        private readonly IHeaderDictionary _headers;
        private readonly HttpRequest _request;

        public RequestHelper(HttpRequest request)
        {
            _request = request;
        }

        public RequestHelper(string scheme, PathString pathBase, IHeaderDictionary headers)
        {
            _scheme = scheme;
            _pathBase = pathBase;
            _headers = headers;
        }

        private string Scheme => _request != null ? _request.Scheme : _scheme;
        private PathString PathBase => _request?.PathBase ?? _pathBase;
        private IHeaderDictionary Headers => _request != null ? _request.Headers : _headers;

        public static bool IsExternalRequestPath(string path)
        {
            if ((path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("//", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Converts a virtual (relative) path to an application absolute path.
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
            if (String.IsNullOrEmpty(path))
                return (string)null;
            if ((int)path[0] == 126)
                return PathBase.Add(new PathString(path.Substring(1))).Value;
            return path;
        }

        /// <summary>
        /// Check what kind of compression to use. Need to select the first available compression 
        /// from the header value as this is how .Net performs caching by compression so we need to follow
        /// this process.
        /// If IE 6 is detected, we will ignore compression as it's known that some versions of IE 6
        /// have issues with it.
        /// </summary>
        public CompressionType GetClientCompression()
        {
            var type = CompressionType.none;
            var agentHeader = (string)Headers[HttpConstants.UserAgent];
            if (agentHeader != null && agentHeader.Contains("MSIE 6"))
            {
                return type;
            }

            string acceptEncoding = Headers[HttpConstants.AcceptEncoding];

            if (!string.IsNullOrEmpty(acceptEncoding))
            {
                string[] supported = acceptEncoding.Split(',');
                //get the first type that we support
                for (var i = 0; i < supported.Length; i++)
                {
                    if (supported[i].Contains("deflate"))
                    {
                        type = CompressionType.deflate;
                        break;
                    }
                    else if (supported[i].Contains("gzip")) //sometimes it could be x-gzip!
                    {
                        type = CompressionType.gzip;
                        break;
                    }
                }
            }

            return type;
        }
    }
}