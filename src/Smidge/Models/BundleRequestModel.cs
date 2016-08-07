using Smidge.CompositeFiles;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Smidge.Models
{

    /// <summary>
    /// Model for an inbound request for a bundle
    /// </summary>
    public class BundleRequestModel : RequestModel
    {
        public BundleRequestModel(IUrlManager urlManager, IActionContextAccessor accessor, IRequestHelper requestHelper)
            : base("bundle", urlManager, accessor, requestHelper)
        {
            if (!ParsedPath.Names.Any())
            {
                throw new InvalidOperationException("The bundle route value does not contain a bundle name");
            }

            FileKey = ParsedPath.Names.Single();

        }

        public override string FileKey { get; }
    }
}