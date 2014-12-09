using System;
using System.Collections.Generic;
using Smidge.Models;
using System.Linq;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Defines the default pre-processor pipelines used
    /// </summary>
    public class PreProcessPipelineFactory
    {
        private IEnumerable<IPreProcessor> _allProcessors;

        public PreProcessPipelineFactory(IEnumerable<IPreProcessor> allProcessors)
        {
            _allProcessors = allProcessors;
        }

        public virtual PreProcessPipeline GetDefault(WebFileType fileType)
        {
            switch (fileType)
            {
                case WebFileType.Js:
                    return new PreProcessPipeline(new IPreProcessor[]
                    {
                        _allProcessors.OfType<JsMin>().Single()
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
    }
}