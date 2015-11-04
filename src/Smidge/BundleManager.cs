using Microsoft.AspNet.Http;
using Microsoft.Framework.OptionsModel;
using Smidge.FileProcessors;
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
        public BundleManager(FileSystemHelper fileSystemHelper, PreProcessPipelineFactory processorFactory, IOptions<Bundles> bundles)
        {
            _processorFactory = processorFactory;
            _bundles = bundles.Value;
            _fileSystemHelper = fileSystemHelper;                    
        }

        private FileSystemHelper _fileSystemHelper;
        private Bundles _bundles;
        private PreProcessPipelineFactory _processorFactory;

        public IEnumerable<string> GetBundleNames()
        {
            return _bundles.GetBundleNames();
        }

        public bool Exists(string bundleName)
        {
            List<IWebFile> result;
            return _bundles.TryGetValue(bundleName, out result);
        }

        /// <summary>
        /// Adds an item to the bundle, if the bundle doesn't exist it will be created
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="file"></param>
        public void AddToBundle(string bundleName, CssFile file)
        {
            List<IWebFile> files;
            if (_bundles.TryGetValue(bundleName, out files))
            {
                files.Add(file);
            }
            else
            {
                _bundles.Create(bundleName, file);
            }
        }

        /// <summary>
        /// Adds an item to the bundle, if the bundle doesn't exist it will be created
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="file"></param>
        public void AddToBundle(string bundleName, JavaScriptFile file)
        {
            List<IWebFile> files;
            if (_bundles.TryGetValue(bundleName, out files))
            {
                files.Add(file);
            }
            else
            {
                _bundles.Create(bundleName, file);
            }
        }

        public IEnumerable<IWebFile> GetFiles(string bundleName, HttpRequest request)
        {
            List<IWebFile> files;
            if (_bundles.TryGetValue(bundleName, out files))
            {
                var fileList = new List<IWebFile>();
                foreach (var file in files)
                {
                    if (file.Pipeline == null)
                    {
                        file.Pipeline = _processorFactory.GetDefault(file.DependencyType);
                    }

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
                                Pipeline = file.Pipeline
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