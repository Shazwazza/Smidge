using Microsoft.AspNet.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace Smidge.Models
{
    internal class BundleModelBinder : IModelBinder
    {
        public Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            throw new NotImplementedException();
        }
    }
}