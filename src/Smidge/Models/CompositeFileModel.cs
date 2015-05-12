using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Smidge.CompositeFiles;
using System;

namespace Smidge.Models
{
    public class CompositeFileModel : RequestModel
    {

        public CompositeFileModel(IHasher hasher, IUrlManager urlManager, ActionContext action)
            : base("file", urlManager, action)
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