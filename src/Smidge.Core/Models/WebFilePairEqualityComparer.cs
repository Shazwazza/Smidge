using System;
using System.Collections.Generic;

namespace Smidge.Models
{
    /// <summary>
    /// Determines equality based on the invariant file path
    /// </summary>
    internal class WebFilePairEqualityComparer : IEqualityComparer<IWebFile>
    {
        public static WebFilePairEqualityComparer Instance { get; } = new WebFilePairEqualityComparer();

        public bool Equals(IWebFile x, IWebFile y)
        {
            return x.FilePath.Equals(y.FilePath, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(IWebFile obj)
        {
            return obj.FilePath.GetHashCode();
        }
    }
}