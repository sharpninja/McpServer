using Microsoft.JSInterop;

namespace McpServer.Web.Services;

internal interface ISseSubscriptionService
{
    ValueTask SubscribeAsync<TCallback>(
        string subscriptionId,
        string url,
        DotNetObjectReference<TCallback>? callbackReference = null,
        string callbackMethod = "OnSseMessage",
        CancellationToken cancellationToken = default)
        where TCallback : class;

    ValueTask DisposeAsync(string subscriptionId, CancellationToken cancellationToken = default);

    ValueTask DisposeAllAsync(CancellationToken cancellationToken = default);
}

internal sealed class SseSubscriptionService : ISseSubscriptionService
{
    private readonly IJSRuntime _jsRuntime;

    public SseSubscriptionService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask SubscribeAsync<TCallback>(
        string subscriptionId,
        string url,
        DotNetObjectReference<TCallback>? callbackReference = null,
        string callbackMethod = "OnSseMessage",
        CancellationToken cancellationToken = default)
        where TCallback : class
    {
        return _jsRuntime.InvokeVoidAsync(
            "mcpSse.subscribe",
            cancellationToken,
            subscriptionId,
            url,
            callbackReference,
            callbackMethod);
    }

    public ValueTask DisposeAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _jsRuntime.InvokeVoidAsync("mcpSse.dispose", cancellationToken, subscriptionId);

    public ValueTask DisposeAllAsync(CancellationToken cancellationToken = default)
        => _jsRuntime.InvokeVoidAsync("mcpSse.disposeAll", cancellationToken);
}
