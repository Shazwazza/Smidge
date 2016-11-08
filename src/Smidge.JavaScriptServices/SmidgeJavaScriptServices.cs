using System;
using Microsoft.AspNetCore.NodeServices;

namespace Smidge.JavaScriptServices
{
    /// <summary>
    /// A custom node services class that encapsulates a custom NodeServices instance
    /// </summary>
    /// <remarks>
    /// This is so we don't interfere with perhaps another node services instance that a 
    /// developer wants to use
    /// </remarks>
    public class SmidgeJavaScriptServices
    {
        public INodeServices NodeServicesInstance { get; private set; }

        public SmidgeJavaScriptServices(INodeServices nodeServicesInstance)
        {
            if (nodeServicesInstance == null) throw new ArgumentNullException(nameof(nodeServicesInstance));
            NodeServicesInstance = nodeServicesInstance;
        }
    }
}
