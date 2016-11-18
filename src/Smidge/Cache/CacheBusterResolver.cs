using System;
using System.Collections.Generic;
using System.Linq;

namespace Smidge.Cache
{
    /// <summary>
    /// Used to resolve an instance of ICacheBuster from the registered ones in the container
    /// </summary>
    public sealed class CacheBusterResolver
    {
        private readonly IEnumerable<ICacheBuster> _cacheBusters;

        public CacheBusterResolver(IEnumerable<ICacheBuster> cacheBusters)
        {
            _cacheBusters = cacheBusters;
        }

        /// <summary>
        /// Get the cache buster for the given type
        /// </summary>
        /// <param name="busterType"></param>
        /// <returns></returns>
        public ICacheBuster GetCacheBuster(Type busterType)
        {
            return _cacheBusters.FirstOrDefault(x => x.GetType() == busterType);
        }
    }
}