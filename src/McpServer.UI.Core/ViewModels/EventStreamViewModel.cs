using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for the Events tab.
/// Owns the active SSE subscription state and bounded event history.
/// </summary>
[ViewModelCommand("events-stream", Description = "Subscribe to workspace change events")]
public sealed partial class EventStreamViewModel : AreaListViewModelBase<ChangeEventItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<EventStreamViewModel> _logger;
    private readonly object _streamGate = new();
    private CancellationTokenSource? _streamCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context.</param>
    /// <param name="logger">Logger instance.</param>
    public EventStreamViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<EventStreamViewModel> logger)
        : base(McpArea.Events)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        StatusMessage = "Not connected.";

        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                return;

            StopStreaming();
            ClearEvents();
            StatusMessage = string.IsNullOrWhiteSpace(workspaceContext.ActiveWorkspacePath)
                ? "Workspace changed. Select an active workspace."
                : $"Workspace changed to '{workspaceContext.ActiveWorkspacePath}'.";
        };
    }

    /// <summary>Optional category filter for the SSE subscription.</summary>
    [ObservableProperty]
    private string? _categoryFilter;

    /// <summary>Maximum number of events retained in memory.</summary>
    [ObservableProperty]
    private int _maxRetainedEvents = 500;

    /// <summary>Whether a stream subscription is currently active.</summary>
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// Starts a new SSE subscription and appends incoming events to the retained item list.
    /// Any existing subscription is cancelled first.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StartAsync(CancellationToken ct = default)
    {
        StopStreaming();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        lock (_streamGate)
            _streamCts = linkedCts;

        IsStreaming = true;
        ErrorMessage = null;
        StatusMessage = "Subscribing to events...";

        try
        {
            var category = string.IsNullOrWhiteSpace(CategoryFilter) ? null : CategoryFilter.Trim();
            var result = await _dispatcher.QueryAsync(new SubscribeToEventsQuery(category), linkedCts.Token).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to subscribe to events.";
                StatusMessage = "Subscription failed.";
                return;
            }

            StatusMessage = "Streaming events...";
            await foreach (var item in result.Value.WithCancellation(linkedCts.Token))
            {
                AppendEvent(item);
            }

            StatusMessage = "Event stream completed.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Streaming stopped.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Streaming failed.";
        }
        finally
        {
            lock (_streamGate)
            {
                if (ReferenceEquals(_streamCts, linkedCts))
                    _streamCts = null;
            }

            linkedCts.Dispose();
            IsStreaming = false;
        }
    }

    /// <summary>
    /// Stops the active stream subscription, if one exists.
    /// </summary>
    public void StopStreaming()
    {
        lock (_streamGate)
        {
            _streamCts?.Cancel();
        }
    }

    /// <summary>
    /// Clears retained event history.
    /// </summary>
    public void ClearEvents()
    {
        ClearItems();
    }

    private void AppendEvent(ChangeEventItem item)
    {
        Items.Add(item);

        var max = MaxRetainedEvents <= 0 ? 500 : MaxRetainedEvents;
        while (Items.Count > max)
            Items.RemoveAt(0);

        TotalCount = Items.Count;
        LastRefreshedAt = DateTimeOffset.UtcNow;
    }
}
