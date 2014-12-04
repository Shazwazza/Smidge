using Microsoft.AspNet.Http;
using Smidge.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smidge
{
    /// <summary>
    /// Puts a collection of web files into appropriate batches - based on internal vs external dependencies or for other
    /// reasons to split files into batches (i.e. different html attributes)
    /// </summary>
    internal class FileBatcher
    {
        private FileSystemHelper _fileSystemHelper;
        private IHasher _hasher;
        private HttpRequest _request;

        public FileBatcher(FileSystemHelper fileSystemHelper, HttpRequest request, IHasher hasher)
        {
            _fileSystemHelper = fileSystemHelper;
            _request = request;
            _hasher = hasher;
        }

        /// <summary>
        /// Get a collection of files that will be used to create the composite file(s), this will normalize all of the paths and ignore
        /// any external requests.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="externalRequests">Returns a collection of external requests found in the collection</param>
        /// <returns></returns>
        /// <remarks>
        /// We need to get a collection of files that have their cached/hashed paths, this is used 
        /// to check if the composite file has already been created, if it is then we don't need to worry 
        /// about anything. If it is not, then we need to minify each of the files now. Then when the request
        /// is made to get the composite file, that process is already complete and the composite handler just
        /// needs to combine, compress and store the file.
        /// 
        /// The result of this method is a staggered collection of files. This will iterate over the files and when it comes across
        /// an external dependency or a dependency that requires a different rendering output, it will close the current collection and 
        /// start another one. Each of these collections will be rendered individually.
        /// </remarks>
        internal IEnumerable<WebFileBatch> GetCompositeFileCollectionForUrlGeneration(IEnumerable<IWebFile> files)
        {
            var current = new WebFileBatch();
            var result = new List<WebFileBatch>();
            foreach (var f in files)
            {
                var webPath = _fileSystemHelper.NormalizeWebPath(f.FilePath, _request);

                //if this is an external path then we need to split and start new
                if (webPath.Contains(Uri.SchemeDelimiter))
                {
                    if (current.Any())
                    {
                        result.Add(current);
                        current = new WebFileBatch();
                    }
                    f.FilePath = webPath;
                    current.AddExternal(f);
                    //add it to the result and split again - each batch can only contain a single external request
                    result.Add(current);
                    current = new WebFileBatch();
                }
                else if (_fileSystemHelper.IsFolder(f.FilePath))
                {
                    //it's a folder so get all of it's individual files and process them 
                    var filePaths = _fileSystemHelper.GetPathsForFilesInFolder(f.FilePath);
                    foreach (var p in filePaths)
                    {
                        var subFile = Duplicate(f, _fileSystemHelper.NormalizeWebPath(p, _request));
                        var hashedFile = Duplicate(subFile, _hasher.Hash(subFile.FilePath));
                        hashedFile.Minify = f.Minify;
                        current.AddInternal(subFile, hashedFile);
                    }
                }
                else {
                    var hashedFile = Duplicate(f, _hasher.Hash(webPath));
                    current.AddInternal(Duplicate(f, webPath), hashedFile);
                }
            }

            //check if there's any left in current and add it
            if (current.Any())
            {
                result.Add(current);
            }

            return result;
        }

        private IWebFile Duplicate(IWebFile orig, string newPath)
        {
            return new WebFile
            {
                DependencyType = orig.DependencyType,
                FilePath = newPath,
                Minify = orig.Minify
            };
        }
    }
}