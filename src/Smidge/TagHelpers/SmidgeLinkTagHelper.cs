using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Smidge.TagHelpers
{
    [HtmlTargetElement("link", Attributes = "href", TagStructure = TagStructure.WithoutEndTag)]
    public class SmidgeLinkTagHelper : TagHelper
    {
        private readonly SmidgeHelper _smidgeHelper;
        private readonly IBundleManager _bundleManager;
        private readonly HtmlEncoder _encoder;

        public SmidgeLinkTagHelper(SmidgeHelper smidgeHelper, IBundleManager bundleManager, HtmlEncoder encoder)
        {
            _smidgeHelper = smidgeHelper;
            _bundleManager = bundleManager;
            _encoder = encoder;
        }

        /// <summary>
        /// TODO: Need to figure out why we need this. If the order is default and executes 'after' the 
        /// default tag helpers like the script tag helper and url resolution tag helper, the url resolution
        /// doesn't actually work, it simply doesn't get passed through. Not sure if this is a bug or if I'm 
        /// doing it wrong. In the meantime, setting this to execute before the defaults works.
        /// </summary>
        public override int Order => -2000;

        [HtmlAttributeName("href")]
        public string Source { get; set; }

        [HtmlAttributeName("debug")]
        public bool Debug { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Pass through attribute that is also a well-known HTML attribute.
            // this is required to make sure that other tag helpers executing against this element have
            // the value copied across
            if (Source != null)
            {
                output.CopyHtmlAttribute("href", context);
            }

            if (_bundleManager.Exists(Source))
            {
                var result = (await _smidgeHelper.GenerateCssUrlsAsync(Source, Debug)).ToArray();
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
                    output.PostElement.SetHtmlContent(new HtmlString(writer.ToString()));
                }
                //This ensures the original tag is not written.
                output.TagName = null;
            }            
        }
    }
}