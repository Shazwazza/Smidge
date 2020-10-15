using System;
using System.Text.RegularExpressions;

namespace Smidge.FileProcessors
{
    internal class RegexStatements
    {
        public static readonly Regex ImportCssRegex = new Regex(@"^\s*@import (url\((.+?)\)|[""'](.+?)[""'])\s*;?", RegexOptions.Compiled | RegexOptions.Multiline);
        public static readonly Regex CssUrlRegex = new Regex(@"url\(((?![""']?data:|[""']?#).+?)\)", RegexOptions.Compiled);
    }
}