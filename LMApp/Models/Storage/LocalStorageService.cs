using Microsoft.JSInterop;

public class LocalStorageService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private Task<IJSObjectReference> _moduleTask;

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_moduleTask == null)
        {
            _moduleTask = _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/localStorageHelper.js").AsTask();
        }

        return await _moduleTask;
    }

    public async Task Save(string key, string value)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("save", key, value);
    }

    public async Task<string> Load(string key)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<string>("load", key);
    }

    public async Task Remove(string key)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("remove", key);
    }


    public async Task SaveObject<T>(string key, T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await Save(key, json);
    }

    public async Task<T> LoadObject<T>(string key)
    {
        var json = await Load(key);
        return string.IsNullOrEmpty(json) ? default : System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask != null && _moduleTask.IsCompletedSuccessfully)
        {
            var module = await _moduleTask;
            await module.DisposeAsync();
        }
    }
}
