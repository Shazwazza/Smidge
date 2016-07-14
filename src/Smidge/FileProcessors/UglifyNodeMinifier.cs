using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Smidge.NodeServices;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// A minifier that uses Uglify + NodeJs
    /// </summary>
    public class UglifyNodeMinifier : IPreProcessor
    {
        private readonly INodeServices _nodeServices;

        public UglifyNodeMinifier(SmidgeNodeServices nodeServices)
        {
            if (nodeServices == null) throw new ArgumentNullException(nameof(nodeServices));
            _nodeServices = nodeServices.NodeServicesInstance;
        }

        public async Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            //if we wanted an intermediary step or if JS lib requires a physical file, we can do this
            //using (var nodeScript = new StringAsTempFile(fileProcessContext.FileContent))
            //{
            //    var result = await _nodeServices.InvokeAsync<string>(
            //        "wwwroot/JS/nodeTest.js", 
            //        nodeScript.FileName);
            //    return result;
            //}

            //uglify can actually just use a raw string
            var result = await _nodeServices.InvokeAsync<string>(
                "JsPreProcessors/UglifyMinifier.js", fileProcessContext.FileContent);
            return result;
        }
    }
}