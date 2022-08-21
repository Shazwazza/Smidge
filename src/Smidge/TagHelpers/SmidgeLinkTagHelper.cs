using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Collections.Generic;
using System;

namespace Smidge.TagHelpers
{
    [HtmlTargetElement("link", Attributes = HrefAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class SmidgeLinkTagHelper : TagHelper
    {
        private const string HrefIncludeAttributeName = "asp-href-include";
        private const string HrefExcludeAttributeName = "asp-href-exclude";
        private const string FallbackHrefAttributeName = "asp-fallback-href";
        private const string SuppressFallbackIntegrityAttributeName = "asp-suppress-fallback-integrity";
        private const string FallbackHrefIncludeAttributeName = "asp-fallback-href-include";
        private const string FallbackHrefExcludeAttributeName = "asp-fallback-href-exclude";
        private const string FallbackTestClassAttributeName = "asp-fallback-test-class";
        private const string FallbackTestPropertyAttributeName = "asp-fallback-test-property";
        private const string FallbackTestValueAttributeName = "asp-fallback-test-value";
        private const string AppendVersionAttributeName = "asp-append-version";
        private const string HrefAttributeName = "href";

        private readonly HashSet<string> _invalid = new()
        {
            HrefIncludeAttributeName,
            HrefExcludeAttributeName,
            FallbackHrefAttributeName,
            FallbackHrefIncludeAttributeName,
            FallbackTestClassAttributeName,
            FallbackTestPropertyAttributeName,
            SuppressFallbackIntegrityAttributeName,
            FallbackHrefExcludeAttributeName,
            FallbackTestValueAttributeName,
            AppendVersionAttributeName
        };
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

        [HtmlAttributeName(HrefAttributeName)]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate content based on the debug or production configuration profile.
        /// If left unset then the configured <see cref="ISmidgeProfileStrategy"/> will determine if the debug profile is used.
        /// </summary>
        [HtmlAttributeName("debug")]
        public bool? Debug { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrWhiteSpace(Source))
            {
                return;
            }

            var exists = _bundleManager.Exists(Source);

            // Pass through attribute that is also a well-known HTML attribute.
            // this is required to make sure that other tag helpers executing against this element have
            // the value copied across
            output.Attributes.SetAttribute("href", Source);

            if (!exists)
            {
                return;
            }

            if (context.AllAttributes.Any(x => _invalid.Contains(x.Name)))
            {
                throw new InvalidOperationException("Smidge tag helpers do not support the ASP.NET tag helpers: " + string.Join(", ", _invalid));
            }

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
