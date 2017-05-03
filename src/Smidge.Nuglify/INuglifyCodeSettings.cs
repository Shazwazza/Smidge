using NUglify.JavaScript;

namespace Smidge.Nuglify
{
    public interface INuglifyCodeSettings
    {
        CodeSettings CodeSettings { get; }
        SourceMapType SourceMapType { get; }
    }
}