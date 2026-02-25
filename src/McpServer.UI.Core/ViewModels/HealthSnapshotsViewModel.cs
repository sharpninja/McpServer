using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for Health tab snapshot history and detail display.
/// Maintains a client-side history list of health checks and exposes an async command for checks.
/// </summary>
[ViewModelCommand("check-health", Description = "Check MCP server health and append a snapshot")]
public sealed class HealthSnapshotsViewModel : AreaListViewModelBase<HealthSnapshot>
{
    private readonly CqrsQueryCommand<HealthSnapshot> _checkHealthCommand;

    /// <summary>Initializes a new instance of the health snapshots ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    public HealthSnapshotsViewModel(Dispatcher dispatcher)
        : base(McpArea.Health)
    {
        _checkHealthCommand = new CqrsQueryCommand<HealthSnapshot>(dispatcher, static () => new CheckHealthQuery());
    }

    /// <summary>
    /// Primary async command used by the UI and <c>director exec</c>.
    /// </summary>
    public IAsyncRelayCommand CheckHealthCommand => _checkHealthCommand;

    /// <summary>Alias for <see cref="CheckHealthCommand"/> for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => CheckHealthCommand;

    /// <summary>Last CQRS dispatch result.</summary>
    public Result<HealthSnapshot>? LastResult => _checkHealthCommand.LastResult;

    /// <summary>
    /// Executes the health check and appends the returned snapshot to the local history list.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task CheckAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Checking server health...";

        try
        {
            var result = await _checkHealthCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error checking health.";
                StatusMessage = "Health check failed.";
                return;
            }

            Items.Insert(0, result.Value);
            SelectedItem = result.Value;
            TotalCount = Items.Count;
            LastRefreshedAt = DateTimeOffset.UtcNow;
            StatusMessage = $"Health check recorded at {result.Value.CheckedAt:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Health check failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
