using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace Smidge.Models
{
    internal class BundleModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            throw new NotImplementedException();
        }
    }
}