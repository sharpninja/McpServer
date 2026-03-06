using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for Tool Registry tool-detail and mutation operations.
/// </summary>
[ViewModelCommand("tool-detail", Description = "Get/create/update/delete a tool")]
public sealed class ToolDetailViewModel : AreaDetailViewModelBase<ToolDetail>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<ToolDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolDetailViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public ToolDetailViewModel(
        Dispatcher dispatcher,
        ILogger<ToolDetailViewModel> logger)
        : base(McpArea.ToolRegistry)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Loads a tool by ID.
    /// </summary>
    /// <param name="toolId">Tool identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Loaded detail, if found.</returns>
    public async Task<ToolDetail?> LoadAsync(int toolId, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading tool #{toolId}...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetToolQuery(toolId), ct).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to load tool detail.";
                StatusMessage = "Tool detail load failed.";
                return null;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null ? $"Tool #{toolId} not found." : $"Loaded tool #{toolId}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Tool detail load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Creates a tool.
    /// </summary>
    /// <param name="command">Create payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation outcome on success, otherwise null.</returns>
    public Task<ToolMutationOutcome?> CreateAsync(CreateToolCommand command, CancellationToken ct = default)
        => SendMutationAsync(command, $"Creating tool '{command.Name}'...", ct);

    /// <summary>
    /// Updates a tool.
    /// </summary>
    /// <param name="command">Update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation outcome on success, otherwise null.</returns>
    public Task<ToolMutationOutcome?> UpdateAsync(UpdateToolCommand command, CancellationToken ct = default)
        => SendMutationAsync(command, $"Saving tool #{command.ToolId}...", ct);

    /// <summary>
    /// Deletes a tool by ID.
    /// </summary>
    /// <param name="toolId">Tool identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation outcome on success, otherwise null.</returns>
    public Task<ToolMutationOutcome?> DeleteAsync(int toolId, CancellationToken ct = default)
        => SendMutationAsync(new DeleteToolCommand(toolId), $"Deleting tool #{toolId}...", ct);

    private async Task<ToolMutationOutcome?> SendMutationAsync<TCommand>(TCommand command, string pendingStatus, CancellationToken ct)
        where TCommand : ICommand<ToolMutationOutcome>
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = pendingStatus;
        try
        {
            var result = await _dispatcher.SendAsync(command, ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Tool mutation failed.";
                StatusMessage = "Tool mutation failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Tool mutation failed.";
                StatusMessage = "Tool mutation failed.";
                return result.Value;
            }

            if (result.Value.Tool is not null)
                Detail = result.Value.Tool;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = "Tool operation completed.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Tool mutation failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
