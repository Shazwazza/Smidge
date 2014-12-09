using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Smidge.CompositeFiles;
using Smidge.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smidge
{
    /// <summary>
    /// This class is used to retrieve the services defined in the SmidgeOptions from DI
    /// </summary>
    public sealed class DefaultFileProcessors
    {
        private IOptions<SmidgeOptions> _options;
        private IServiceProvider _serviceScope;
        private IMinifier _cssMinifier;
        private IMinifier _javaScriptMinifier;       

        public DefaultFileProcessors(IServiceProvider serviceProvider, IOptions<SmidgeOptions> options)
        {
            _options = options;
            _serviceScope = serviceProvider;
        }

        public IMinifier CssMinifier
        {
            get
            {
                return _cssMinifier ?? (_cssMinifier = FindService(_options.Options.DefaultCssMinifier));
            }
        }

        public IMinifier JavaScriptMinifier
        {
            get
            {
                return _javaScriptMinifier ?? (_javaScriptMinifier = FindService(_options.Options.DefaultJavaScriptMinifier));
            }
        }

        private IMinifier FindService(Type t)
        {
            var found = _serviceScope.GetRequiredService<IEnumerable<IMinifier>>()
                .FirstOrDefault(x => x.GetType() == t);

            if (found == null)
            {
                throw new InvalidOperationException("The default type registered " + t + " could not be found in the DI container, ensure it is registered as " + typeof(IMinifier));
            }

            return found;
        }
    }

    
    
    
}