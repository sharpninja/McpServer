// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Singleton service that tracks connectivity to the MCP backend server.
/// Uses exponential backoff health probes when disconnected and notifies
/// subscribers via <see cref="System.ComponentModel.INotifyPropertyChanged"/>.
/// </summary>
public sealed partial class BackendConnectionMonitor : ObservableObject, IDisposable
{
    private static readonly TimeSpan MinRetryInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRetryInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ConnectedProbeInterval = TimeSpan.FromSeconds(60);

    private readonly IHealthApiClient _healthApiClient;
    private readonly ILogger<BackendConnectionMonitor> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate = new();
    private TimeSpan _currentRetryInterval = MinRetryInterval;
    private Task? _probeLoop;
    private bool _disposed;
    private bool _loggedDisconnect;

    /// <summary>Initializes a new instance of the <see cref="BackendConnectionMonitor"/> class.</summary>
    /// <param name="healthApiClient">Health API client for probing the backend.</param>
    /// <param name="logger">Logger instance.</param>
    public BackendConnectionMonitor(
        IHealthApiClient healthApiClient,
        ILogger<BackendConnectionMonitor> logger)
    {
        _healthApiClient = healthApiClient;
        _logger = logger;
    }

    /// <summary>Whether the backend is currently reachable.</summary>
    [ObservableProperty]
    private bool _isConnected = true; // Optimistic default

    /// <summary>The last connectivity error message, if any.</summary>
    [ObservableProperty]
    private string? _lastError;

    /// <summary>When the last successful probe occurred.</summary>
    [ObservableProperty]
    private DateTimeOffset? _lastConnectedAt;

    /// <summary>
    /// Reports that a backend call succeeded, resetting the connection state.
    /// </summary>
    public void ReportConnected()
    {
        lock (_gate)
        {
            if (!IsConnected)
            {
                _logger.LogInformation("Backend connection restored.");
                _loggedDisconnect = false;
            }

            _currentRetryInterval = MinRetryInterval;
            IsConnected = true;
            LastError = null;
            LastConnectedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Reports that a backend call failed due to a connectivity issue.
    /// Starts the background probe loop if not already running.
    /// </summary>
    /// <param name="error">The error message from the failed call.</param>
    public void ReportDisconnected(string error)
    {
        lock (_gate)
        {
            IsConnected = false;
            LastError = error;
            if (!_loggedDisconnect)
            {
                _logger.LogWarning("Backend connection lost: {Error}. Will retry with backoff.", error);
                _loggedDisconnect = true;
            }

            EnsureProbeLoopRunning();
        }
    }

    /// <summary>Starts the background probe loop. Call once during application startup.</summary>
    public void Start()
    {
        lock (_gate)
        {
            EnsureProbeLoopRunning();
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the given exception indicates a network connectivity issue
    /// (as opposed to an application-level error like 404 or 500).
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns><see langword="true"/> if the exception is a connectivity failure.</returns>
    public static bool IsConnectivityException(Exception? exception)
    {
        return exception is HttpRequestException or SocketException
            || (exception?.InnerException is HttpRequestException or SocketException);
    }

    private void EnsureProbeLoopRunning()
    {
        if (_probeLoop is null || _probeLoop.IsCompleted)
        {
            _probeLoop = Task.Run(() => ProbeLoopAsync(_cts.Token));
        }
    }

    private async Task ProbeLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var delay = IsConnected ? ConnectedProbeInterval : _currentRetryInterval;
            try
            {
                await Task.Delay(delay, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await _healthApiClient.CheckHealthAsync(ct).ConfigureAwait(true);
                ReportConnected();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                lock (_gate)
                {
                    IsConnected = false;
                    LastError = ex.Message;
                    _currentRetryInterval = TimeSpan.FromSeconds(
                        Math.Min(_currentRetryInterval.TotalSeconds * 2, MaxRetryInterval.TotalSeconds));
                    _logger.LogDebug("Backend probe failed (retry in {Interval}s): {Error}",
                        _currentRetryInterval.TotalSeconds, ex.Message);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
