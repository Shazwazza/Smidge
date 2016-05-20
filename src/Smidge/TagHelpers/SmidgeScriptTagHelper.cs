using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.WebEncoders;

namespace Smidge.TagHelpers
{

    [HtmlTargetElement("script", Attributes = "src")]
    public class SmidgeScriptTagHelper : TagHelper
    {
        private readonly SmidgeHelper _smidgeHelper;
        private readonly BundleManager _bundleManager;
        private readonly HtmlEncoder _encoder;

        public SmidgeScriptTagHelper(SmidgeHelper smidgeHelper, BundleManager bundleManager, HtmlEncoder encoder)
        {
            _smidgeHelper = smidgeHelper;
            _bundleManager = bundleManager;
            _encoder = encoder;
        }

        [HtmlAttributeName("src")]
        public string Source { get; set; }

        [HtmlAttributeName("debug")]
        public bool Debug { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (_bundleManager.Exists(Source))
            {
                var result = (await _smidgeHelper.GenerateJsUrlsAsync(Source, Debug)).ToArray();
                var currAttr = output.Attributes.ToDictionary(x => x.Name, x => x.Value);
                using (var writer = new StringWriter())
                {
                    foreach (var s in result)
                    {
                        var builder = new TagBuilder(output.TagName)
                        {
                            TagRenderMode = TagRenderMode.Normal
                        };
                        builder.MergeAttributes(currAttr);
                        builder.Attributes["src"] = s;

                        builder.WriteTo(writer, _encoder);
                    }
                    writer.Flush();
                    output.PostElement.SetContent(writer.ToString());
                }
                //This ensures the original tag is not written.
                output.TagName = null;
            }           
        }
    }
}