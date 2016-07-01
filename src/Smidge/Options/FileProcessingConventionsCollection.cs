using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Smidge.FileProcessors;

namespace Smidge.Options
{
    /// <summary>
    /// Custom collection to validate types
    /// </summary>
    internal class FileProcessingConventionsCollection : Collection<Type>
    {
        protected override void InsertItem(int index, Type item)
        {
            Validate(item);
            base.InsertItem(index, item);
        }
        
        protected override void SetItem(int index, Type item)
        {
            Validate(item);
            base.SetItem(index, item);
        }

        private void Validate(Type item)
        {
            if (!typeof(IFileProcessingConvention).IsAssignableFrom(item))
                throw new InvalidOperationException("File processing conventions must implement " + typeof(IFileProcessingConvention));
        }
    }
}