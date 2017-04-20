using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Smidge.FileProcessors;

namespace Smidge.JavaScriptServices
{
    /// <summary>
    /// A minifier that uses Uglify + NodeJs
    /// </summary>
    public class UglifyNodeMinifier : IPreProcessor
    {
        private readonly INodeServices _nodeServices;
        private readonly Lazy<StringAsTempFile> _nodeScript;

        public UglifyNodeMinifier(SmidgeJavaScriptServices javaScriptServices)
        {
            if (javaScriptServices == null) throw new ArgumentNullException(nameof(javaScriptServices));
            _nodeServices = javaScriptServices.NodeServicesInstance;

            _nodeScript = new Lazy<StringAsTempFile>(() =>
            {
                using (var reader = new StreamReader(
                    typeof(UglifyNodeMinifier).GetTypeInfo().Assembly.GetManifestResourceStream("Smidge.JavaScriptServices.UglifyMinifier.js")))
                {
                    var script = reader.ReadToEnd();
                    return new StringAsTempFile(script); // Will be cleaned up on process exit
                }
            });
        }

        public async Task ProcessAsync(FileProcessContext fileProcessContext, Func<string, Task<string>> next)
        {
            var result = await _nodeServices.InvokeAsync<string>(
                _nodeScript.Value.FileName, fileProcessContext.FileContent);
            await next(result);
        }
    }
}