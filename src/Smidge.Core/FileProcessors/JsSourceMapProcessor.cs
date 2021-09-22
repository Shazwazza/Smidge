using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;



namespace Smidge.FileProcessors
{
    /// <summary>
    /// If a //# sourceMappingURL declaration is declared it is updated to the correct path
    /// </summary>
    /// <remarks>
    /// This is used for already minified files
    /// </remarks>
    public class JsSourceMapProcessor
        : IPreProcessor
    {
        private static readonly string _sourceMappingUrl = "//# sourceMappingURL";
        private readonly IWebsiteInfo _siteInfo;
        private readonly IRequestHelper _requestHelper;

        public JsSourceMapProcessor(IWebsiteInfo siteInfo, IRequestHelper requestHelper)
        {
            _siteInfo = siteInfo;
            _requestHelper = requestHelper;
        }

        public Task ProcessAsync(FileProcessContext fileProcessContext, PreProcessorDelegate next)
        {
            var sb = new StringBuilder();
            using (var reader = new StringReader(fileProcessContext.FileContent))
            {
                var line = reader.ReadLine();
                while(line != null)
                {                    
                    var isTrim = true;
                    var foundIndex = 0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        char c = line[i];
                        if (isTrim && char.IsWhiteSpace(c))
                            continue;

                        isTrim = false;
                        if (c == _sourceMappingUrl[foundIndex])
                        {
                            foundIndex++;
                            if (foundIndex == _sourceMappingUrl.Length)
                            {
                                // found! parse it
                                var match = RegexStatements.SourceMap.Match(line);
                                if (match.Success)
                                {
                                    var url = match.Groups[1].Value;
                                    // convert to it's absolute path
                                    var contentPath = _requestHelper.Content(fileProcessContext.WebFile);
                                    var uri = new Uri(contentPath, UriKind.RelativeOrAbsolute).MakeAbsoluteUri(_siteInfo.GetBaseUrl());
                                    var absolute = uri.ToAbsolutePath(url);
                                    var path = _requestHelper.Content(absolute);
                                    // replace the source map with the correct url
                                    WriteLine(sb, $"{_sourceMappingUrl}={path};");
                                }
                                else
                                {
                                    // should have matched, perhaps the source map is formatted in a weird way, we're going to ignore 
                                    // it since if it's rendered without the correct path then other errors will occur.
                                }
                                break; // exit for loop
                            }
                        }
                        else
                        {
                            // not found on this line
                            WriteLine(sb, line);
                            break; // exit for loop
                        }
                    }

                    // next
                    line = reader.ReadLine();
                }
            }
            
            fileProcessContext.Update(sb.ToString());
            return next(fileProcessContext);
        }

        private void WriteLine(StringBuilder sb, string line)
        {
            if (sb.Length > 0)
            {
                sb.Append(Environment.NewLine);
            }
            sb.Append(line);
        }
    }


}