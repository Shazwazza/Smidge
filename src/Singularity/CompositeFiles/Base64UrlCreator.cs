using System;
using System.Collections.Generic;
using Singularity.Files;
using Microsoft.AspNet.Http;
using System.Text;
using System.Linq;

namespace Singularity.CompositeFiles
{
    public class Base64UrlCreatorOptions
    {
        public int MaxUrlLength { get; set; }
        public string Version { get; set; }
        public string RequestHandlerPath { get; set; }
    }

    public class Base64UrlCreator : IUrlCreator
    {
        private Base64UrlCreatorOptions _options;

        public Base64UrlCreator(Base64UrlCreatorOptions options)
        {
            _options = options;
        }

        public IEnumerable<string> GetUrls(IDependentFileType type, IEnumerable<IDependentFile> dependencies)
        {
            var files = new List<string>();
            var currBuilder = new StringBuilder();
            var base64Builder = new StringBuilder();
            var builderCount = 1;
            var stringType = type.ToString();

            var remaining = new Queue<IDependentFile>(dependencies);
            while (remaining.Any())
            {
                var current = remaining.Peek();

                //update the base64 output to get the length
                base64Builder.Append(current.FilePath.EncodeTo64());

                //test if the current base64 string exceeds the max length, if so we need to split
                if ((base64Builder.Length
                     + _options.RequestHandlerPath.Length
                     + stringType.Length
                     + _options.Version.Length
                     //this number deals with the ampersands, etc...
                     + 10)
                    >= (_options.MaxUrlLength))
                {
                    //we need to do a check here, this is the first one and it's already exceeded the max length we cannot continue
                    if (currBuilder.Length == 0)
                    {
                        throw new InvalidOperationException("The path for the single dependency: '" + current.FilePath + "' exceeds the max length (" + _options.MaxUrlLength + "), either reduce the single dependency's path length or increase the CompositeDependencyHandler.MaxHandlerUrlLength value");
                    }

                    //flush the current output to the array
                    files.Add(currBuilder.ToString());
                    //create some new output
                    currBuilder = new StringBuilder();
                    base64Builder = new StringBuilder();
                    builderCount++;
                }
                else
                {
                    //update the normal builder
                    currBuilder.Append(current.FilePath + ";");
                    //remove from the queue
                    remaining.Dequeue();
                }
            }

            //foreach (var a in dependencies)
            //{
            //    //update the base64 output to get the length
            //    base64Builder.Append(a.FilePath.EncodeTo64());

            //    //test if the current base64 string exceeds the max length, if so we need to split
            //    if ((base64Builder.Length
            //        + compositeFileHandlerPath.Length
            //        + stringType.Length
            //        + version.Length
            //        + 10)
            //        >= (maxLength))
            //    {
            //        //add the current output to the array
            //        files.Add(currBuilder.ToString());
            //        //create some new output
            //        currBuilder = new StringBuilder();
            //        base64Builder = new StringBuilder();
            //        builderCount++;
            //    }

            //    //update the normal builder
            //    currBuilder.Append(a.FilePath + ";");
            //}

            if (builderCount > files.Count)
            {
                files.Add(currBuilder.ToString());
            }

            //now, compress each url
            for (var i = 0; i < files.Count; i++)
            {
                //append our version to the combined url 
                var encodedFile = files[i].EncodeTo64Url();
                files[i] = GetCompositeFileUrl(encodedFile, type);
            }

            return files.ToArray();
        }

        private string GetCompositeFileUrl(string fileKey, IDependentFileType type)
        {
            var url = new StringBuilder();

            //Create a URL with a base64 query string

            const string handler = "{0}?s={1}&t={2}";
            url.Append(string.Format(handler,
                                     _options.RequestHandlerPath,
                                     Uri.EscapeUriString(fileKey), type));
            url.Append("&v=");
            url.Append(_options.Version);

            return url.ToString();
        }
    }
}