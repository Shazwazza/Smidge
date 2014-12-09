using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Simple css minifier
    /// </summary>
    public sealed class CssMinifier : IPreProcessor
    {
        /// <summary>
        /// Minifies Css
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            var input = fileProcessContext.FileContent;
            input = Regex.Replace(input, @"[\n\r]+\s*", string.Empty);
            input = Regex.Replace(input, @"\s+", " ");
            input = Regex.Replace(input, @"\s?([:,;{}])\s?", "$1");
            input = Regex.Replace(input, @"([\s:]0)(px|pt|%|em)", "$1");
            input = Regex.Replace(input, @"/\*[\d\D]*?\*/", string.Empty);
            return Task.FromResult(input);
        }
    }
}