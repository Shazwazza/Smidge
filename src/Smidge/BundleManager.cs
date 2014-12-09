using Microsoft.AspNet.Http;
using Microsoft.Framework.OptionsModel;
using Smidge.Models;
using Smidge.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smidge
{
   
    public sealed class BundleManager
    {
        public BundleManager(FileSystemHelper fileSystemHelper, IOptions<Bundles> bundles)
        {
            _bundles = bundles.Options;
            _fileSystemHelper = fileSystemHelper;                    
        }

        private FileSystemHelper _fileSystemHelper;
        private Bundles _bundles;

        public bool Exists(string bundleName)
        {
            IEnumerable<IWebFile> result;
            return _bundles.TryGetValue(bundleName, out result);
        }

        public IEnumerable<IWebFile> GetFiles(string bundleName, HttpRequest request)
        {
            IEnumerable<IWebFile> files;
            if (_bundles.TryGetValue(bundleName, out files))
            {
                var fileList = new List<IWebFile>();
                foreach (var file in files)
                {
                    file.FilePath = _fileSystemHelper.NormalizeWebPath(file.FilePath, request);

                    //We need to check if this path is a folder, then iterate the files
                    if (_fileSystemHelper.IsFolder(file.FilePath))
                    {
                        var filePaths = _fileSystemHelper.GetPathsForFilesInFolder(file.FilePath);
                        foreach (var f in filePaths)
                        {
                            fileList.Add(new WebFile
                            {
                                FilePath = _fileSystemHelper.NormalizeWebPath(f, request),
                                DependencyType = file.DependencyType,
                                Minify = file.Minify
                            });
                        }
                    }
                    else
                    {
                        fileList.Add(file);
                    }
                }
                return fileList;
            }
            return null;
        }
    }
}