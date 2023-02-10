using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smidge.Models;

namespace Smidge
{
    public class RequestHelper : IRequestHelper
    {
        private readonly IWebsiteInfo _siteInfo;

        public RequestHelper(IWebsiteInfo siteInfo)
        {
            _siteInfo = siteInfo ?? throw new ArgumentNullException(nameof(siteInfo));
        }

        public bool IsExternalRequestPath(string path)
        {
            if ((path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("//", StringComparison.OrdinalIgnoreCase)))
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
                return Regex.Replace(file.FilePath, @"^\/\/", scheme + SmidgeConstants.SchemeDelimiter);
            }

            var filePath = Content(file.FilePath);
            if (filePath == null)
                return null;

            var requestPath = file.RequestPath != null ? Content(file.RequestPath) : string.Empty;

            if (requestPath.EndsWith('/'))
            {
                requestPath.TrimEnd('/');
            }

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
                return Regex.Replace(path, @"^\/\/", scheme + SmidgeConstants.SchemeDelimiter);
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
        public CompressionType GetClientCompression(IDictionary<string, StringValues> headers)
        {
            var type = CompressionType.None;

            if (headers is not IHeaderDictionary headerDictionary)
            {
                headerDictionary = new HeaderDictionary(headers.Count);
                foreach ((var key, StringValues stringValues) in headers)
                {
                    headerDictionary[key] = stringValues;
                }
            }

            var acceptEncoding = headerDictionary.GetCommaSeparatedValues(HeaderNames.AcceptEncoding);
            if (acceptEncoding.Length > 0)
            {
                // Prefer in order: Brotli, GZip, Deflate.
                // https://www.iana.org/assignments/http-parameters/http-parameters.xml#http-content-coding-registry
                for (var i = 0; i < acceptEncoding.Length; i++)
                {
                    var encoding = acceptEncoding[i].Trim();

                    CompressionType parsed = CompressionType.Parse(encoding);

                    // Brotli is typically last in the accept encoding header.
                    if (parsed == CompressionType.Brotli)
                    {
                        return CompressionType.Brotli;
                    }

                    // Not pack200-gzip.
                    if (parsed == CompressionType.GZip)
                    {
                        type = CompressionType.GZip;
                    }

                    if (type != CompressionType.GZip && parsed == CompressionType.Deflate)
                    {
                        type = CompressionType.Deflate;
                    }
                }
            }

            return type;
        }
    }
}
