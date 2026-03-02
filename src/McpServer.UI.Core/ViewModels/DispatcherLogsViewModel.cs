using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for viewing recent local CQRS dispatcher logs captured in memory by <see cref="Dispatcher"/>.
/// This ViewModel is intended for interactive UI use only (process-local data; not exposed via director exec).
/// </summary>
public sealed partial class DispatcherLogsViewModel : AreaListViewModelBase<DispatchLogRecord>
{
    private readonly Dispatcher _dispatcher;
    private readonly AsyncRelayCommand _refreshCommand;

    [ObservableProperty]
    private int _limit = 100;

    [ObservableProperty]
    private int _activeDispatchCount;

    [ObservableProperty]
    private IReadOnlyList<DispatchLogRecord> _result = [];
    private readonly ILogger<DispatcherLogsViewModel> _logger;


    /// <summary>Initializes a new instance.</summary>
    /// <param name="dispatcher">The local CQRS dispatcher retaining dispatch log history.</param>
    /// <param name="logger">Logger instance.</param>
    public DispatcherLogsViewModel(Dispatcher dispatcher,
        ILogger<DispatcherLogsViewModel> logger)
        : base(McpArea.DispatcherLogs)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _refreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>Refreshes the local dispatcher log history snapshot.</summary>
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    /// <summary>Loads a snapshot of recent completed dispatch logs and active dispatch count.</summary>
    /// <param name="ct">Cancellation token.</param>
    public Task LoadAsync(CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled(ct);

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading dispatcher logs...";

        try
        {
            ActiveDispatchCount = _dispatcher.ActiveContexts.Count;

            var records = _dispatcher.RecentDispatches
                .OrderByDescending(static r => r.FinishedAt)
                .Take(Math.Max(1, Limit))
                .ToArray();

            SetItems(records, records.Length);
            Result = records;

            if (SelectedItem is null || !Items.Contains(SelectedItem))
                SelectedIndex = Items.Count > 0 ? 0 : -1;

            StatusMessage = $"Loaded {records.Length} dispatch log(s); active: {ActiveDispatchCount}.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Failed to load dispatcher logs.";
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }
}
