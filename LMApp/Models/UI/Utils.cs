using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace LMApp.Models.UI
{
    public class Utils(IJSRuntime js) : IAsyncDisposable
    {
        readonly IJSRuntime JS = js;

        private IJSObjectReference module;


        public async ValueTask Init()
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import",
                $"./js/utils.js");
        }

        public async ValueTask<ScrollInfo> GetScrollInfo(ElementReference element)
        {
            await EnsureInit();
            return await module.InvokeAsync<ScrollInfo>("getScrollInfo", element);
        }

        public async ValueTask FixEnterIssue(ElementReference elm)
        {
            await EnsureInit();
            await module.InvokeVoidAsync("fixEnterIssue", elm);
        }

        public async ValueTask Share(string url, string title, string text)
        {
            await EnsureInit();
            await module.InvokeVoidAsync("share", url, title, text);
        }


        public async ValueTask FixBootstrapEnterHandling(ElementReference elm)
        {
            await EnsureInit();
            await module.InvokeVoidAsync("fixBootstrapEnterHandling", elm);
        }

        public async ValueTask FixBootstrapEnterHandlingById(string id)
        {
            await EnsureInit();
            await module.InvokeVoidAsync("fixBootstrapEnterHandlingById", id);
        }

        public async ValueTask FocusById(string id)
        {
            await EnsureInit();
            await module.InvokeVoidAsync("focusById", id);
        }

        public async ValueTask HistoryBack()
        {
            await EnsureInit();
            await module.InvokeVoidAsync("historyBack");
        }

        public async ValueTask OpenNewTab(string url)
        {
            await JS.InvokeVoidAsync("open", url, "_blank");
        }

        public async ValueTask AddDebouncedEventCallback<T>(string eventName, string callbackMethodName, ElementReference elem, DotNetObjectReference<T> component, int interval)
            where T : class
        {
            await module.InvokeVoidAsync("onDebouncedEvent", eventName, callbackMethodName, elem, component, interval);
        }

        public async ValueTask AddThrottledEventCallback<T>(string eventName, string callbackMethodName, ElementReference elem, DotNetObjectReference<T> component, int interval)
           where T : class
        {
            await module.InvokeVoidAsync("onThrottledEvent", eventName, callbackMethodName, elem, component, interval);
        }

        public async ValueTask AttachScrollEndEventWithPolyfill<T>(string callbackMethodName, ElementReference elem, DotNetObjectReference<T> component, int interval)
           where T : class
        {   
            await module.InvokeVoidAsync("attachScrollEndEvent", callbackMethodName, elem, component, interval);
        }

        public async ValueTask DownloadFile(string filename, string content, string mimeType)
        {
            await EnsureInit();
            await module.InvokeVoidAsync("downloadFile", filename, content, mimeType);
        }

        public async ValueTask RegisterTransactionFormHotkeys<T>(DotNetObjectReference<T> component)
            where T : class
        {
            await EnsureInit();
            await module.InvokeVoidAsync("registerTransactionFormHotkeys", component);
        }

        public async ValueTask UnregisterTransactionFormHotkeys()
        {
            await EnsureInit();
            await module.InvokeVoidAsync("unregisterTransactionFormHotkeys");
        }

        public async ValueTask ScrollToSelectedItem(ElementReference containerElement, string selectedClass)
        {
            await EnsureInit();
            await module.InvokeVoidAsync("scrollToSelectedItem", containerElement, selectedClass);
        }

        private async ValueTask EnsureInit()
        {
            if (module is null)
            {
                await Init();
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (module is not null)
            {
                await module.DisposeAsync();
            }
        }
    }
}
