using System;
using System.Collections.Generic;
using System.Collections;

namespace Smidge.Models
{
    /// <summary>
    /// A batch/collection of web files that can contain one or more local files or a single external file
    /// </summary>
    internal class WebFileBatch : IEnumerable<WebFilePair>
    {
        public WebFileBatch()
        {
            IsExternal = false;
        }

        private HashSet<WebFilePair> _files = new HashSet<WebFilePair>();
        
        public void AddExternal(IWebFile original)
        {
            if(IsExternal)
            {
                throw new InvalidOperationException("Cannot add more than one external file");
            }
            _files.Add(new WebFilePair(original, null));

            if (!original.FilePath.Contains(Constants.SchemeDelimiter))
            {
                throw new InvalidOperationException("Use " + nameof(AddInternal) + " to add an internal file");
            }

            IsExternal = true;
        }

        public void AddInternal(IWebFile original, IWebFile hashed)
        {
            if (IsExternal)
            {
                throw new InvalidOperationException("Cannot add more than one external file");
            }
            _files.Add(new WebFilePair(original, hashed));
            if (original.FilePath.Contains(Constants.SchemeDelimiter))
            {
                throw new InvalidOperationException("Use " + nameof(AddExternal) + " to add an external file");
            }
        }

        public IEnumerator<WebFilePair> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        public IEnumerable<WebFilePair> Files
        {
            get
            {
                return _files;
            }
        }

        public bool IsExternal { get; private set; }
    }
}