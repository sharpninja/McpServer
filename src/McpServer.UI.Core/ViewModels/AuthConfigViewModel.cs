using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for auth config discovery.</summary>
[ViewModelCommand("get-auth-config", Description = "Get public auth/OIDC configuration")]
public sealed partial class AuthConfigViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<AuthConfigSnapshot> _command;
    private readonly ILogger<AuthConfigViewModel> _logger;

    /// <summary>Initializes a new auth-config ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public AuthConfigViewModel(Dispatcher dispatcher, ILogger<AuthConfigViewModel> logger)
    {
        _logger = logger;
        _command = new CqrsQueryCommand<AuthConfigSnapshot>(dispatcher, static () => new GetAuthConfigQuery());
    }

    [ObservableProperty]
    private AuthConfigSnapshot? _snapshot;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Gets the underlying CQRS command.</summary>
    public IAsyncRelayCommand GetCommand => _command;

    /// <summary>Primary command alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => GetCommand;

    /// <summary>Gets the last CQRS dispatch result.</summary>
    public Result<AuthConfigSnapshot>? LastResult => _command.LastResult;

    /// <summary>Loads public auth configuration and updates UI-facing state.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading auth configuration...";

        try
        {
            var result = await _command.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                Snapshot = null;
                ErrorMessage = result.Error ?? "Unknown error loading auth configuration.";
                StatusMessage = "Auth config load failed.";
                return;
            }

            Snapshot = result.Value;
            StatusMessage = Snapshot is null
                ? "Auth config not available."
                : "Auth config loaded.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Snapshot = null;
            ErrorMessage = ex.Message;
            StatusMessage = "Auth config load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

