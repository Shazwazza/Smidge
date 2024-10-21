using Smidge.CompositeFiles;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Smidge.Hashing;

namespace Smidge.Models
{
    public class CompositeFileModel : RequestModel
    {

        public CompositeFileModel(IHasher hasher, IUrlManager urlManager, IActionContextAccessor accessor, IRequestHelper requestHelper)
            : base("file", urlManager, accessor, requestHelper)
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
