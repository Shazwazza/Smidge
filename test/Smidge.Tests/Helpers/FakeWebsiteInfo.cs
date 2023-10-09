using System;

namespace Smidge.Tests.Helpers
{
    public class FakeWebsiteInfo : IWebsiteInfo
    {

        private Uri _baseUrl;
        public Uri GetBaseUrl() => _baseUrl ??= new Uri("http://test.com");

        public string GetBasePath() => string.Empty;
    }
}
