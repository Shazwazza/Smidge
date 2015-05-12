using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Smidge.CompositeFiles;
using System;
using System.Linq;

namespace Smidge.Models
{
    /// <summary>
    /// Base class model for an inbound request
    /// </summary>
    public abstract class RequestModel
    {
        public RequestModel(string valueName, IUrlManager urlManager, IScopedInstance<ActionContext> action)
        {
            //default 
            LastFileWriteTime = DateTime.Now;

            Compression = action.Value.HttpContext.Request.GetClientCompression();

            var bundleId = (string)action.Value.RouteData.Values[valueName];
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