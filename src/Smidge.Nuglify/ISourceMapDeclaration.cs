using Smidge.CompositeFiles;

namespace Smidge.Nuglify
{
    /// <summary>
    /// A class that returns the JS string at the bottom of a JS file to declare the source map
    /// </summary>
    public interface ISourceMapDeclaration
    {
        string GetDeclaration(BundleContext bundleContext, V3DeferredSourceMap sourceMap);
    }
}