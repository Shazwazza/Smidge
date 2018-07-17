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
    public class BundleFileSetGenerator : IBundleFileSetGenerator
    {
        private readonly FileProcessingConventions _conventions;
        private readonly FileSystemHelper _fileSystemHelper;
        private readonly IRequestHelper _requestHelper;
        
        public BundleFileSetGenerator(
            FileSystemHelper fileSystemHelper,
            IRequestHelper requestHelper,
            FileProcessingConventions conventions)
        {
            if (fileSystemHelper == null) throw new ArgumentNullException(nameof(fileSystemHelper));
            if (requestHelper == null) throw new ArgumentNullException(nameof(requestHelper));
            if (conventions == null) throw new ArgumentNullException(nameof(conventions));     
            _conventions = conventions;
            _fileSystemHelper = fileSystemHelper;
            _requestHelper = requestHelper;
        }

        /// <summary>
        /// Returns the ordered file set for a bundle and ensures that all pre-processor pipelines are applied correctly
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        public IEnumerable<IWebFile> GetOrderedFileSet(Bundle bundle, PreProcessPipeline pipeline)
        {
            if (bundle == null) throw new ArgumentNullException(nameof(bundle));
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

            var ordered = GetOrderedFileSet(bundle.Files, pipeline);

            //call the registered callback if any is set
            return bundle.OrderingCallback == null ? ordered : bundle.OrderingCallback(ordered);
        }

        /// <summary>
        /// Returns the ordered file set for dynamically registered assets and ensures that all pre-processor pipelines are applied correctly
        /// </summary>
        /// <param name="files"></param>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        public IEnumerable<IWebFile> GetOrderedFileSet(IEnumerable<IWebFile> files, PreProcessPipeline pipeline)
        {
            var customOrdered = new HashSet<IWebFile>(WebFilePairEqualityComparer.Instance);
            var defaultOrdered = new HashSet<IWebFile>(WebFilePairEqualityComparer.Instance);
            foreach (var file in files)
            {
                ValidateFile(file);
                if (file.Pipeline == null)
                {
                    file.Pipeline = pipeline.Copy();
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
            foreach(var f in customOrdered.OrderBy(x => x.Order))
            {
                defaultOrdered.Add(f);
            }

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