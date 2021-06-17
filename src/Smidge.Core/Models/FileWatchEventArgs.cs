using Microsoft.Extensions.FileProviders;
using System;

namespace Smidge.Models
{
    public class FileWatchEventArgs : EventArgs
    {
        public WatchedFile File { get; }

        public FileWatchEventArgs(WatchedFile file)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
        }
    }
}