using System;
using System.Text.RegularExpressions;

namespace Smidge.FileProcessors
{
    internal class RegexStatements
    {
        public static readonly Regex ImportCssRegex = new Regex(@"@import (url\((.+?)\)|[""'](.+?)[""']);?", RegexOptions.Compiled);
        public static readonly Regex CssUrlRegex = new Regex(@"url\(((?![""']?data:).+?)\)", RegexOptions.Compiled);
    }
}