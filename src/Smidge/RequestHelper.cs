using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Smidge.Models;

namespace Smidge
{
    public class RequestHelper : IRequestHelper
    {
        private readonly IWebsiteInfo _siteInfo;

        public RequestHelper(IWebsiteInfo siteInfo)
        {            
            if (siteInfo == null) throw new ArgumentNullException(nameof(siteInfo));
            _siteInfo = siteInfo;
        }

        public bool IsExternalRequestPath(string path)
        {
            if ((path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("//", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        public string Content(IWebFile file)
        {
            if (string.IsNullOrEmpty(file.FilePath))
                return null;

            //if this is a protocol-relative/protocol-less uri, then we need to add the protocol for the remaining
            // logic to work properly
            if (file.FilePath.StartsWith("//"))
            {
                var scheme = _siteInfo.GetBaseUrl().Scheme;
                return Regex.Replace(file.FilePath, @"^\/\/", string.Format("{0}{1}", scheme, Constants.SchemeDelimiter));
            }

            var filePath = Content(file.FilePath);
            if (filePath == null) return null;

            var requestPath = file.RequestPath != null ? Content(file.RequestPath) : string.Empty;
#if NETCORE3_0
            if (requestPath.EndsWith('/'))
            {
                requestPath.TrimEnd('/');
            }
#else
            if (requestPath.EndsWith("/"))
            {
                requestPath.TrimEnd(new[] { '/' });
            }
#endif

            return string.Concat(requestPath, filePath);
        }

        /// <summary>
        /// Converts a virtual (relative) path to an application absolute path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string Content(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            //if this is a protocol-relative/protocol-less uri, then we need to add the protocol for the remaining
            // logic to work properly
            if (path.StartsWith("//"))
            {
                var scheme = _siteInfo.GetBaseUrl().Scheme;
                return Regex.Replace(path, @"^\/\/", string.Format("{0}{1}", scheme, Constants.SchemeDelimiter));
            }

            //This code is taken from the UrlHelper code ... which shouldn't need to be tucked away in there
            // since it is not dependent on the ActionContext           
            if (path[0] == 126)
            {
                PathString pathBase = _siteInfo.GetBasePath();
                return pathBase.Add(new PathString(path.Substring(1))).Value;
            }
                
            return path;
        }

        /// <summary>
        /// Check what kind of compression to use. Need to select the first available compression 
        /// from the header value as this is how .Net performs caching by compression so we need to follow
        /// this process.
        /// If IE 6 is detected, we will ignore compression as it's known that some versions of IE 6
        /// have issues with it.
        /// </summary>
        public CompressionType GetClientCompression(IHeaderDictionary headers)
        {
            var type = CompressionType.none;
            var agentHeader = (string)headers[HttpConstants.UserAgent];
            if (agentHeader != null && agentHeader.Contains("MSIE 6"))
            {
                return type;
            }

            string acceptEncoding = headers[HttpConstants.AcceptEncoding];

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