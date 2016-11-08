using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Smidge.Models;
using System.Linq;
using Smidge.Options;

namespace Smidge.FileProcessors
{
    
    /// <summary>
    /// Defines the default pre-processor pipelines used
    /// </summary>
    public class PreProcessPipelineFactory
    {
        private readonly IReadOnlyCollection<IPreProcessor> _allProcessors;
        private Func<WebFileType, IReadOnlyCollection<IPreProcessor>, PreProcessPipeline> _setGetDefaultCallback;

        public PreProcessPipelineFactory(IEnumerable<IPreProcessor> allProcessors)
        {
            _allProcessors = allProcessors.ToList();
        }        

        /// <summary>
        /// Returns a pipeline with the specified types in order
        /// </summary>
        /// <param name="preProcessorTypes"></param>
        /// <returns></returns>
        public PreProcessPipeline GetPipeline(params Type[] preProcessorTypes)
        {
            var processors = new List<IPreProcessor>();
            foreach (var type in preProcessorTypes)
            {
                processors.Add(_allProcessors.Single(x => x.GetType() == type));
            }
            return new PreProcessPipeline(processors);
        }

        /// <summary>
        /// Returns the default pipeline for a given file
        /// </summary>
        /// <param name="fileType"></param>
        /// <returns></returns>
        public virtual PreProcessPipeline GetDefault(WebFileType fileType)
        {
            //try to use the callback first and if something is returned use it, otherwise 
            // defer to the defaults
            var result = _setGetDefaultCallback?.Invoke(fileType, _allProcessors);
            if (result != null)
                return result;


            switch (fileType)
            {
                case WebFileType.Js:
                    return new PreProcessPipeline(new IPreProcessor[]
                    {                        
                        _allProcessors.OfType<JsMinifier>().Single()
                    });
                case WebFileType.Css:
                default:
                    return new PreProcessPipeline(new IPreProcessor[]
                    {
                        _allProcessors.OfType<CssImportProcessor>().Single(),
                        _allProcessors.OfType<CssUrlProcessor>().Single(),
                        _allProcessors.OfType<CssMinifier>().Single()
                    });
            }
        }

        /// <summary>
        /// Allows setting the callback used to get the default PreProcessPipeline, if the callback returns null
        /// then the logic defers to the GetDefault default result
        /// </summary>
        public Func<WebFileType, IReadOnlyCollection<IPreProcessor>, PreProcessPipeline> OnGetDefault
        {
            set { _setGetDefaultCallback = value; }
        }
    }
}