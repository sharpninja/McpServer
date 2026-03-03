using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>ViewModel for the Tunnels tab — lists all providers and dispatches lifecycle commands.</summary>
[ViewModelCommand("list-tunnels", Description = "List tunnel providers and run lifecycle actions")]
public sealed partial class TunnelListViewModel : AreaListViewModelBase<TunnelProviderSnapshot>
{
    private readonly Dispatcher _dispatcher;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _enableCommand;
    private readonly AsyncRelayCommand _disableCommand;
    private readonly AsyncRelayCommand _startCommand;
    private readonly AsyncRelayCommand _stopCommand;
    private readonly AsyncRelayCommand _restartCommand;
    private readonly ILogger<TunnelListViewModel> _logger;


    /// <summary>Initializes a new instance of the <see cref="TunnelListViewModel"/> class.</summary>
    public TunnelListViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<TunnelListViewModel> logger) : base(McpArea.Tunnels)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _refreshCommand = new AsyncRelayCommand(LoadAsync);
        _enableCommand = new AsyncRelayCommand(EnableSelectedAsync);
        _disableCommand = new AsyncRelayCommand(DisableSelectedAsync);
        _startCommand = new AsyncRelayCommand(StartSelectedAsync);
        _stopCommand = new AsyncRelayCommand(StopSelectedAsync);
        _restartCommand = new AsyncRelayCommand(RestartSelectedAsync);
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                _ = Task.Run(() => LoadAsync());
        };
    }

    /// <summary>Loads or refreshes the tunnel provider list.</summary>
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    /// <summary>Enables the currently selected tunnel provider.</summary>
    public IAsyncRelayCommand EnableCommand => _enableCommand;

    /// <summary>Disables the currently selected tunnel provider.</summary>
    public IAsyncRelayCommand DisableCommand => _disableCommand;

    /// <summary>Starts the currently selected tunnel provider.</summary>
    public IAsyncRelayCommand StartCommand => _startCommand;

    /// <summary>Stops the currently selected tunnel provider.</summary>
    public IAsyncRelayCommand StopCommand => _stopCommand;

    /// <summary>Restarts the currently selected tunnel provider.</summary>
    public IAsyncRelayCommand RestartCommand => _restartCommand;

    /// <summary>Primary command alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => RefreshCommand;

    /// <summary>Loads or refreshes the tunnel list.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _dispatcher.QueryAsync(new ListTunnelsQuery(), ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                SetItems(result.Value.Providers, result.Value.Providers.Count);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load tunnels.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Enable a provider and refresh list.</summary>
    public async Task EnableAsync(string providerName, CancellationToken ct = default)
    {
        await _dispatcher.SendAsync(new EnableTunnelCommand(providerName), ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Disable a provider and refresh list.</summary>
    public async Task DisableAsync(string providerName, CancellationToken ct = default)
    {
        await _dispatcher.SendAsync(new DisableTunnelCommand(providerName), ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Start a provider and refresh list.</summary>
    public async Task StartAsync(string providerName, CancellationToken ct = default)
    {
        await _dispatcher.SendAsync(new StartTunnelCommand(providerName), ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Stop a provider and refresh list.</summary>
    public async Task StopAsync(string providerName, CancellationToken ct = default)
    {
        await _dispatcher.SendAsync(new StopTunnelCommand(providerName), ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Restart a provider and refresh list.</summary>
    public async Task RestartAsync(string providerName, CancellationToken ct = default)
    {
        await _dispatcher.SendAsync(new RestartTunnelCommand(providerName), ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
    }

    private async Task EnableSelectedAsync()
    {
        var providerName = RequireSelectedProviderName();
        if (providerName is null)
            return;

        await EnableAsync(providerName).ConfigureAwait(false);
    }

    private async Task DisableSelectedAsync()
    {
        var providerName = RequireSelectedProviderName();
        if (providerName is null)
            return;

        await DisableAsync(providerName).ConfigureAwait(false);
    }

    private async Task StartSelectedAsync()
    {
        var providerName = RequireSelectedProviderName();
        if (providerName is null)
            return;

        await StartAsync(providerName).ConfigureAwait(false);
    }

    private async Task StopSelectedAsync()
    {
        var providerName = RequireSelectedProviderName();
        if (providerName is null)
            return;

        await StopAsync(providerName).ConfigureAwait(false);
    }

    private async Task RestartSelectedAsync()
    {
        var providerName = RequireSelectedProviderName();
        if (providerName is null)
            return;

        await RestartAsync(providerName).ConfigureAwait(false);
    }

    private string? RequireSelectedProviderName()
    {
        var providerName = SelectedItem?.Provider;
        if (!string.IsNullOrWhiteSpace(providerName))
            return providerName;

        ErrorMessage = "Select a tunnel provider first.";
        StatusMessage = "Tunnel action failed.";
        return null;
    }
}
