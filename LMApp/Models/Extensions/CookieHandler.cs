using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace LMApp.Models.Extensions
{
    public class CookieHandler : DelegatingHandler
    {
        public CookieHandler()
        {
        }

        public CookieHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
