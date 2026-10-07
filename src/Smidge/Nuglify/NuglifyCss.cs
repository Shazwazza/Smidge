using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUglify;
using NUglify.Css;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.Nuglify
{
    public class NuglifyCss : IPreProcessor, IMinifier
    {
        private readonly NuglifySettings _settings;
        private readonly IRequestHelper _requestHelper;
        private readonly ILogger _logger;

        public NuglifyCss(NuglifySettings settings, IRequestHelper requestHelper)
            : this(settings, requestHelper, NullLogger<NuglifyCss>.Instance)
        {
        }

        public NuglifyCss(NuglifySettings settings, IRequestHelper requestHelper, ILogger<NuglifyCss> logger)
        {
            _settings = settings;
            _requestHelper = requestHelper;
            _logger = logger ?? (ILogger)NullLogger.Instance;
        }

        /// <inheritdoc />
        public WebFileType FileType => WebFileType.Css;

        public Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            if (fileProcessContext.WebFile.DependencyType == WebFileType.Js)
                throw new InvalidOperationException("Cannot use " + nameof(NuglifyCss) + " with a js file source");

            if (!_settings.EnableMinification)
                return next(fileProcessContext);

            var result = NuglifyProcess(fileProcessContext, _settings.CssCodeSettings);

            if (result.HasErrors)
            {
                var message = NuglifyErrorFormatter.Format(nameof(NuglifyCss), fileProcessContext.WebFile.FilePath, result.Errors);
                if (_settings.ErrorBehavior == NuglifyErrorBehavior.Throw)
                    throw new InvalidOperationException(message);

                _logger.LogWarning("{Message}. The original file content will be used without minification.", message);
                return next(fileProcessContext);
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
            => Uglify.Css(fileProcessContext.FileContent, _requestHelper.Content(fileProcessContext.WebFile), cssSettings);
    }
}