using System;

namespace Smidge.Models
{
    public static class WebFileExtensions
    {
        public static IWebFile Copy(this IWebFile orig, string newPath)
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