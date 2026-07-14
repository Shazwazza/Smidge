using System;
using System.IO;
using System.Text;
using NUglify.JavaScript;
using NUglify.JavaScript.Syntax;

namespace Smidge.Nuglify
{
    /// <summary>
    /// Used to create source maps over multiple files during a single bundle context
    /// </summary>
    /// <remarks>
    /// Typically the standard V3SourceMap requires that you provide it a list of files to proces up-front and it will return a single source map
    /// for them all, however we don't have the ability to tell a single instance up-front which files to process we just know the files when we
    /// start processing them. Luckily the underlying V3SourceMap can work with this since anytime Nuglify processes a JS file it will update the
    /// underlying V3SourceMap document with a new source and append to it's sources list.
    ///
    /// So this is deferred because it does not write to the processed JS file, it defers the output and writes to a string builder which can be retrieved later.
    /// </remarks>
    public class V3DeferredSourceMap : ISourceMap
    {
        public const string ImplementationName = "V3Inline";

        public SourceMapType SourceMapType { get; }

        private readonly V3SourceMap _wrapped;
        private readonly StringBuilder _mapBuilder;
        private string _mapPath;

        /// <summary>
        /// Creates a new inline source map
        /// </summary>
        /// <param name="wrapped"></param>
        /// <param name="mapBuilder"></param>
        /// <param name="sourceSourceMapType"></param>
        public V3DeferredSourceMap(V3SourceMap wrapped, StringBuilder mapBuilder, SourceMapType sourceSourceMapType)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
            _mapBuilder = mapBuilder ?? throw new ArgumentNullException(nameof(mapBuilder));
            SourceMapType = sourceSourceMapType;
            // Disable relative paths, this will always fail and make source maps super slow.
            _wrapped.MakePathsRelative = false;
        }

        public void Dispose()
        {
            _wrapped.Dispose();
        }

        public void StartPackage(string sourcePath, string mapPath)
        {
            _mapPath = mapPath;
            _wrapped.StartPackage(sourcePath, mapPath);
        }

        /// <summary>
        /// Override to end the output of all files processed, this is required if not appending source maps to each file
        /// </summary>
        public void EndPackage()
        {
            _wrapped.EndPackage();

            //need to dispose the wrapped source map which is what is used to generate the whole thing
            _wrapped.Dispose();

            //get the created map and base64 it
            SourceMapOutput = _mapBuilder.ToString();
        }

        public object StartSymbol(AstNode node, int startLine, int startColumn)
        {
            return _wrapped.StartSymbol(node, startLine, startColumn);
        }

        public void MarkSegment(AstNode node, int startLine, int startColumn, string name, SourceContext context)
        {
            _wrapped.MarkSegment(node, startLine, startColumn, name, context);
        }

        public void EndSymbol(object symbol, int endLine, int endColumn, string parentContext)
        {
            _wrapped.EndSymbol(symbol, endLine, endColumn, parentContext);
        }

        public void EndOutputRun(int lineNumber, int columnPosition)
        {
            _wrapped.EndOutputRun(lineNumber, columnPosition);
        }

        /// <summary>
        /// Override EndFile to ensure nothing is written to the source file
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="newLine"></param>
        public void EndFile(TextWriter writer, string newLine)
        {
            //Do nothing here, this is different than the normal implementation which would write to the source file, we want to defer this operation
        }

        public void NewLineInsertedInOutput()
        {
            _wrapped.NewLineInsertedInOutput();
        }

        public string Name => ImplementationName;

        public string SourceRoot
        {
            get => _wrapped.SourceRoot;
            set => _wrapped.SourceRoot = value;
        }

        public bool SafeHeader
        {
            get => _wrapped.SafeHeader;
            set => _wrapped.SafeHeader = value;
        }

        public string GetInlineSourceMapMarkup()
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(SourceMapOutput));
            return string.Format("//# sourceMappingURL=data:application/json;charset=utf-8;base64,{0}", base64);
        }

        public string GetExternalFileSourceMapMarkup(string mapPath)
        {
            return string.Format("//# sourceMappingURL={0}", mapPath);
        }

        /// <summary>
        /// Reutrns the Source map output once processing is complete
        /// </summary>
        public string SourceMapOutput { get; private set; }
    }
}
