using Smidge.CompositeFiles;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace Smidge.Models
{
    public class CompositeFileModel : RequestModel
    {

        public CompositeFileModel(IHasher hasher, IUrlManager urlManager, IActionContextAccessor accessor)
            : base("file", urlManager, accessor)
        {
            //Creates a single hash of the full url (which can include many files)
            _fileSetKey = hasher.Hash(string.Join(".", ParsedPath.Names));
        }

        private string _fileSetKey;

        public override string FileKey
        {
            get
            {
                return _fileSetKey;
            }
        }

        
    }
}