using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NUglify;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.Nuglify
{
    public class NuglifyCss : IPreProcessor
    {
        private readonly NuglifySettings _settings;

        public NuglifyCss(NuglifySettings settings)
        {
            _settings = settings;
        }

        public Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            if (fileProcessContext.WebFile.DependencyType == WebFileType.Js)
                throw new InvalidOperationException("Cannot use " + nameof(NuglifyCss) + " with a js file source");
            
            var result = Uglify.Css(fileProcessContext.FileContent
                , string.IsNullOrEmpty(fileProcessContext.WebFile.RequestPath) ? fileProcessContext.WebFile.FilePath : fileProcessContext.WebFile.RequestPath
                , _settings.CssCodeSettings);

            if (result.HasErrors)
            {
                //TODO: need to format this exception message nicely
                throw new InvalidOperationException(
                    string.Join(",", result.Errors.Select(x => x.Message)));
            }

            fileProcessContext.Update(result.Code);

            return next(fileProcessContext);
        }
    }
}