using System;
using System.Collections.Generic;
using Smidge.Models;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Smidge.Options;

namespace Smidge.CompositeFiles
{
    public class DefaultUrlManager : IUrlManager
    {
        private readonly ISmidgeConfig _config;
        private readonly IHasher _hasher;
        private readonly IRequestHelper _requestHelper;
        private readonly UrlManagerOptions _options;

        public DefaultUrlManager(IOptions<SmidgeOptions> options, ISmidgeConfig config, IHasher hasher, IRequestHelper requestHelper)
        {
            _hasher = hasher;
            _requestHelper = requestHelper;
            _options = options.Value.UrlOptions;
            _config = config;
        }

        public string GetUrl(string bundleName, string fileExtension)
        {
            var url = BuildUrl(
                _options.UrlPattern,
                _options.BundleFilePath,
                Uri.EscapeUriString(bundleName),
                fileExtension,
                _config.Version);

            return _requestHelper.Content(url);
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
            var url = BuildUrl(
                _options.UrlPattern, 
                _options.CompositeFilePath, 
                Uri.EscapeUriString(fileKey), 
                fileExtension, 
                _config.Version);

            return _requestHelper.Content(url);
        }

        public static string BuildUrl(string pattern, string path, string name, string ext, string version)
        {
            pattern = pattern.Trim();

            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));            
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(ext)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(ext));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(version));

            if (!ext.StartsWith("."))
                throw new FormatException("The file extension is not formatted correctly and must begin with '.'");

            if (!pattern.StartsWith("~/"))
                throw new FormatException("The URL pattern is not formatted correctly and must begin with '~/'");
            
            //strip the . from the file extension, that can be part of the pattern
            ext = ext.TrimStart('.');

            var matches = UrlPatternTokens.Matches(pattern);

            if (matches.Count != 4)
                throw new FormatException("The URL pattern is not formatted correctly and must contain the correct tokens");
            
            var lastIndex = 0;
            var sb = new StringBuilder();
            foreach (Match r in matches)
            {
                var delim = pattern.Substring(lastIndex, r.Index - lastIndex);

                if (sb.Length > 0)
                {
                    //if there's already a string, we need to validate that there is some delimiter
                    //between each match, otherwise we cannot parse it the other way around
                    if (delim.Length == 0)
                        throw new FormatException("The URL pattern is not formatted correctly, it must contain some delimiter text between each token");
                }
                sb.Append(delim);
                lastIndex = (r.Index + r.Length);

                switch (r.Value)
                {
                    case "{Path}":
                        sb.Append(path);
                        break;
                    case "{Name}":
                        sb.Append(name);
                        break;
                    case "{Ext}":
                        sb.Append(ext);
                        break;
                    case "{Version}":
                        sb.Append(version);
                        break;
                }                
            }

            if (lastIndex < pattern.Length - 1)
            {
                sb.Append(pattern.Substring(lastIndex, (pattern.Length) - lastIndex));
            }

            return sb.ToString();
        }

        private static readonly Regex UrlPatternTokens = new Regex("({Path}){1}|({Name}){1}|({Ext}){1}|({Version}){1}", RegexOptions.Compiled);
    }
}