using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for listing functional requirements.
/// </summary>
[ViewModelCommand("requirements-fr-list", Description = "List functional requirements")]
public sealed class FrListViewModel : AreaListViewModelBase<FunctionalRequirementItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<FrListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public FrListViewModel(
        Dispatcher dispatcher,
        ILogger<FrListViewModel> logger)
        : base(McpArea.Requirements)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Loads functional requirements.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading functional requirements...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListFunctionalRequirementsQuery(), ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Functional requirements load failed.";
                StatusMessage = "Functional requirements load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.Items.Count);
            StatusMessage = $"Loaded {Items.Count} functional requirements.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Functional requirements load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
