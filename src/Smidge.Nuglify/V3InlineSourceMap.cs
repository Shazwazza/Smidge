using System;
using System.IO;
using System.Text;
using NUglify.JavaScript;
using NUglify.JavaScript.Syntax;

namespace Smidge.Nuglify
{
    public class V3InlineSourceMap : ISourceMap
    {

        public const string ImplementationName = "V3Inline";

        private readonly V3SourceMap _wrapped;
        private readonly StringBuilder _mapBuilder;
        private string _mapPath;

        public V3InlineSourceMap(V3SourceMap wrapped, StringBuilder mapBuilder)
        {
            if (wrapped == null) throw new ArgumentNullException(nameof(wrapped));
            if (mapBuilder == null) throw new ArgumentNullException(nameof(mapBuilder));

            _wrapped = wrapped;
            _mapBuilder = mapBuilder;
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
        /// Override to 
        /// </summary>
        public void EndPackage()
        {
            _wrapped.EndPackage();
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
        /// Override EndFile to create an inline source map
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="newLine"></param>
        public void EndFile(TextWriter writer, string newLine)
        {
            if (writer == null || string.IsNullOrWhiteSpace(_mapPath))
                return;
            writer.Write(newLine);

            //need to dispose the wrapped source map which is what is used to generate the whole thing
            _wrapped.Dispose();

            //get the created map and base64 it
            var mapOutput = _mapBuilder.ToString();
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(mapOutput));
            
            //write the inline map
            writer.Write("//# sourceMappingURL=data:application/json;charset=utf-8;base64,{0}", base64);

            writer.Write(newLine);
            
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
    }
}