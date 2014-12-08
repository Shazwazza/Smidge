using System;

namespace Smidge
{
    internal class HttpConstants
    {
        internal const string IfNoneMatch = "If-None-Match";
        internal const string IfModifiedSince = "If-Modified-Since";
        internal const string LastModified = "Last-Modified";
        internal const string Expires = "Expires";
        internal const string CacheControl = "Cache-Control";
        internal const string ETag = "ETag";
        internal const string ContentEncoding = "Content-encoding";
        internal const string UserAgent = "User-Agent";
        internal const string AcceptEncoding = "Accept-Encoding";
        internal const string HttpDateFormat = "r";
        internal const int NotModified304 = 304;
    }
}