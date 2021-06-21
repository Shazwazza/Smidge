using System;
using Smidge.Models;

namespace Smidge.Options
{
    public sealed class FileWatchOptions
    {
        public bool Enabled { get; set; }

        //TODO: Add an event here to be able to subscribe to file changes?
        public void Changed(FileWatchEventArgs args)
        {
            OnFileModified(args);
        }

        public event EventHandler<FileWatchEventArgs> FileModified;


        private void OnFileModified(FileWatchEventArgs e)
        {
            FileModified?.Invoke(this, e);
        }
    }
}