using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smidge.Controllers
{
    internal sealed class KeyedAsyncLock
    {
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        public async Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken)
        {
            Entry entry;
            lock (_entries)
            {
                if (!_entries.TryGetValue(key, out entry))
                {
                    entry = new Entry();
                    _entries.Add(key, entry);
                }

                // Count waiters before awaiting so a handoff cannot remove their semaphore.
                entry.References++;
            }

            try
            {
                await entry.Semaphore.WaitAsync(cancellationToken);
            }
            catch
            {
                RemoveReference(key, entry);
                throw;
            }

            return new Lease(this, key, entry);
        }

        private void RemoveReference(string key, Entry entry)
        {
            lock (_entries)
            {
                if (--entry.References == 0)
                {
                    _entries.Remove(key);
                    entry.Semaphore.Dispose();
                }
            }
        }

        private sealed class Entry
        {
            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
            public int References { get; set; }
        }

        private sealed class Lease : IDisposable
        {
            private KeyedAsyncLock _owner;
            private readonly string _key;
            private readonly Entry _entry;

            public Lease(KeyedAsyncLock owner, string key, Entry entry)
            {
                _owner = owner;
                _key = key;
                _entry = entry;
            }

            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref _owner, null);
                if (owner != null)
                {
                    _entry.Semaphore.Release();
                    owner.RemoveReference(_key, _entry);
                }
            }
        }
    }
}
