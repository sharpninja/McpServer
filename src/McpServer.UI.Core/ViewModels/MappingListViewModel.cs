using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for FR-to-TR requirement mapping rows.
/// </summary>
[ViewModelCommand("requirements-mapping-list", Description = "List and mutate FR-to-TR mappings")]
public sealed class MappingListViewModel : AreaListViewModelBase<RequirementMappingItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<MappingListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public MappingListViewModel(
        Dispatcher dispatcher,
        ILogger<MappingListViewModel> logger)
        : base(McpArea.Requirements)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Loads mapping rows.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading mappings...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListRequirementMappingsQuery(), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Mapping load failed.";
                StatusMessage = "Mapping load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.Items.Count);
            StatusMessage = $"Loaded {Items.Count} mappings.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Mapping load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates or updates an FR-to-TR mapping row.
    /// </summary>
    /// <param name="frId">Functional requirement identifier.</param>
    /// <param name="trIds">Mapped technical requirement IDs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated mapping row on success, otherwise null.</returns>
    public async Task<RequirementMappingItem?> UpsertAsync(string frId, IReadOnlyList<string> trIds, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = $"Saving mapping for {frId}...";
        try
        {
            var command = new UpsertRequirementMappingCommand(frId, trIds);
            var result = await _dispatcher.SendAsync(command, ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Mapping save failed.";
                StatusMessage = "Mapping save failed.";
                return null;
            }

            await LoadAsync(ct);
            StatusMessage = $"Saved mapping for {frId}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Mapping save failed.";
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Deletes a mapping row by FR ID.
    /// </summary>
    /// <param name="frId">Functional requirement identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation outcome on success, otherwise null.</returns>
    public async Task<RequirementsMutationOutcome?> DeleteAsync(string frId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = $"Deleting mapping for {frId}...";
        try
        {
            var result = await _dispatcher.SendAsync(new DeleteRequirementMappingCommand(frId), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Mapping delete failed.";
                StatusMessage = "Mapping delete failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Mapping delete failed.";
                StatusMessage = "Mapping delete failed.";
                return result.Value;
            }

            await LoadAsync(ct);
            StatusMessage = $"Deleted mapping for {frId}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Mapping delete failed.";
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
