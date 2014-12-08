using Microsoft.AspNet.Http;
using Smidge.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smidge
{
    public sealed class BundleManager
    {
        public BundleManager(FileSystemHelper fileSystemHelper, Action<BundleManager> initialize = null)
        {
            _fileSystemHelper = fileSystemHelper;
            if (initialize != null)
            {
                initialize(this);
            }            
        }

        private ConcurrentDictionary<string, IEnumerable<IWebFile>> _bundles = new ConcurrentDictionary<string, IEnumerable<IWebFile>>();
        private FileSystemHelper _fileSystemHelper;

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

        public void Create(string bundleName, params JavaScriptFile[] jsFiles)
        {
            _bundles.TryAdd(bundleName, jsFiles);
        }

        public void Create(string bundleName, params CssFile[] cssFiles)
        {
            _bundles.TryAdd(bundleName, cssFiles);
        }

        public void Create(string bundleName, WebFileType type, params string[] paths)
        {
            _bundles.TryAdd(
                bundleName, 
                type == WebFileType.Css 
                ? paths.Select(x => (IWebFile)new CssFile(x)) 
                : paths.Select(x => (IWebFile)new JavaScriptFile(x)));
        }
    }
}