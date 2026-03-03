using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>ViewModel for the Tunnels tab — lists all providers and dispatches lifecycle commands.</summary>
public sealed partial class TunnelListViewModel : AreaListViewModelBase<TunnelProviderSnapshot>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<TunnelListViewModel> _logger;


    /// <summary>Initializes a new instance of the <see cref="TunnelListViewModel"/> class.</summary>
    public TunnelListViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<TunnelListViewModel> logger) : base(McpArea.Tunnels)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                _ = Task.Run(() => LoadAsync());
        };
    }

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
}
