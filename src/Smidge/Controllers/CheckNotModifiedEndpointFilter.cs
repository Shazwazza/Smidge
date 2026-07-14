using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
                var isDifferent = request.HasETagBeenModified(etag);
                var hasChanged = request.HasRequestBeenModifiedSince(file.LastFileWriteTime.ToUniversalTime());
                if (!isDifferent || !hasChanged)
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }
            }

            return result;
        }
    }
}
