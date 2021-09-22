using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Smidge
{
    /// <summary>
    /// A default implementation for globbing pattern matching for IFileProvider.
    /// </summary>
    /// <remarks>
    /// If the underlying IFileProvider is the basic PhysicalFileProvider then we can use
    /// the built in Matcher API in .NET. Else, we use a regex pattern to match all of the common
    /// globbing patterns and recursively move through the directories.
    /// </remarks>
    public class DefaultFileProviderFilter : IFileProviderFilter
    {
        //private readonly Regex _simpleFileGlobPattern = new Regex(@"(^.*?)(/\*{1,2})?/(\w+|\*)\.(\w+|\*)", RegexOptions.Compiled);
        private IFileInfo _rootFileInfo;

        public IEnumerable<string> GetMatchingFiles(IFileProvider fileProvider, string filePattern)
        {
            if (string.IsNullOrWhiteSpace(filePattern))
            {
                throw new ArgumentException($"'{nameof(filePattern)}' cannot be null or whitespace.", nameof(filePattern));
            }

            filePattern = filePattern.TrimEnd('/');

            if (_rootFileInfo == null)
            {
                _rootFileInfo = fileProvider.GetFileInfo("/");
            }

            // If the file provider contains a single physical file provider
            // then we can use the built in Microsoft.Extensions.FileSystemGlobbing
            // to match files.
            if (_rootFileInfo?.PhysicalPath != null)
            {
                var matcher = new Matcher();
                matcher.AddInclude(filePattern);
                var dir = new DirectoryInfoWrapper(new DirectoryInfo(_rootFileInfo.PhysicalPath));
                var result = matcher.Execute(dir);

                PatternMatchingResult globbingResult = matcher.Execute(dir);
                IEnumerable<string> fileMatches = globbingResult.Files.Select(x => x.Path);

                return fileMatches;
            }
            else
            {
                // Fallback to rudimentary matching

                string splitter = filePattern.Contains("**/")
                    ? "**/"
                    : filePattern.Contains("*/") ? "*/"
                    : null;

                var recursiveParts = filePattern.Split(splitter);
                if (recursiveParts.Length > 2)
                {
                    throw new InvalidOperationException("invalid file pattern");
                }
                if (recursiveParts.Length == 2)
                {
                    var folder = recursiveParts[0].TrimEnd('/');
                    var fileName = recursiveParts[1];
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);

                    return GetFilesInFolder(fileProvider, folder, extension, 0, splitter == "**/" ? 2 : 1);
                }
                else
                {
                    var fileName = Path.GetFileName(filePattern);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(filePattern);

                    bool isEmptyExtensions = string.IsNullOrWhiteSpace(extension);
                    if (fileNameWithoutExt != "*" && !isEmptyExtensions)
                    {
                        // single file only
                        return new[] { filePattern };
                    }

                    string folder;
                    if (isEmptyExtensions)
                    {
                        extension = ".*";
                        folder = filePattern;
                    }
                    else
                    {
                        folder = filePattern.Substring(0, filePattern.LastIndexOf(fileName)).TrimEnd('/');
                    }

                    return GetFilesInFolder(fileProvider, folder, extension, 0, 0);
                }

                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Gets files in folders recursively
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="folder"></param>
        /// <param name="extension"></param>
        /// <param name="currentRecursiveDepth">
        /// Tracks the current recursion depth
        /// </param>
        /// <param name="recurseDepth">
        /// How to recurse.
        /// 0 = don't recurse.
        /// 1 = recurse to only a single depth (i.e. one directory), don't include files at depth 0.
        /// 2 = recurse all, include files at all levels.
        /// </param>
        /// <returns></returns>
        private IEnumerable<string> GetFilesInFolder(IFileProvider fileProvider, string folder, string extension, int currentRecursiveDepth, int recurseDepth)
        {
            var folderContents = fileProvider.GetDirectoryContents(folder);
            if (folderContents.Exists)
            {
                var folderPath = string.IsNullOrWhiteSpace(folder) ? folder : $"{folder}/";

                foreach (var item in folderContents)
                {
                    if (!item.IsDirectory
                        && (recurseDepth == 2 || recurseDepth ==  currentRecursiveDepth)
                        && item.Exists
                        && (extension == ".*" || extension == Path.GetExtension(item.Name)))
                    {
                        yield return $"{folderPath}{item.Name}";
                    }
                    else if ((recurseDepth == 2 || (recurseDepth == 1 && currentRecursiveDepth < recurseDepth)) && item.IsDirectory)
                    {
                        var nextDepth = currentRecursiveDepth + 1;
                        foreach (var f in GetFilesInFolder(fileProvider, $"{folderPath}{item.Name}", extension, nextDepth, recurseDepth))
                        {
                            yield return f;
                        }
                    }
                }
            }
        }
    }
}
