using System;

namespace Smidge.Models
{
    public static class WebFileExtensions
    {
        /// <summary>
        /// Creates a copy of the <see cref="IWebFile"/> with the new path
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="newPath"></param>
        /// <returns></returns>
        public static IWebFile Duplicate(this IWebFile orig, string newPath)
        {
            return new WebFile
            {
                DependencyType = orig.DependencyType,
                FilePath = newPath,
                Pipeline = orig.Pipeline
            };
        }
    }
}