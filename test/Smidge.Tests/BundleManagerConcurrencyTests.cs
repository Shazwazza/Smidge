using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Smidge.Models;
using Smidge.Options;
using Xunit;

namespace Smidge.Tests
{
    /// <summary>
    /// Regression tests for https://github.com/Shazwazza/Smidge/issues/228 - ensures that registering files into
    /// the same bundle concurrently (e.g. from multiple concurrent ASP.NET Core requests sharing the singleton
    /// <see cref="IBundleManager"/>) does not corrupt the bundle's underlying file list with null/dropped entries.
    /// </summary>
    public class BundleManagerConcurrencyTests
    {
        private static BundleManager CreateBundleManager()
        {
            var smidgeOptions = new Mock<IOptions<SmidgeOptions>>();
            smidgeOptions.Setup(opt => opt.Value).Returns(new SmidgeOptions
            {
                DefaultBundleOptions = new BundleEnvironmentOptions()
            });

            return new BundleManager(smidgeOptions.Object, Mock.Of<ILogger<BundleManager>>());
        }

        [Fact]
        public async Task AddToBundle_Called_Concurrently_From_Many_Threads_Does_Not_Corrupt_File_List()
        {
            var bundleManager = CreateBundleManager();
            const string bundleName = "concurrent-bundle";
            const int threadCount = 50;
            const int filesPerThread = 20;

            var tasks = new List<Task>();
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks.Add(Task.Run(() =>
                {
                    for (int f = 0; f < filesPerThread; f++)
                    {
                        bundleManager.AddToBundle(bundleName, new JavaScriptFile($"/scripts/thread-{threadIndex}-file-{f}.js"));
                    }
                }));
            }

            await Task.WhenAll(tasks);

            bool found = bundleManager.TryGetValue(bundleName, out Bundle bundle);
            Assert.True(found);

            // No null/corrupted entries should be present in the file list.
            Assert.DoesNotContain(bundle.Files, f => f == null);

            // Every file registered by every thread must be present exactly once.
            Assert.Equal(threadCount * filesPerThread, bundle.Files.Count);

            var expectedPaths = new HashSet<string>();
            for (int t = 0; t < threadCount; t++)
            {
                for (int f = 0; f < filesPerThread; f++)
                {
                    expectedPaths.Add($"/scripts/thread-{t}-file-{f}.js");
                }
            }

            var actualPaths = new HashSet<string>(bundle.Files.Select(x => x.FilePath));
            Assert.Equal(expectedPaths, actualPaths);
        }

        [Fact]
        public async Task AddToBundle_Called_Concurrently_On_First_Registration_Does_Not_Lose_Entries()
        {
            var bundleManager = CreateBundleManager();
            const string bundleName = "first-registration-bundle";
            const int threadCount = 30;

            var tasks = new List<Task>();
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks.Add(Task.Run(() =>
                {
                    bundleManager.AddToBundle(bundleName, new JavaScriptFile($"/scripts/first-{threadIndex}.js"));
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(bundleManager.TryGetValue(bundleName, out Bundle bundle));
            Assert.DoesNotContain(bundle.Files, f => f == null);
            Assert.Equal(threadCount, bundle.Files.Count);
        }
    }
}
