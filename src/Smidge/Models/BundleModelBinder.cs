using Microsoft.AspNet.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace Smidge.Models
{
    internal class BundleModelBinder : IModelBinder
    {
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            throw new NotImplementedException();
        }
    }
}