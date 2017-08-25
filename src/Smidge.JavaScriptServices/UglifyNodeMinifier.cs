using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.NodeServices;
using Smidge.FileProcessors;

namespace Smidge.JavaScriptServices
{
    /// <summary>
    /// A minifier that uses Uglify + NodeJs
    /// </summary>
    public class UglifyNodeMinifier : IPreProcessor
    {
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly INodeServices _nodeServices;
        private readonly Lazy<StringAsTempFile> _nodeScript;

        public UglifyNodeMinifier(SmidgeJavaScriptServices javaScriptServices, IApplicationLifetime applicationLifetime)
        {
            if (javaScriptServices == null) throw new ArgumentNullException(nameof(javaScriptServices));
            if (applicationLifetime == null) throw new ArgumentNullException(nameof(applicationLifetime));
            _nodeServices = javaScriptServices.NodeServicesInstance;
            _applicationLifetime = applicationLifetime;

            _nodeScript = new Lazy<StringAsTempFile>(() =>
            {
                using (var reader = new StreamReader(
                    typeof(UglifyNodeMinifier).GetTypeInfo().Assembly.GetManifestResourceStream("Smidge.JavaScriptServices.UglifyMinifier.js")))
                {
                    var script = reader.ReadToEnd();
                    return new StringAsTempFile(script, _applicationLifetime.ApplicationStopping); // Will be cleaned up on process exit
                }
            });
        }

        public async Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            var result = await _nodeServices.InvokeAsync<string>(
                _nodeScript.Value.FileName, fileProcessContext.FileContent);

            fileProcessContext.Update(result);

            await next(fileProcessContext);
        }
    }
}