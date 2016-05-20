using Smidge.CompositeFiles;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Smidge.Models
{
    public class CompositeFileModel : RequestModel
    {

        public CompositeFileModel(IHasher hasher, IUrlManager urlManager, IActionContextAccessor accessor)
            : base("file", urlManager, accessor)
        {
            //Creates a single hash of the full url (which can include many files)
            FileKey = hasher.Hash(string.Join(".", ParsedPath.Names));
        }

        public override string FileKey { get; }
    }
}