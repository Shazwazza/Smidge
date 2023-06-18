using Smidge.CompositeFiles;
using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Smidge.Models
{
    /// <summary>
    /// Base class model for an inbound request
    /// </summary>
    public abstract class RequestModel : IRequestModel
    {
        protected RequestModel(string valueName, IUrlManager urlManager, IActionContextAccessor accessor, IRequestHelper requestHelper)
        {
            if (string.IsNullOrWhiteSpace(valueName)) throw new ArgumentException("message", nameof(valueName));
            if (urlManager is null) throw new ArgumentNullException(nameof(urlManager));
            if (accessor is null)throw new ArgumentNullException(nameof(accessor));
            if (requestHelper is null)throw new ArgumentNullException(nameof(requestHelper));

            //default 
            LastFileWriteTime = DateTime.MinValue;

            Compression = requestHelper.GetClientCompression(accessor.ActionContext.HttpContext.Request.Headers);

            var bundleId = (string)accessor.ActionContext.RouteData.Values[valueName];
            ParsedPath = urlManager.ParsePath(bundleId);

            if (ParsedPath == null)
            {
                IsBundleFound = false;
                return;
            }

            Debug = ParsedPath.Debug;

            switch (ParsedPath.WebType)
            {
                case WebFileType.Js:
                    Extension = ".js";
                    Mime = "text/javascript";
                    break;
                case WebFileType.Css:
                default:
                    Extension = ".css";
                    Mime = "text/css";
                    break;
            }
        }

        /// <summary>
        /// The bundle definition name - this is either the bundle name when using named bundles or the composite file
        /// key generated when using composite files
        /// </summary>
        public abstract string FileKey { get; }

        public bool Debug { get; }
        public ParsedUrlPath ParsedPath { get; }

        /// <summary>
        /// The compression type allowed by the client/browser for this request
        /// </summary>
        public CompressionType Compression { get; private set; }

        public string Extension { get; private set; }
        public string Mime { get; private set; }

        public DateTime LastFileWriteTime { get; set; }

        public bool IsBundleFound { get; set; } = true;
    }
}
