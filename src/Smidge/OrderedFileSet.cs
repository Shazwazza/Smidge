using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge
{
    public sealed class OrderedFileSet
    {
        private readonly IEnumerable<IWebFile> _files;
        private readonly PreProcessPipeline _defaultPipeline;
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly HttpRequest _request;

        public OrderedFileSet(IEnumerable<IWebFile> files,            
            FileSystemHelper fileSystemHelper, 
            HttpRequest request,
            PreProcessPipeline defaultPipeline)
        {
            _files = files;
            _defaultPipeline = defaultPipeline;
            _fileSystemHelper = fileSystemHelper;
            _request = request;
        }

        public IEnumerable<IWebFile> GetOrderedFileSet()
        {
            var customOrdered = new List<IWebFile>();
            var defaultOrdered = new List<IWebFile>();
            foreach (var file in _files)
            {
                ValidateFile(file);
                if (file.Pipeline == null)
                {
                    file.Pipeline = _defaultPipeline;
                }
                file.FilePath = _fileSystemHelper.NormalizeWebPath(file.FilePath, _request);

                //We need to check if this path is a folder, then iterate the files
                if (_fileSystemHelper.IsFolder(file.FilePath))
                {
                    var filePaths = _fileSystemHelper.GetPathsForFilesInFolder(file.FilePath);
                    foreach (var f in filePaths)
                    {
                        if (file.Order > 0)
                        {
                            customOrdered.Add(new WebFile
                            {
                                FilePath = _fileSystemHelper.NormalizeWebPath(f, _request),
                                DependencyType = file.DependencyType,
                                Pipeline = file.Pipeline,
                                Order = file.Order
                            });
                        }
                        else
                        {
                            defaultOrdered.Add(new WebFile
                            {
                                FilePath = _fileSystemHelper.NormalizeWebPath(f, _request),
                                DependencyType = file.DependencyType,
                                Pipeline = file.Pipeline,
                                Order = file.Order
                            });
                        }
                    }
                }
                else
                {
                    if (file.Order > 0)
                    {
                        customOrdered.Add(file);
                    }
                    else
                    {
                        defaultOrdered.Add(file);
                    }
                }
            }

            //add the custom ordered to the end of the list
            foreach (var ordered in customOrdered.OrderBy(x => x.Order))
            {
                defaultOrdered.Add(ordered);
            }

            return defaultOrdered;
        }

        private void ValidateFile(IWebFile file)
        {
            if (file.Order < 0)
            {
                throw new NotSupportedException("The Order of a web file cannot be less than zero");
            }
            
        }
    }
}