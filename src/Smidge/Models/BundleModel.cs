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
        public BundleModel(IUrlManager urlManager, IActionContextAccessor accessor)
            : base("bundle", urlManager, accessor)
        {
            if (!ParsedPath.Names.Any())
            {
                throw new InvalidOperationException("The bundle route value does not contain a bundle name");
            }

            _bundleName = ParsedPath.Names.Single();

        }

        private string _bundleName;

        public override string FileKey
        {
            get
            {
                return _bundleName;
            }
        }
    }
}