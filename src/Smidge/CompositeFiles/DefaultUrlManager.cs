using System;
using System.Collections.Generic;
using Smidge.Models;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Options;
using Smidge.Options;

namespace Smidge.CompositeFiles
{
    public class DefaultUrlManager : IUrlManager
    {
        private readonly ISmidgeConfig _config;
        private readonly IHasher _hasher;
        private readonly IVirtualPathTranslator _virtualPathTranslator;
        private readonly UrlManagerOptions _options;

        public DefaultUrlManager(IOptions<SmidgeOptions> options, ISmidgeConfig config, IHasher hasher, IVirtualPathTranslator virtualPathTranslator)
        {
            _hasher = hasher;
            _virtualPathTranslator = virtualPathTranslator;
            _options = options.Value.UrlOptions;
            _config = config;
        }

        public string GetUrl(string bundleName, string fileExtension)
        {
            const string handler = "~/{0}/{1}{2}.v{3}";
            return _virtualPathTranslator.Content(
                string.Format(
                    handler,
                    _options.BundleFilePath,
                    Uri.EscapeUriString(bundleName),
                    fileExtension,
                    _config.Version));

        }

        public IEnumerable<FileSetUrl> GetUrls(IEnumerable<IWebFile> dependencies, string fileExtension)
        {
            var files = new List<FileSetUrl>();
            var currBuilder = new StringBuilder();
            var delimitedBuilder = new StringBuilder();
            var builderCount = 1;

            var remaining = new Queue<IWebFile>(dependencies);
            while (remaining.Any())
            {
                var current = remaining.Peek();

                //add the normal file path (generally this would already be hashed)
                delimitedBuilder.Append(current.FilePath.TrimExtension(fileExtension).EnsureEndsWith('.'));

                //test if the current string exceeds the max length, if so we need to split
                if ((delimitedBuilder.Length
                     + _options.CompositeFilePath.Length
                     + fileExtension.Length
                     + _config.Version.Length
                     //this number deals with slashes, etc...
                     + 10)
                    >= (_options.MaxUrlLength))
                {
                    //we need to do a check here, this is the first one and it's already exceeded the max length we cannot continue
                    if (currBuilder.Length == 0)
                    {
                        throw new InvalidOperationException("The path for the single dependency: '" + current.FilePath.TrimExtension(fileExtension) + "' exceeds the max length (" + _options.MaxUrlLength + "), either reduce the single dependency's path length or increase the MaxHandlerUrlLength value");
                    }

                    //flush the current output to the array
                    var output = currBuilder.ToString().TrimEnd('.');
                    files.Add(new FileSetUrl
                    {
                        Key = _hasher.Hash(output),
                        Url = GetCompositeUrl(output, fileExtension)
                    });
                    //create some new output
                    currBuilder = new StringBuilder();
                    delimitedBuilder = new StringBuilder();
                    builderCount++;
                }
                else
                {
                    //update the normal builder
                    currBuilder.Append(current.FilePath.TrimExtension(fileExtension).EnsureEndsWith('.'));
                    //remove from the queue
                    remaining.Dequeue();
                }
            }

            if (builderCount > files.Count)
            {
                //flush the remaining output to the array
                var output = currBuilder.ToString().TrimEnd('.');
                files.Add(new FileSetUrl
                {
                    Key = _hasher.Hash(output),
                    Url = GetCompositeUrl(output, fileExtension)
                });
            }

            return files.ToArray();
        }

        public ParsedUrlPath ParsePath(string input)
        {
            var result = new ParsedUrlPath();

            var parts = input.Split(new[] { '.' });

            if (parts.Length < 3)
            {
                //invalid
                return null;
            }

            if (!parts[parts.Length - 1].StartsWith("v"))
            {
                //invalid
                return null;
            }

            result.Version = parts[parts.Length - 1].Substring(1);
            var ext = parts[parts.Length - 2];
            WebFileType type;
            if (!Enum.TryParse(ext, true, out type))
            {
                //invalid
                return null;
            }
            result.WebType = type;

            result.Names = parts.Take(parts.Length - 2);

            return result;
        }

        private string GetCompositeUrl(string fileKey, string fileExtension)
        {
            //Create a delimited URL query string

            const string handler = "~/{0}/{1}{2}.v{3}";
            return _virtualPathTranslator.Content(
                string.Format(
                    handler,
                    _options.CompositeFilePath,
                    Uri.EscapeUriString(fileKey),
                    fileExtension,
                    _config.Version));
        }
    }
}