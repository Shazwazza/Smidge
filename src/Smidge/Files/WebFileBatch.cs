using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Collections;

namespace Smidge.Files
{
    /// <summary>
    /// A batch/collection of web files that can contain one or more local files or a single external file
    /// </summary>
    public class WebFileBatch : IEnumerable<IWebFile>
    {
        public WebFileBatch()
        {
            IsExternal = false;
        }

        private List<IWebFile> _files = new List<IWebFile>();
        
        public void Add(IWebFile file)
        {
            if (IsExternal)
            {
                throw new InvalidOperationException("Cannot add more than one external file");
            }
            _files.Add(file);
            if (file.FilePath.Contains(Uri.SchemeDelimiter))
            {
                IsExternal = true;
            }
        }

        public IEnumerator<IWebFile> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        public IEnumerable<IWebFile> Files
        {
            get
            {
                return _files;
            }
        }

        public bool IsExternal { get; private set; }
    }
}