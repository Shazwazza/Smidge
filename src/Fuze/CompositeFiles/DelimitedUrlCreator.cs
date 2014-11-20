using System;
using System.Collections.Generic;
using Fuze.Files;
using Microsoft.AspNet.Http;
using System.Text;
using System.Linq;

namespace Fuze.CompositeFiles
{
    public class DelimitedUrlCreator : IUrlCreator
    {
        private FuzeConfig _config;
        private UrlCreatorOptions _options;

        public DelimitedUrlCreator(UrlCreatorOptions options, FuzeConfig config)
        {
            _options = options;
            _config = config;
        }

        public IEnumerable<FileSetUrl> GetUrls(WebFileType type, IEnumerable<IWebFile> dependencies)
        {
            var files = new List<FileSetUrl>();
            var currBuilder = new StringBuilder();
            var delimitedBuilder = new StringBuilder();
            var builderCount = 1;
            var stringType = type.ToString();

            var remaining = new Queue<IWebFile>(dependencies);
            while (remaining.Any())
            {
                var current = remaining.Peek();

                //add the normal file path (generally this would already be hashed)
                delimitedBuilder.Append(current.FilePath);

                //test if the current string exceeds the max length, if so we need to split
                if ((delimitedBuilder.Length
                     + _options.RequestHandlerPath.Length
                     + stringType.Length
                     + _config.Version.Length
                     //this number deals with the ampersands, etc...
                     + 10)
                    >= (_options.MaxUrlLength))
                {
                    //we need to do a check here, this is the first one and it's already exceeded the max length we cannot continue
                    if (currBuilder.Length == 0)
                    {
                        throw new InvalidOperationException("The path for the single dependency: '" + current.FilePath + "' exceeds the max length (" + _options.MaxUrlLength + "), either reduce the single dependency's path length or increase the MaxHandlerUrlLength value");
                    }

                    //flush the current output to the array
                    var output = currBuilder.ToString();
                    files.Add(new FileSetUrl
                    {
                        Key = output.GenerateHash(),
                        Url = GetCompositeUrl(output, type)
                    });
                    //create some new output
                    currBuilder = new StringBuilder();
                    delimitedBuilder = new StringBuilder();
                    builderCount++;
                }
                else
                {
                    //update the normal builder
                    currBuilder.Append(current.FilePath);
                    //remove from the queue
                    remaining.Dequeue();
                }
            }

            if (builderCount > files.Count)
            {
                //flush the remaining output to the array
                var output = currBuilder.ToString();
                files.Add(new FileSetUrl
                {
                    Key = output.GenerateHash(),
                    Url = GetCompositeUrl(output, type)
                });
            }

            return files.ToArray();
        }

        private string GetCompositeUrl(string fileKey, WebFileType type)
        {
            var url = new StringBuilder();

            //Create a delimited URL query string

            const string handler = "{0}?s={1}&t={2}";
            url.Append(string.Format(handler,
                                     _options.RequestHandlerPath,
                                     Uri.EscapeUriString(fileKey), type));
            url.Append("&v=");
            url.Append(_config.Version);

            return url.ToString();
        }
    }
}