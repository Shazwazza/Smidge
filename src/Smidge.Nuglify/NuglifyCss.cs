using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NUglify;
using NUglify.Css;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.Nuglify
{
    public class NuglifyCss : IPreProcessor
    {
        private readonly NuglifySettings _settings;
        private readonly ISmidgeFileSystem _fileSystem;

        public NuglifyCss(NuglifySettings settings, ISmidgeFileSystem fileSystem)
        {
            _settings = settings;
            _fileSystem = fileSystem;
        }

        public Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            if (fileProcessContext.WebFile.DependencyType == WebFileType.Js)
                throw new InvalidOperationException("Cannot use " + nameof(NuglifyCss) + " with a js file source");
            
            var result = NuglifyProcess(fileProcessContext, _settings.CssCodeSettings);

            if (result.HasErrors)
            {
                //TODO: need to format this exception message nicely
                throw new InvalidOperationException(
                    string.Join(",", result.Errors.Select(x => x.Message)));
            }

            fileProcessContext.Update(result.Code);

            return next(fileProcessContext);
        }

        /// <summary>
        /// Processes the file content by Nuglify
        /// </summary>
        /// <remarks>
        /// This is virtual allowing developers to override this in cases where customizations may need to be done 
        /// to the Nuglify process. For example, changing the FilePath used.
        /// </remarks>
        protected virtual UglifyResult NuglifyProcess(FileProcessContext fileProcessContext, CssSettings cssSettings)
            => Uglify.Css(fileProcessContext.FileContent, _fileSystem.ConvertToFileProviderPath(fileProcessContext.WebFile.FilePath), cssSettings);
    }
}