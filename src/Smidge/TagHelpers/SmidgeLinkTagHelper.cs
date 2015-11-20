using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.WebEncoders;

namespace Smidge.TagHelpers
{
    [HtmlTargetElement("link", Attributes = "href")]
    public class SmidgeLinkTagHelper : TagHelper
    {
        private readonly SmidgeHelper _smidgeHelper;
        private readonly BundleManager _bundleManager;
        private readonly IHtmlEncoder _encoder;

        public SmidgeLinkTagHelper(SmidgeHelper smidgeHelper, BundleManager bundleManager, IHtmlEncoder encoder)
        {
            _smidgeHelper = smidgeHelper;
            _bundleManager = bundleManager;
            _encoder = encoder;
        }

        [HtmlAttributeName("href")]
        public string Source { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (_bundleManager.Exists(Source))
            {
                var result = (await _smidgeHelper.GenerateCssUrlsAsync(Source)).ToArray();
                var currAttr = output.Attributes.ToDictionary(x => x.Name, x => x.Value);
                using (var writer = new StringWriter())
                {
                    foreach (var s in result)
                    {
                        var builder = new TagBuilder(output.TagName)
                        {
                            TagRenderMode = TagRenderMode.SelfClosing
                        };
                        builder.MergeAttributes(currAttr);
                        builder.Attributes["href"] = s;

                        builder.WriteTo(writer, _encoder);
                    }
                    writer.Flush();
                    output.PostElement.SetContent(new HtmlString(writer.ToString()));
                }
                //This ensures the original tag is not written.
                output.TagName = null;
            }
            else
            {
                //use what is there
                output.Attributes["href"] = Source;
            }
        }
    }
}