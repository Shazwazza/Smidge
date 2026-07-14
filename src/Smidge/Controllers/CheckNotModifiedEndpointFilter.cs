using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Smidge.Hashing;
using Smidge.Models;

namespace Smidge.Controllers
{
    /// <summary>
    /// Checks the request headers to see if the response has been modified, if it has not a 304 is returned and the request is short circuited
    /// </summary>
    internal sealed class CheckNotModifiedEndpointFilter : IEndpointFilter
    {
        private readonly IHasher _hasher;

        public CheckNotModifiedEndpointFilter(IHasher hasher)
            => _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));

        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);

            if (context.Arguments.OfType<RequestModel>().FirstOrDefault() is RequestModel file && file.IsBundleFound)
            {
                //Don't execute when the request is in Debug
                if (file.Debug)
                    return result;

                var etag = _hasher.Hash(file.FileKey + file.Compression + file.Mime);

                var request = context.HttpContext.Request;

                // Per RFC 7232, If-None-Match takes precedence over If-Modified-Since: when an
                // If-None-Match header is present the If-Modified-Since header must be ignored,
                // otherwise a mismatched ETag combined with an unchanged date could wrongly 304.
                bool notModified;
                if (request.Headers.ContainsKey(HeaderNames.IfNoneMatch))
                {
                    notModified = !request.HasETagBeenModified(etag);
                }
                else
                {
                    notModified = !request.HasRequestBeenModifiedSince(file.LastFileWriteTime.ToUniversalTime());
                }

                if (notModified)
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }
            }

            return result;
        }
    }
}
