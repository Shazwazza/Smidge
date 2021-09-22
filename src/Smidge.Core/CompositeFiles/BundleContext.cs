using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Smidge.FileProcessors;
using Smidge.Models;

namespace Smidge.CompositeFiles
{
    /// <summary>
    /// Tracks state for a bundle of files and is used to combine files into one
    /// </summary>
    public class BundleContext : IDisposable
    {
        /// <summary>
        /// Creates an empty <see cref="BundleContext"/> which does not track prependers or appenders
        /// </summary>
        /// <returns></returns>
        public static BundleContext CreateEmpty(string cacheBusterValue)
        {
            return new BundleContext(cacheBusterValue);
        }

        private BundleContext(string cacheBusterValue)
        {
            CacheBusterValue = cacheBusterValue;
        }

        public BundleContext(string cacheBusterValue, IRequestModel bundleRequest, string bundleCompositeFilePath)
        {
            // TODO: Should the cache buster even be on the request model?
            CacheBusterValue = cacheBusterValue; // BundleRequest.CacheBuster.GetValue();
            BundleRequest = bundleRequest;
            _bundleCompositeFilePath = bundleCompositeFilePath;
        }

        private readonly List<Func<Task<string>>> _appenders = new List<Func<Task<string>>>();
        private readonly List<Func<Task<string>>> _prependers = new List<Func<Task<string>>>();        
        private readonly string _bundleCompositeFilePath;

        public IRequestModel BundleRequest { get; }

        /// <summary>
        /// Allows for any <see cref="IPreProcessor"/> to track state among the collection of files
        /// </summary>
        public IDictionary<string, object> Items { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Returns the path of the composite bundle file
        /// </summary>
        public string BundleCompositeFilePath
        {
            get
            {
                if (_bundleCompositeFilePath == null) throw new NotSupportedException("No file available in an empty " + nameof(BundleContext));
                return _bundleCompositeFilePath;
            }
        }


        /// <summary>
        /// Returns the bundle file name
        /// </summary>
        /// <remarks>
        /// If it's an empty bundle context (i.e. it's not processing a real bundle but only a composite file) then this will be generated
        /// </remarks>
        public string BundleName => BundleRequest?.FileKey ?? "generated_" + Guid.NewGuid();

        public string FileExtension => BundleRequest?.Extension ?? string.Empty;

        public string CacheBusterValue { get; }

        public void AddAppender(Func<Task<string>> appender)
        {
            if (_bundleCompositeFilePath == null) return;
            _appenders.Add(appender);
        }

        public void AddPrepender(Func<Task<string>> prepender)
        {
            if (_bundleCompositeFilePath == null) return;
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
        public async Task<Stream> GetCombinedStreamAsync(IEnumerable<Stream> inputs, string delimeter)
        {
            // TODO: Should we use a buffer pool here?
            // TODO: We should be using Span<T> for reading strings, streams aren't really doing us many favors here

            var d = Encoding.UTF8.GetBytes(delimeter);

            var ms = new MemoryStream();
            //prependers
            foreach (var prepender in _prependers)
            {
                var p = await prepender();
                var bytes = Encoding.UTF8.GetBytes(p);
                await ms.WriteAsync(bytes, 0, bytes.Length);
            }

            //files
            foreach (var input in inputs)
            {
                await input.CopyToAsync(ms);
                await ms.WriteAsync(d, 0, d.Length);
            }

            //prependers
            foreach (var appender in _appenders)
            {
                var a = await appender();
                var bytes = Encoding.UTF8.GetBytes(a);
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
