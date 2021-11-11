using Microsoft.AspNetCore.Http;
using Smidge.Models;
using System;
using System.Globalization;
using Microsoft.Net.Http.Headers;

namespace Smidge
{
    public static class HttpExtensions
    {
        /// <summary>
        /// Checks if the incoming request is asking for content that has been changed based on the If-None-Match (ETag) header
        /// </summary>
        /// <param name="request"></param>
        /// <param name="etag"></param>
        /// <remarks>
        /// This is used to determine a 304 response code
        /// </remarks>
        public static bool HasETagBeenModified(this HttpRequest request, string etag)
        {
            var ifNoneMatch = request.Headers.GetCommaSeparatedValues(HeaderNames.IfNoneMatch);
            if (ifNoneMatch != null)
            {
                foreach (var segment in ifNoneMatch)
                {
                    if (segment.Equals("*", StringComparison.Ordinal) || segment.Equals(etag, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the incoming request is asking for content that has been changed based on the If-Modified-Since header
        /// </summary>
        /// <param name="request"></param>
        /// <param name="utcLastModified"></param>
        /// <remarks>
        /// This is used to determine a 304 response code
        /// </remarks>
        public static bool HasRequestBeenModifiedSince(this HttpRequest request, DateTime utcLastModified)
        {
            string ifModifiedSinceString = request.Headers[HeaderNames.IfModifiedSince];
            if (TryParseHttpDate(ifModifiedSinceString, out DateTime ifModifiedSince))
                return ifModifiedSince < utcLastModified;


            return true;
        }

        internal static bool TryParseHttpDate(string dateString, out DateTime parsedDate)
        {
            return DateTime.TryParseExact(dateString, HttpConstants.HttpDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
        }

        public static void AddLastModifiedResponseHeader(this HttpResponse response, RequestModel model)
        {
            response.Headers[HeaderNames.LastModified] = model.LastFileWriteTime.ToUniversalTime().ToString(HttpConstants.HttpDateFormat);
        }

        public static void AddExpiresResponseHeader(this HttpResponse response, int cacheHours = 10)
        {
            var dateTime = DateTime.UtcNow.AddHours(cacheHours);
            response.Headers[HeaderNames.Expires] = dateTime.ToString(HttpConstants.HttpDateFormat);
        }

        public static void AddCacheControlResponseHeader(this HttpResponse response, int cacheHours = 10)
        {
            response.Headers[HeaderNames.CacheControl] = string.Format("public, max-age={0}, s-maxage={0}", TimeSpan.FromHours(cacheHours) .TotalSeconds);
        }

        public static void AddETagResponseHeader(this HttpResponse response, string etag)
        {
            response.Headers[HeaderNames.ETag] = $"\"{etag}\"";
        }

        public static void AddCompressionResponseHeader(this HttpResponse response, CompressionType cType)
        {
            if (cType != CompressionType.None)
            {
                response.Headers[HeaderNames.ContentEncoding] = cType.ToString();
                response.Headers[HeaderNames.Vary] = HeaderNames.AcceptEncoding;
            }
        }
    }
}
