using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge
{
    /// <summary>
    /// Returns the ordered file set and ensures that all pre-processor pipelines are applied correctly
    /// </summary>
    public sealed class OrderedFileSet
    {
        private readonly IEnumerable<IWebFile> _files;
        private readonly PreProcessPipeline _defaultPipeline;
        private readonly IEnumerable<IFileProcessingConvention> _allConventions;
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly HttpRequest _request;

        public OrderedFileSet(IEnumerable<IWebFile> files,
            FileSystemHelper fileSystemHelper, 
            HttpRequest request,
            PreProcessPipeline defaultPipeline, 
            IEnumerable<IFileProcessingConvention> allConventions)
        {
            _files = files;
            _defaultPipeline = defaultPipeline;
            _allConventions = allConventions;
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
                    file.Pipeline = _defaultPipeline.Copy();
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
            defaultOrdered.AddRange(customOrdered.OrderBy(x => x.Order));

            //apply conventions 
            return defaultOrdered.Select(ApplyConventions).Where(x => x != null);
        }

        private IWebFile ApplyConventions(IWebFile curr)
        {
            //Here we can apply some rules about the pipeline based on conventions.
            // For example, if the file name ends with .min.js we don't want to have JsMin execute,
            // there could be others of course and this should be configurable.
            foreach (var convention in _allConventions)
            {
                if (curr != null)
                {
                    curr = convention.Apply(curr);
                }
            }
            return curr;
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