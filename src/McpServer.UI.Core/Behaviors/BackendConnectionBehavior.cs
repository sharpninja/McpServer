// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Behaviors;

/// <summary>
/// Pipeline behavior that short-circuits API calls when the backend is unreachable,
/// and updates <see cref="BackendConnectionMonitor"/> on connectivity changes.
/// <see cref="CheckHealthQuery"/> is always allowed through so the monitor can detect recovery.
/// </summary>
internal sealed class BackendConnectionBehavior : IPipelineBehavior
{
    private readonly BackendConnectionMonitor _monitor;

    /// <summary>Initializes a new instance of the <see cref="BackendConnectionBehavior"/> class.</summary>
    /// <param name="monitor">Backend connection monitor singleton.</param>
    public BackendConnectionBehavior(BackendConnectionMonitor monitor)
    {
        _monitor = monitor;
    }

    /// <inheritdoc />
    public async Task<Result<T>> HandleAsync<T>(object request, CallContext context, Func<Task<Result<T>>> next)
    {
        var isHealthCheck = request is CheckHealthQuery;

        // Short-circuit non-health requests when disconnected
        if (!_monitor.IsConnected && !isHealthCheck)
        {
            return Result<T>.Failure($"Backend unavailable: {_monitor.LastError ?? "connection lost"}");
        }

        var result = await next().ConfigureAwait(true);

        // Detect connectivity failures from result exceptions
        if (result.IsFailure && BackendConnectionMonitor.IsConnectivityException(result.Exception))
        {
            _monitor.ReportDisconnected(result.Exception!.Message);
        }
        else if (result.IsSuccess)
        {
            _monitor.ReportConnected();
        }

        return result;
    }
}
