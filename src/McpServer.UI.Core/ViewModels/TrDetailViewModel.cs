using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for technical requirement detail and CRUD operations.
/// </summary>
[ViewModelCommand("requirements-tr-detail", Description = "Get/create/update/delete technical requirements")]
public sealed class TrDetailViewModel : AreaDetailViewModelBase<TechnicalRequirementItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<TrDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrDetailViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public TrDetailViewModel(
        Dispatcher dispatcher,
        ILogger<TrDetailViewModel> logger)
        : base(McpArea.Requirements)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Loads a technical requirement by ID.</summary>
    public async Task<TechnicalRequirementItem?> LoadAsync(string id, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading {id}...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetTechnicalRequirementQuery(id), ct);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Load failed.";
                StatusMessage = "Load failed.";
                return null;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null ? $"{id} not found." : $"Loaded {id}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Creates a technical requirement.</summary>
    public Task<TechnicalRequirementItem?> CreateAsync(string id, string? title, string body, CancellationToken ct = default)
        => SaveAsync(
            new CreateTechnicalRequirementCommand(id, title, body),
            $"Creating {id}...",
            id,
            ct);

    /// <summary>Updates a technical requirement.</summary>
    public Task<TechnicalRequirementItem?> UpdateAsync(string id, string? title, string body, CancellationToken ct = default)
        => SaveAsync(
            new UpdateTechnicalRequirementCommand(id, title, body),
            $"Saving {id}...",
            id,
            ct);

    /// <summary>Deletes a technical requirement.</summary>
    public async Task<RequirementsMutationOutcome?> DeleteAsync(string id, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Deleting {id}...";
        try
        {
            var result = await _dispatcher.SendAsync(new DeleteTechnicalRequirementCommand(id), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Delete failed.";
                StatusMessage = "Delete failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Delete failed.";
                StatusMessage = "Delete failed.";
                return result.Value;
            }

            if (Detail is not null && string.Equals(Detail.Id, id, StringComparison.OrdinalIgnoreCase))
                Detail = null;
            StatusMessage = $"Deleted {id}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Delete failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<TechnicalRequirementItem?> SaveAsync<TCommand>(
        TCommand command,
        string pendingStatus,
        string id,
        CancellationToken ct)
        where TCommand : ICommand<TechnicalRequirementItem>
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = pendingStatus;
        try
        {
            var result = await _dispatcher.SendAsync(command, ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Save failed.";
                StatusMessage = "Save failed.";
                return null;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = $"Saved {id}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Save failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
