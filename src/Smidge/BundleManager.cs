using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Smidge.Models;
using Smidge.Options;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Smidge.FileProcessors;

namespace Smidge
{
   
    public sealed class BundleManager
    {
        public BundleManager(FileSystemHelper fileSystemHelper, IOptions<Bundles> bundles, PreProcessPipelineFactory processorFactory)
        {
            _bundles = bundles.Value;
            _fileSystemHelper = fileSystemHelper;
            _processorFactory = processorFactory;
        }

        private readonly FileSystemHelper _fileSystemHelper;
        private readonly Bundles _bundles;
        private readonly PreProcessPipelineFactory _processorFactory;

        public IEnumerable<string> GetBundleNames(WebFileType type)
        {
            return _bundles.GetBundleNames(type);
        }

        public bool Exists(string bundleName)
        {
            BundleFileCollection collection;
            return _bundles.TryGetValue(bundleName, out collection);
        }

        /// <summary>
        /// Adds an item to the bundle, if the bundle doesn't exist it will be created
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="file"></param>
        public void AddToBundle(string bundleName, CssFile file)
        {
            BundleFileCollection collection;
            if (_bundles.TryGetValue(bundleName, out collection))
            {
                collection.Files.Add(file);
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
            BundleFileCollection collection;
            if (_bundles.TryGetValue(bundleName, out collection))
            {
                collection.Files.Add(file);
            }
            else
            {
                _bundles.Create(bundleName, file);
            }
        }

        /// <summary>
        /// Returns the ordered collection of files for the bundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="virtualPathTranslator"></param>
        /// <returns></returns>
        public IEnumerable<IWebFile> GetFiles(string bundleName, IVirtualPathTranslator virtualPathTranslator)
        {
            BundleFileCollection collection;
            if (!_bundles.TryGetValue(bundleName, out collection)) return null;

            //the file type in the bundle will always be the same
            var first = collection.Files.FirstOrDefault();
            if (first == null) return Enumerable.Empty<IWebFile>();

            var orderedSet = new OrderedFileSet(collection.Files, _fileSystemHelper, virtualPathTranslator,
                _processorFactory.GetDefault(first.DependencyType), 
                _processorFactory.FileProcessingConventions);
            var ordered = orderedSet.GetOrderedFileSet();

            //call the registered callback if any is set
            return collection.OrderingCallback == null ? ordered : collection.OrderingCallback(ordered);
        }

        
    }
}