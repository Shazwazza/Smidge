using System.Threading.Tasks;
using Microsoft.AspNet.NodeServices;

namespace Smidge.FileProcessors
{
    public class NodeMinifier : IPreProcessor
    {
        private readonly INodeServices _nodeServices;

        public NodeMinifier(INodeServices nodeServices)
        {
            _nodeServices = nodeServices;
        }

        public async Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            //var script = "console.log(\"Hello World\");";
            //var nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit

            var result = await _nodeServices.Invoke("wwwroot\\JS\\nodeTest.js");
            return result; 
        }
    }
}