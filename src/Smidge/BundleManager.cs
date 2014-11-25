using Smidge.Files;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Smidge
{
    public sealed class BundleManager
    {
        public BundleManager(FileSystemHelper fileSystemHelper, Action<BundleManager> initialize)
        {
            _fileSystemHelper = fileSystemHelper;
            if (initialize != null)
            {
                initialize(this);
            }            
        }

        private ConcurrentDictionary<string, object> _bundles = new ConcurrentDictionary<string, object>();
        private FileSystemHelper _fileSystemHelper;

        public bool Exists(string bundleName)
        {
            object result;
            return _bundles.TryGetValue(bundleName, out result);
        }

        public IEnumerable<IWebFile> GetFiles(string bundleName)
        {
            object result;
            if (_bundles.TryGetValue(bundleName, out result))
            {
                var collection = result as IEnumerable<IWebFile>;
                if (collection != null)
                {
                    return collection;
                }
                var folder = result as FolderBundle;
                if (folder != null)
                {
                    var filesInFolder = new List<IWebFile>();
                    var path = _fileSystemHelper.MapPath(folder.Folder);
                    var files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        switch (folder.WebFileType)
                        {
                            case WebFileType.Js:
                                filesInFolder.Add(new JavaScriptFile(_fileSystemHelper.ReverseMapPath(file)));
                                break;
                            case WebFileType.Css:
                                filesInFolder.Add(new CssFile(_fileSystemHelper.ReverseMapPath(file)));
                                break;
                        }
                    }
                    return filesInFolder;
                }
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

        public void Create(string bundleName, string folder, WebFileType type)
        {
            _bundles.TryAdd(bundleName, new FolderBundle { Folder = folder, WebFileType = type });
        }

        private class FolderBundle
        {
            public WebFileType WebFileType { get; set; }
            public string Folder { get; set; }
        }

    }
}