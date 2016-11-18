using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smidge.Cache
{
    /// <summary>
    /// Returns the value to cache bust the request
    /// </summary>
    public interface ICacheBuster
    {
        string GetValue();
    }
}
