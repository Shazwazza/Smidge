using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Smidge.CompositeFiles;

namespace Smidge.Models
{
    /// <summary>
    /// Base class model for an inbound request
    /// </summary>
    public abstract class RequestModel : IRequestModel
    {
        protected RequestModel(string valueName, IUrlManager urlManager, IActionContextAccessor accessor, IRequestHelper requestHelper)
        {
            if (string.IsNullOrWhiteSpace(valueName))
                throw new ArgumentException("message", nameof(valueName));

#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(urlManager);
            ArgumentNullException.ThrowIfNull(accessor);
            ArgumentNullException.ThrowIfNull(requestHelper);
#else
            if (urlManager is null)
                throw new ArgumentNullException(nameof(urlManager));

            if (accessor is null)
                throw new ArgumentNullException(nameof(accessor));

            if (requestHelper is null)
                throw new ArgumentNullException(nameof(requestHelper));
#endif

            //default 
            LastFileWriteTime = DateTime.MinValue;

            Compression = requestHelper.GetClientCompression(accessor.ActionContext.HttpContext.Request.Headers);

            var bundleId = (string)accessor.ActionContext.RouteData.Values[valueName];
            ParsedPath = urlManager.ParsePath(bundleId);

            if (ParsedPath == null)
                throw new InvalidOperationException($"Could not parse {bundleId} as a valid smidge path");

            Debug = ParsedPath.Debug;

            switch (ParsedPath.WebType)
            {
                case WebFileType.Js:
                    Extension = ".js";
                    Mime = "text/javascript";
                    break;
                default:
                    Extension = ".css";
                    Mime = "text/css";
                    break;
            }
        }

        /// <summary>
        /// The compression type allowed by the client/browser for this request
        /// </summary>
        public CompressionType Compression { get; }

        public bool Debug { get; }

        public string Extension { get; }

        /// <summary>
        /// The bundle definition name - this is either the bundle name when using named bundles or the composite file
        /// key generated when using composite files
        /// </summary>
        public abstract string FileKey { get; }

        public DateTime LastFileWriteTime { get; set; }

        public string Mime { get; }

        public ParsedUrlPath ParsedPath { get; }
    }
}
