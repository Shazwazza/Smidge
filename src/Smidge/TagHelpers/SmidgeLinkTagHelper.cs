using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smidge.TagHelpers
{
    [HtmlTargetElement("link", Attributes = HrefAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class SmidgeLinkTagHelper : TagHelper
    {
        private const string HrefAttributeName = "href";
        private readonly IBundleManager _bundleManager;
        private readonly HtmlEncoder _encoder;

        private readonly HashSet<string> _invalidAttributes = new()
        {
            "asp-href-include",
            "asp-href-exclude",
            "asp-fallback-href",
            "asp-fallback-href-exclude",
            "asp-fallback-test-class",
            "asp-fallback-test-property",
            "asp-fallback-test-value",
            "asp-suppress-fallback-integrity",
            "asp-suppress-fallback-integrity",
            "asp-append-version"
        };

        private readonly SmidgeHelper _smidgeHelper;

        public SmidgeLinkTagHelper(SmidgeHelper smidgeHelper, IBundleManager bundleManager, HtmlEncoder encoder)
        {
            _smidgeHelper = smidgeHelper;
            _bundleManager = bundleManager;
            _encoder = encoder;
        }

        [HtmlAttributeName("debug")]
        public bool Debug { get; set; }

        /// <summary>
        /// TODO: Need to figure out why we need this. If the order is default and executes 'after' the
        /// default tag helpers like the script tag helper and url resolution tag helper, the url resolution
        /// doesn't actually work, it simply doesn't get passed through. Not sure if this is a bug or if I'm
        /// doing it wrong. In the meantime, setting this to execute before the defaults works.
        /// </summary>
        public override int Order => -2000;

        [HtmlAttributeName(HrefAttributeName)]
        public string Source { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrWhiteSpace(Source))
            {
                return;
            }

            bool exists = _bundleManager.Exists(Source);

            // Pass through attribute that is also a well-known HTML attribute.
            // this is required to make sure that other tag helpers executing against this element have
            // the value copied across
            output.Attributes.SetAttribute("href", Source);

            if (!exists)
            {
                return;
            }

            if (context.AllAttributes.Any(x => _invalidAttributes.Contains(x.Name)))
            {
                return;
            }

            if (context.AllAttributes.TryGetAttribute("as", out TagHelperAttribute attribute) && attribute.Value is not "style")
            {
                return;
            }

            var attributes = output.Attributes.ToDictionary(x => x.Name, x => x.Value);
            await using (var writer = new StringWriter())
            {
                foreach (var url in await _smidgeHelper.GenerateCssUrlsAsync(Source, Debug))
                {
                    var builder = new TagBuilder(output.TagName) { TagRenderMode = TagRenderMode.SelfClosing };
                    builder.MergeAttributes(attributes);
                    builder.Attributes["href"] = url;

                    builder.WriteTo(writer, _encoder);
                }

                await writer.FlushAsync();
                output.PostElement.SetHtmlContent(new HtmlString(writer.ToString()));
            }

            //This ensures the original tag is not written.
            output.TagName = null;
        }
    }
}
