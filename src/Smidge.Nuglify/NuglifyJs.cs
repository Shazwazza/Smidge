using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUglify;
using Smidge.FileProcessors;

namespace Smidge.Nuglify
{
    public class NuglifyJs : IPreProcessor
    {
        public Task ProcessAsync(FileProcessContext fileProcessContext, Func<string, Task> next)
        {
            var result = Uglify.Js(fileProcessContext.FileContent);

            if (result.HasErrors)
            {
                //TODO: need to format this exception message nicely
                throw new InvalidOperationException(
                    string.Join(",", result.Errors.Select(x => x.Message)));
            }

            return next(result.Code);
        }
    }
}
