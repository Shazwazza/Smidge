using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NUglify;
using Smidge.FileProcessors;

namespace Smidge.Nuglify
{
    public class NuglifyCss : IPreProcessor
    {
        public Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            var result = Uglify.Css(fileProcessContext.FileContent);

            if (result.HasErrors)
            {
                //TODO: need to format this exception message nicely
                throw new InvalidOperationException(
                    string.Join(",", result.Errors.Select(x => x.Message)));
            }

            return Task.FromResult(result.Code);
        }
    }
}