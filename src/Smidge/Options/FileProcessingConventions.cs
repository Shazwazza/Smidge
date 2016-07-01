using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Smidge.FileProcessors;

namespace Smidge.Options
{
    /// <summary>
    /// The main access point to get the filtered conventions
    /// </summary>
    public class FileProcessingConventions
    {
        private readonly IOptions<SmidgeOptions> _options;
        private readonly IEnumerable<IFileProcessingConvention> _allConventions;

        public FileProcessingConventions(IOptions<SmidgeOptions> options, IEnumerable<IFileProcessingConvention> allConventions)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (allConventions == null) throw new ArgumentNullException(nameof(allConventions));
            _options = options;
            _allConventions = allConventions;
        }

        private IFileProcessingConvention[] _filtered;

        /// <summary>
        /// Returns all conventions that match the options filter
        /// </summary>
        public IEnumerable<IFileProcessingConvention> Values
        {
            get
            {
                return _filtered 
                       ?? (_filtered = _allConventions.Where(x => _options.Value.FileProcessingConventions.Contains(x.GetType())).ToArray());
            }
        }
    }
}