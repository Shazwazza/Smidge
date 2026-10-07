using System;
using System.Collections.Generic;
using System.Linq;
using NUglify;

namespace Smidge.Nuglify
{
    internal static class NuglifyErrorFormatter
    {
        public static string Format(string minifierName, string filePath, IEnumerable<UglifyError> errors)
            => $"{minifierName} failed to minify file '{filePath}': "
               + string.Join(Environment.NewLine, errors.Select(Format));

        private static string Format(UglifyError error)
            => $"{(error.IsError ? "error" : "warning")} {error.ErrorCode}: {error.Message} (line {error.StartLine}, column {error.StartColumn})";
    }
}
