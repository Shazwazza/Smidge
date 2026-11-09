using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Smidge.Cache;
using Smidge.CompositeFiles;
using Smidge.Controllers;
using Smidge.FileProcessors;
using Smidge.Hashing;
using Smidge.Models;
using Smidge.Options;
using Xunit;

namespace Smidge.Tests
{
    public class SmidgeRequestHandlerConcurrencyTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Concurrent_Cold_Requests_Complete_And_Write_Once(bool namedBundle)
        {
            var context = new HandlerContext(namedBundle);
            var writeStarted = NewSignal();
            var finishWrite = NewSignal();
            context.BeforeWrite = async () =>
            {
                writeStarted.TrySetResult(true);
                await finishWrite.Task;
            };

            var first = context.RequestAsync();
            await writeStarted.Task.WaitAsync(Timeout);
            var queued = Enumerable.Range(0, 19).Select(_ => context.RequestAsync()).ToArray();
            try
            {
                Assert.All(queued, task => Assert.False(task.IsCompleted));
            }
            finally
            {
                finishWrite.TrySetResult(true);
            }

            var results = await Task.WhenAll(queued.Prepend(first)).WaitAsync(Timeout);
            foreach (var result in results)
            {
                await AssertContentAsync(result);
            }
            Assert.Equal(1, context.Writes);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Cancelled_Waiter_Leaves_Other_Requests_Blocked_Until_Writer_Finishes(bool namedBundle)
        {
            var context = new HandlerContext(namedBundle);
            var finishWrite = NewSignal();
            context.BeforeWrite = () => finishWrite.Task;
            var first = context.RequestAsync();
            using var cancellation = new CancellationTokenSource();
            var cancelled = context.RequestAsync(cancellation.Token);
            var next = context.RequestAsync();
            try
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.WaitAsync(Timeout));
                Assert.False(first.IsCompleted);
                Assert.False(next.IsCompleted);
            }
            finally
            {
                finishWrite.TrySetResult(true);
            }

            foreach (var result in await Task.WhenAll(first, next).WaitAsync(Timeout))
            {
                await AssertContentAsync(result);
            }
            Assert.Equal(1, context.Writes);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Failed_Writer_Releases_Queued_Requests_To_Retry(bool namedBundle)
        {
            var context = new HandlerContext(namedBundle);
            var failWrite = NewSignal();
            context.BeforeWrite = async () =>
            {
                if (context.Writes == 1)
                {
                    await failWrite.Task;
                    throw new IOException("Cache write failed");
                }
            };

            var first = context.RequestAsync();
            var second = context.RequestAsync();
            var third = context.RequestAsync();
            failWrite.TrySetResult(true);

            await Assert.ThrowsAsync<IOException>(() => first.WaitAsync(Timeout));
            foreach (var result in await Task.WhenAll(second, third).WaitAsync(Timeout))
            {
                await AssertContentAsync(result);
            }
            Assert.Equal(2, context.Writes);
        }

        [Fact]
        public async Task Stale_Bundle_Requests_Release_The_Lock_After_BadRequest()
        {
            var context = new HandlerContext(namedBundle: true);
            var finishWrite = NewSignal();
            context.BeforeWrite = () => finishWrite.Task;
            var first = context.RequestAsync();
            var second = context.RequestAsync(cacheBuster: "stale");
            var third = context.RequestAsync(cacheBuster: "stale");
            finishWrite.TrySetResult(true);

            await AssertContentAsync(await first.WaitAsync(Timeout));
            foreach (var result in await Task.WhenAll(second, third).WaitAsync(Timeout))
            {
                Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
            }
            Assert.Equal(1, context.Writes);
        }

