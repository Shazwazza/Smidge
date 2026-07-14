using Smidge.CompositeFiles;
using System.Threading.Tasks;

namespace Smidge.Nuglify
{
    /// <summary>
    /// A class that returns the JS string at the bottom of a JS file to declare the source map
    /// </summary>
    public interface ISourceMapDeclaration
    {
        Task<string> GetDeclarationAsync(BundleContext bundleContext, V3DeferredSourceMap sourceMap);
    }
}