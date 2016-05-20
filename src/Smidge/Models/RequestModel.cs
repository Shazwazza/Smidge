using Smidge.CompositeFiles;
using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Smidge.Models
{
    /// <summary>
    /// Base class model for an inbound request
    /// </summary>
    public abstract class RequestModel
    {
        protected RequestModel(string valueName, IUrlManager urlManager, IActionContextAccessor accessor)
        {
            //default 
            LastFileWriteTime = DateTime.Now;

            Compression = accessor.ActionContext.HttpContext.Request.GetClientCompression();

            var bundleId = (string)accessor.ActionContext.RouteData.Values[valueName];
            ParsedPath = urlManager.ParsePath(bundleId);

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

        public abstract string FileKey { get; }

        public ParsedUrlPath ParsedPath { get; private set; }
        public CompressionType Compression { get; private set; }
        public string Extension { get; private set; }
        public string Mime { get; private set; }

        public DateTime LastFileWriteTime { get; set; }
    }
}