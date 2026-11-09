using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Smidge.Controllers;
using Xunit;

namespace Smidge.Tests
{
    public class KeyedAsyncLockTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task Queued_And_New_Arrivals_Use_The_Same_Semaphore()
        {
            var keyedLock = new KeyedAsyncLock();
            using var first = await keyedLock.AcquireAsync("bundle", default);
            var secondTask = keyedLock.AcquireAsync("bundle", default);
            var thirdTask = keyedLock.AcquireAsync("bundle", default);
            Assert.False(secondTask.IsCompleted);
            Assert.False(thirdTask.IsCompleted);

            first.Dispose();
            using var second = await secondTask.WaitAsync(Timeout);
            var fourthTask = keyedLock.AcquireAsync("bundle", default);
            Assert.False(thirdTask.IsCompleted);
            Assert.False(fourthTask.IsCompleted);

            second.Dispose();
            using var third = await thirdTask.WaitAsync(Timeout);
            Assert.False(fourthTask.IsCompleted);
            third.Dispose();
            using var fourth = await fourthTask.WaitAsync(Timeout);
            fourth.Dispose();
            AssertNoEntries(keyedLock);
        }

        [Fact]
        public async Task Cancelling_A_Waiter_Does_Not_Release_The_Holder()
        {
            var keyedLock = new KeyedAsyncLock();
            using var first = await keyedLock.AcquireAsync("bundle", default);
            using var cancellation = new CancellationTokenSource();
            var cancelledTask = keyedLock.AcquireAsync("bundle", cancellation.Token);
            var nextTask = keyedLock.AcquireAsync("bundle", default);

            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledTask.WaitAsync(Timeout));
            Assert.False(nextTask.IsCompleted);

            first.Dispose();
            using var next = await nextTask.WaitAsync(Timeout);
            next.Dispose();
            AssertNoEntries(keyedLock);
        }

        [Fact]
        public async Task Different_Keys_Do_Not_Block_Each_Other()
        {
            var keyedLock = new KeyedAsyncLock();
            using var first = await keyedLock.AcquireAsync("first", default);
            using var second = await keyedLock.AcquireAsync("second", default).WaitAsync(Timeout);
        }

        [Fact]
        public async Task Unused_Entries_Are_Removed_After_Release_Or_Cancellation()
        {
            var keyedLock = new KeyedAsyncLock();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            for (var i = 0; i < 100; i++)
            {
                var key = i.ToString();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => keyedLock.AcquireAsync(key, cancellation.Token));
                using (await keyedLock.AcquireAsync(key, default))
                {
                }
            }

            AssertNoEntries(keyedLock);
        }

        [Fact]
        public async Task Disposing_A_Lease_Twice_Does_Not_Release_The_Next_Holder()
        {
            var keyedLock = new KeyedAsyncLock();
            var first = await keyedLock.AcquireAsync("bundle", default);
            first.Dispose();
            using var second = await keyedLock.AcquireAsync("bundle", default);
            first.Dispose();

            var thirdTask = keyedLock.AcquireAsync("bundle", default);
            Assert.False(thirdTask.IsCompleted);
            second.Dispose();
            using var third = await thirdTask.WaitAsync(Timeout);
        }

        [Fact]
        public async Task Concurrent_Acquisition_And_Release_Remain_Exclusive()
        {
            var keyedLock = new KeyedAsyncLock();
            var active = 0;
            var workers = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
            {
                for (var i = 0; i < 100; i++)
                {
                    using (await keyedLock.AcquireAsync("bundle", default))
                    {
                        Assert.Equal(1, Interlocked.Increment(ref active));
                        await Task.Yield();
                        Assert.Equal(0, Interlocked.Decrement(ref active));
                    }
                }
            }));

            await Task.WhenAll(workers).WaitAsync(Timeout);
            AssertNoEntries(keyedLock);
        }

        private static void AssertNoEntries(KeyedAsyncLock keyedLock)
        {
            var entries = typeof(KeyedAsyncLock).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Empty(Assert.IsAssignableFrom<IDictionary>(entries.GetValue(keyedLock)));
        }
    }
}
