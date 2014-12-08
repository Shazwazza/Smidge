using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Smidge.CompositeFiles;
using System;
using System.Linq;

namespace Smidge.Models
{

    /// <summary>
    /// Model for an inbound request for a bundle
    /// </summary>
    public class BundleModel : RequestModel
    {
        private IContextAccessor<ActionContext> _action;
        private IUrlManager _urlManager;
        
        public BundleModel(IUrlManager urlManager, IContextAccessor<ActionContext> action)
        {
            _urlManager = urlManager;
            _action = action;

            Compression = _action.Value.HttpContext.Request.GetClientCompression();

            var bundleId = (string)_action.Value.RouteData.Values["bundle"];
            var parsed = _urlManager.ParsePath(bundleId);

            if (!parsed.Names.Any())
            {
                throw new InvalidOperationException("The bundle route value does not contain a bundle name");
            }

            BundleName = parsed.Names.Single();

            switch (parsed.WebType)
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

        public string BundleName { get; set; }
    }
}