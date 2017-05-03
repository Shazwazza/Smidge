using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Smidge.FileProcessors;

namespace Smidge.CompositeFiles
{
    /// <summary>
    /// Tracks state for a bundle of files and is used to combine files into one
    /// </summary>
    public class BundleContext : IDisposable
    {        
        public BundleContext()
        {
        }

        public BundleContext(string bundleFileName)
        {
            _bundleFileName = bundleFileName;
        }

        private readonly List<Func<string>> _appenders = new List<Func<string>>();
        private readonly List<Func<string>> _prependers = new List<Func<string>>();
        private readonly string _bundleFileName;

        /// <summary>
        /// Allows for any <see cref="IPreProcessor"/> to track state among the collection of files
        /// </summary>
        public IDictionary<string, object> Items { get; private set; } = new Dictionary<string, object>();

        public string BundleFileName
        {
            get { return _bundleFileName ?? "generated_" + Guid.NewGuid(); }
        }

        public void AddAppender(Func<string> appender)
        {
            _appenders.Add(appender);
        }

        public void AddPrepender(Func<string> prepender)
        {
            _prependers.Add(prepender);
        }

        /// <summary>
        /// Combines streams into a single stream
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        /// <remarks>
        /// This does not dispose the input streams
        /// </remarks>
        public async Task<Stream> GetCombinedStreamAsync(IEnumerable<Stream> inputs)
        {
            //TODO: Should we use a buffer pool here?

            var semicolon = Encoding.UTF8.GetBytes(";");
            var ms = new MemoryStream();
            //prependers
            foreach (var prepender in _prependers)
            {
                var bytes = Encoding.UTF8.GetBytes(prepender());
                await ms.WriteAsync(bytes, 0, bytes.Length);
            }
            
            //files
            foreach (var input in inputs)
            {
                await input.CopyToAsync(ms);
                await ms.WriteAsync(semicolon, 0, semicolon.Length);
            }

            //prependers
            foreach (var appender in _appenders)
            {
                var bytes = Encoding.UTF8.GetBytes(appender());
                await ms.WriteAsync(bytes, 0, bytes.Length);
            }

            //ensure it's reset
            ms.Position = 0;
            return ms;
        }

        public void Dispose()
        {
            Items?.Clear();
            _appenders?.Clear();
            _prependers?.Clear();
            Items = null;
        }
    }
}
