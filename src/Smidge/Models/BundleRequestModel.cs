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
        public BundleRequestModel(IUrlManager urlManager, IActionContextAccessor accessor, IRequestHelper requestHelper, IBundleManager bundleManager)
            : base("bundle", urlManager, accessor, requestHelper)
        {
            //TODO: Pretty sure if we want to control the caching of the file, we'll have to retrieve the bundle definition here
            // In reality we'll need to do that anyways if we want to support load balancing!
            // https://github.com/Shazwazza/Smidge/issues/17

            if (!IsBundleFound)
            {
                return;
            }

            if (!ParsedPath.Names.Any())
            {
                IsBundleFound = false;

                return;
            }

            FileKey = ParsedPath.Names.Single();

            if (!bundleManager.TryGetValue(FileKey, out Bundle bundle))
            {
                IsBundleFound = false;

                return;
            }
            Bundle = bundle;
        }

        public Bundle Bundle { get; }        
        public override string FileKey { get; }
    }
}
