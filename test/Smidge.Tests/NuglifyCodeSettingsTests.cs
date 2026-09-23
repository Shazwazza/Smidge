using NUglify;
using NUglify.JavaScript;
using Smidge.Nuglify;
using Xunit;

namespace Smidge.Tests
{
    public class NuglifyCodeSettingsTests
    {
        [Fact]
        public void Default_Settings_Minify_Ace_Verilog_Regex()
        {
            const string javascript = """
                var rule = { regex:/\\(?:[ntvfa\\"]|[0-7]{1,3}|\x[a-fA-F\d]{1,2}|)/ };
                """;

            var settings = new NuglifyCodeSettings();
            var result = Uglify.Js(javascript, "mode-verilog.js", settings.CodeSettings);

            Assert.False(result.HasErrors, string.Join(",", result.Errors));
            Assert.NotEmpty(result.Code);
        }

        [Fact]
        public void Custom_Settings_Preserve_Nuglify_Diagnostics()
        {
            const string javascript = """
                var rule = { regex:/\\(?:[ntvfa\\"]|[0-7]{1,3}|\x[a-fA-F\d]{1,2}|)/ };
                """;

            var settings = new NuglifyCodeSettings(new CodeSettings());
            var result = Uglify.Js(javascript, "mode-verilog.js", settings.CodeSettings);

            Assert.True(result.HasErrors);
        }
    }
}
