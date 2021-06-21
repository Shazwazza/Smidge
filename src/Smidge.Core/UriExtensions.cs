using System;

namespace Smidge
{
    public static class UriExtensions
    {
        internal static string ToAbsolutePath(this Uri originalUri, string path)
        {
            var hashSplit = path.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

            return string.Format(@"{0}{1}",
                                 (path.InvariantIgnoreCaseStartsWith("http://")
                                 || path.InvariantIgnoreCaseStartsWith("https://")
                                 || path.InvariantIgnoreCaseStartsWith("//")) ? path : new Uri(originalUri, path).PathAndQuery,
                                 hashSplit.Length > 1 ? ("#" + hashSplit[1]) : "");
        }

        /// <summary>
        /// Checks if the url is a local/relative uri, if it is, it makes it absolute based on the 
        /// current request uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="baseUrl">The base URL of the website</param>
        /// <returns></returns>
        public static Uri MakeAbsoluteUri(this Uri uri, Uri baseUrl)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));
            if (!baseUrl.IsAbsoluteUri) throw new ArgumentException(nameof(baseUrl) + " must be an absolute URI");

            if (!uri.IsAbsoluteUri)
            {
                var left = baseUrl.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);

                var absoluteUrl = new Uri(new Uri(left), uri);
                return absoluteUrl;
            }
            return uri;
        }
    }
}