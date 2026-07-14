using System;
using System.IO;

namespace Smidge.Tests
{
    /// <summary>
    /// A temporary directory that is deleted on dispose, used by file system tests.
    /// </summary>
    internal sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smidge-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }
}