        [Fact]
        public async Task Missing_Composite_Files_Release_The_Lock_After_NotFound()
        {
            var context = new HandlerContext(namedBundle: false);
            context.SourceFile.SetupGet(x => x.Exists).Returns(false);
            for (var i = 0; i < 3; i++)
            {
                var result = await context.RequestAsync().WaitAsync(Timeout);
                Assert.Equal(StatusCodes.Status404NotFound, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
            }
            Assert.Equal(0, context.Writes);
        }

        private static TaskCompletionSource<bool> NewSignal()
            => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static async Task AssertContentAsync(IResult result)
        {
            using var stream = Assert.IsType<FileStreamHttpResult>(result).FileStream;
            using var reader = new StreamReader(stream);
            Assert.Equal("var answer = 42;;\n", await reader.ReadToEndAsync());
        }

        private sealed class HandlerContext
        {
            private readonly bool _namedBundle;
            private readonly string _name = Guid.NewGuid().ToString("N");
            private readonly Mock<IBundleManager> _bundles = new Mock<IBundleManager>();
            private readonly SmidgeRequestHandler _handler;
            private byte[] _cached;
            private int _writes;

            public HandlerContext(bool namedBundle)
            {
                _namedBundle = namedBundle;
                var options = new BundleEnvironmentOptions();
                options.ProductionOptions.SetCacheBusterType<TestCacheBuster>();
                _bundles.SetupGet(x => x.DefaultBundleOptions).Returns(options);
                var webFile = new JavaScriptFile("~/test.js");
                var bundle = new Bundle(_name, new List<IWebFile> { webFile });
                _bundles.Setup(x => x.TryGetValue(_name, out bundle)).Returns(true);

                SourceFile.SetupGet(x => x.Exists).Returns(true);
                SourceFile.Setup(x => x.CreateReadStream())
                    .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("var answer = 42;")));

                var cache = new Mock<ICacheFileSystem>();
                var cachePath = _name;
                cache.Setup(x => x.GetCachedCompositeFile(It.IsAny<string>(), It.IsAny<CompressionType>(), _name, out cachePath))
                    .Returns((string version, CompressionType compression, string key, string path) =>
                    {
                        if (_cached == null || version != "current")
                        {
                            return new NotFoundFileInfo(path);
                        }
                        var cachedFile = new Mock<IFileInfo>();
                        cachedFile.SetupGet(x => x.Exists).Returns(true);
                        cachedFile.Setup(x => x.CreateReadStream()).Returns(() => new MemoryStream(_cached));
                        return cachedFile.Object;
                    });
                cache.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(SourceFile.Object);
                cache.Setup(x => x.GetCacheFile(It.IsAny<IWebFile>(), It.IsAny<Func<IFileInfo>>(),
                        It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), out cachePath))
                    .Returns(SourceFile.Object);
                cache.Setup(x => x.WriteFileAsync(_name, It.IsAny<Stream>()))
                    .Returns(async (string path, Stream stream) =>
                    {
                        Interlocked.Increment(ref _writes);
                        await BeforeWrite();
                        using var copy = new MemoryStream();
                        await stream.CopyToAsync(copy);
                        _cached = copy.ToArray();
                    });

                var fileSystem = new Mock<ISmidgeFileSystem>();
                fileSystem.SetupGet(x => x.CacheFileSystem).Returns(cache.Object);
                var generator = new Mock<IBundleFileSetGenerator>();
                generator.Setup(x => x.GetOrderedFileSet(bundle, It.IsAny<PreProcessPipeline>()))
                    .Returns(new[] { webFile });
                var pipeline = new Mock<PreProcessPipelineFactory>(new Lazy<IEnumerable<IPreProcessor>>(() => Array.Empty<IPreProcessor>()));
                pipeline.Setup(x => x.CreateDefault(It.IsAny<WebFileType>()))
                    .Returns(new PreProcessPipeline(Array.Empty<IPreProcessor>()));
                var processor = new Mock<IPreProcessManager>();
                processor.Setup(x => x.ProcessAndCacheFileAsync(It.IsAny<IWebFile>(), It.IsAny<BundleOptions>(), It.IsAny<BundleContext>()))
                    .Returns(Task.CompletedTask);
                _handler = new SmidgeRequestHandler(fileSystem.Object, _bundles.Object, generator.Object, pipeline.Object,
                    processor.Object, NullLogger<SmidgeRequestHandler>.Instance,
                    new CacheBusterResolver(new[] { new TestCacheBuster() }));
            }

            public Mock<IFileInfo> SourceFile { get; } = new Mock<IFileInfo>();
            public Func<Task> BeforeWrite { get; set; } = () => Task.CompletedTask;
            public int Writes => Volatile.Read(ref _writes);

            public Task<IResult> RequestAsync(CancellationToken cancellationToken = default, string cacheBuster = "current")
            {
                var urls = new Mock<IUrlManager>();
                urls.Setup(x => x.ParsePath(It.IsAny<string>())).Returns(new ParsedUrlPath
                {
                    Names = new[] { _name },
                    CacheBusterValue = cacheBuster,
                    WebType = WebFileType.Js
                });
                var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
                var requestHelper = new Mock<IRequestHelper>();
                requestHelper.Setup(x => x.GetClientCompression(It.IsAny<IHeaderDictionary>())).Returns(CompressionType.None);
                if (_namedBundle)
                {
                    accessor.HttpContext.Request.RouteValues["bundle"] = _name;
                    return _handler.Bundle(new BundleRequestModel(urls.Object, accessor, requestHelper.Object, _bundles.Object), cancellationToken);
                }

                accessor.HttpContext.Request.RouteValues["file"] = _name;
                var hasher = new Mock<IHasher>();
                hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns(_name);
                return _handler.Composite(new CompositeFileModel(hasher.Object, urls.Object, accessor, requestHelper.Object), cancellationToken);
            }
        }

        private sealed class TestCacheBuster : ICacheBuster
        {
            public string GetValue() => "current";
        }
    }
}
