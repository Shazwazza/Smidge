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

        public async Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            ////if we wanted an intermediary step or if JS lib requires a physical file, we can do this
            //using (var nodeScript = new StringAsTempFile(fileProcessContext.FileContent))
            //{
            //    var result = await _nodeServices.InvokeAsync<string>(
            //        "wwwroot/JS/nodeTest.js",
            //        nodeScript.FileName);
            //    return result;
            //}

            //uglify can actually just use a raw string
            var result = await _nodeServices.InvokeAsync<string>(
                _nodeScript.Value.FileName, fileProcessContext.FileContent);
            return result;
        }
    }
}