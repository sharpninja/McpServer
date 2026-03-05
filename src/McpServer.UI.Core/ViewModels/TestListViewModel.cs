using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for listing testing requirements.
/// </summary>
[ViewModelCommand("requirements-test-list", Description = "List testing requirements")]
public sealed class TestListViewModel : AreaListViewModelBase<TestingRequirementItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<TestListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public TestListViewModel(
        Dispatcher dispatcher,
        ILogger<TestListViewModel> logger)
        : base(McpArea.Requirements)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Loads testing requirements.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading testing requirements...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListTestingRequirementsQuery(), ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Testing requirements load failed.";
                StatusMessage = "Testing requirements load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.Items.Count);
            StatusMessage = $"Loaded {Items.Count} testing requirements.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Testing requirements load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
