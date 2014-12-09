using Microsoft.AspNet.Http;
using System;
using Microsoft.AspNet.WebUtilities;

namespace Smidge
{
    public static class UriExtensions
    {
        /// <summary>
        /// Checks if the url is a local/relative uri, if it is, it makes it absolute based on the 
        /// current request uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public static Uri MakeAbsoluteUri(this Uri uri, HttpRequest req)
        {
            if (!uri.IsAbsoluteUri)
            {
                if (req.Path.HasValue)
                {
                    var uriHelper = new UriHelper(req);
                    var fullUri = uriHelper.GetFullUri();
                    var reqUri = new Uri(fullUri);

                    var left = reqUri.GetComponents(UriComponents.StrongAuthority, UriFormat.SafeUnescaped);

                    var absoluteUrl = new Uri(new Uri(left), uri);
                    return absoluteUrl;
                }
            }
            return uri;
        }
    }
}