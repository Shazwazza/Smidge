using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;

namespace Smidge.NodeServices
{
    /// <summary>
    /// A custom node services class that encapsulates a custom NodeServices instance
    /// </summary>
    /// <remarks>
    /// This is so we don't interfere with perhaps another node services instance that a 
    /// developer wants to use
    /// </remarks>
    public class SmidgeNodeServices
    {
        public INodeServices NodeServicesInstance { get; private set; }

        public SmidgeNodeServices(INodeServices nodeServicesInstance)
        {
            if (nodeServicesInstance == null) throw new ArgumentNullException(nameof(nodeServicesInstance));
            NodeServicesInstance = nodeServicesInstance;
        }
    }
}
