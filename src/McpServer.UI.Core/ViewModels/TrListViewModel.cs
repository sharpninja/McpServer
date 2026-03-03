using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for listing technical requirements.
/// </summary>
[ViewModelCommand("requirements-tr-list", Description = "List technical requirements")]
public sealed class TrListViewModel : AreaListViewModelBase<TechnicalRequirementItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<TrListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public TrListViewModel(
        Dispatcher dispatcher,
        ILogger<TrListViewModel> logger)
        : base(McpArea.Requirements)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Loads technical requirements.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading technical requirements...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListTechnicalRequirementsQuery(), ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Technical requirements load failed.";
                StatusMessage = "Technical requirements load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.Items.Count);
            StatusMessage = $"Loaded {Items.Count} technical requirements.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Technical requirements load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
