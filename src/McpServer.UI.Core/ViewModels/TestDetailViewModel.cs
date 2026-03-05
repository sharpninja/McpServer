using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for testing requirement detail and CRUD operations.
/// </summary>
[ViewModelCommand("requirements-test-detail", Description = "Get/create/update/delete testing requirements")]
public sealed class TestDetailViewModel : AreaDetailViewModelBase<TestingRequirementItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<TestDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDetailViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public TestDetailViewModel(
        Dispatcher dispatcher,
        ILogger<TestDetailViewModel> logger)
        : base(McpArea.Requirements)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Loads a testing requirement by ID.</summary>
    public async Task<TestingRequirementItem?> LoadAsync(string id, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading {id}...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetTestingRequirementQuery(id), ct).ConfigureAwait(false);
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

    /// <summary>Creates a testing requirement.</summary>
    public Task<TestingRequirementItem?> CreateAsync(string id, string condition, CancellationToken ct = default)
        => SaveAsync(
            new CreateTestingRequirementCommand(id, condition),
            $"Creating {id}...",
            id,
            ct);

    /// <summary>Updates a testing requirement.</summary>
    public Task<TestingRequirementItem?> UpdateAsync(string id, string condition, CancellationToken ct = default)
        => SaveAsync(
            new UpdateTestingRequirementCommand(id, condition),
            $"Saving {id}...",
            id,
            ct);

    /// <summary>Deletes a testing requirement.</summary>
    public async Task<RequirementsMutationOutcome?> DeleteAsync(string id, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Deleting {id}...";
        try
        {
            var result = await _dispatcher.SendAsync(new DeleteTestingRequirementCommand(id), ct).ConfigureAwait(false);
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

    private async Task<TestingRequirementItem?> SaveAsync<TCommand>(
        TCommand command,
        string pendingStatus,
        string id,
        CancellationToken ct)
        where TCommand : ICommand<TestingRequirementItem>
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = pendingStatus;
        try
        {
            var result = await _dispatcher.SendAsync(command, ct).ConfigureAwait(false);
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
