using Smidge.CompositeFiles;
using Microsoft.AspNetCore.Http;
using Smidge.Hashing;

namespace Smidge.Models
{
    public class CompositeFileModel : RequestModel
    {

        public CompositeFileModel(IHasher hasher, IUrlManager urlManager, IHttpContextAccessor httpContextAccessor, IRequestHelper requestHelper)
            : base("file", urlManager, httpContextAccessor, requestHelper)
        {
            if (!IsBundleFound)
            {
                return;
            }
            //Creates a single hash of the full url (which can include many files)
            FileKey = hasher.Hash(string.Join(".", ParsedPath.Names));
        }

        public override string FileKey { get; }
    }
}
