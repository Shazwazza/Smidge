using System;

namespace Smidge.Models
{
    public class FileWatchEventArgs : EventArgs
    {
        public WatchedFile File { get; }
        public FileSystemHelper FileSystemHelper { get; }

        public FileWatchEventArgs(WatchedFile file, FileSystemHelper fileSystemHelper)
        {
            File = file;
            FileSystemHelper = fileSystemHelper;
        }
    }
}