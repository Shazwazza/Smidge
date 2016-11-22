using Smidge.CompositeFiles;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Smidge.Cache;
using Smidge.Hashing;

namespace Smidge.Models
{
    public class CompositeFileModel : RequestModel
    {

        public CompositeFileModel(IHasher hasher, IUrlManager urlManager, IActionContextAccessor accessor, IRequestHelper requestHelper, IBundleManager bundleManager, CacheBusterResolver cacheBusterResolver)
            : base("file", urlManager, accessor, requestHelper)
        {
            //Creates a single hash of the full url (which can include many files)
            FileKey = hasher.Hash(string.Join(".", ParsedPath.Names));

            CacheBuster = cacheBusterResolver.GetCacheBuster(bundleManager.GetDefaultBundleOptions(false).GetCacheBusterType());
        }

        public override ICacheBuster CacheBuster { get; }
        public override string FileKey { get; }
    }
}