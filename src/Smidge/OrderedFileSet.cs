using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smidge.FileProcessors;
using Smidge.Models;
using Smidge.Options;

namespace Smidge
{
    /// <summary>
    /// Returns the ordered file set and ensures that all pre-processor pipelines are applied correctly
    /// </summary>
    public sealed class OrderedFileSet
    {
        private readonly IEnumerable<IWebFile> _files;
        private readonly PreProcessPipeline _defaultPipeline;
        private readonly FileProcessingConventions _conventions;
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly IRequestHelper _requestHelper;

        public OrderedFileSet(IEnumerable<IWebFile> files,
            FileSystemHelper fileSystemHelper,
            IRequestHelper requestHelper,
            PreProcessPipeline defaultPipeline,
            FileProcessingConventions conventions)
        {
            _files = files;
            _defaultPipeline = defaultPipeline;
            _conventions = conventions;
            _fileSystemHelper = fileSystemHelper;
            _requestHelper = requestHelper;
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
                file.FilePath = _requestHelper.Content(file.FilePath);

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
                                FilePath = _requestHelper.Content(f),
                                DependencyType = file.DependencyType,
                                Pipeline = file.Pipeline,
                                Order = file.Order
                            });
                        }
                        else
                        {
                            defaultOrdered.Add(new WebFile
                            {
                                FilePath = _requestHelper.Content(f),
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
            foreach (var convention in _conventions.Values)
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