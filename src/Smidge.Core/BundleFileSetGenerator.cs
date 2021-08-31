using System;
using System.Collections.Generic;
using System.Linq;
using Smidge.Core;
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
        private readonly ISmidgeFileSystem _fileSystem;
        
        public BundleFileSetGenerator(
            ISmidgeFileSystem fileSystem,
            FileProcessingConventions conventions)
        {
            _conventions = conventions ?? throw new ArgumentNullException(nameof(conventions));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
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
            var customOrdered = new List<IWebFile>();
            var defaultOrdered = new OrderedSet<IWebFile>(WebFilePairEqualityComparer.Instance);
            
            foreach (var file in files)
            {
                ValidateFile(file);

                file.Pipeline ??= pipeline;

                // We need to check if this path is a folder, then iterate the files
                // TODO: this should support Glob patterns, so this check would need to be a little different
                // or the file system should just call GetPathsForFilesInFolder with a glob pattern and work with that.
                // i.e. if the result is more than one, than it's treated here like a folder.
                if (_fileSystem.IsFolder(file.FilePath))
                {
                    var filePaths = _fileSystem.GetPathsForFilesInFolder(file.FilePath);

                    foreach (var f in filePaths)
                    {
                        var item = new WebFile
                        {
                            FilePath = f,
                            DependencyType = file.DependencyType,
                            Pipeline = file.Pipeline,
                            Order = file.Order
                        };

                        if (ApplyConventions(item) is { } webFile)
                        {
                            if (file.Order > 0)
                            {
                                customOrdered.Add(webFile);
                            }
                            else
                            {
                                defaultOrdered.Add(webFile);
                            }
                        }
                    }
                }
                else
                {
                    if (ApplyConventions(file) is { } webFile)
                    {
                        if (file.Order > 0)
                        {
                            customOrdered.Add(webFile);
                        }
                        else
                        {
                            defaultOrdered.Add(webFile);
                        }
                    }
                }
            }

            // Add the custom ordered to the end of the list
            foreach(var f in customOrdered.OrderBy(x => x.Order))
            {
                defaultOrdered.Add(f);
            }
            
            return defaultOrdered;
        }

        private IWebFile ApplyConventions(IWebFile file)
        {
            //Here we can apply some rules about the pipeline based on conventions.
            // For example, if the file name ends with .min.js we don't want to have JsMin execute,
            // there could be others of course and this should be configurable.
            foreach (var convention in _conventions.Values)
            {
                if (file != null)
                {
                    file = convention.Apply(file);
                }
            }
            return file;
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