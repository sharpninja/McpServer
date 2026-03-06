using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for global agent definition list operations.
/// </summary>
[ViewModelCommand("agent-definitions-list", Description = "List global agent definitions")]
public sealed class AgentDefinitionListViewModel : AreaListViewModelBase<AgentDefinitionSummaryItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<AgentDefinitionListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDefinitionListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public AgentDefinitionListViewModel(
        Dispatcher dispatcher,
        ILogger<AgentDefinitionListViewModel> logger)
        : base(McpArea.Agents)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Loads global agent definitions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading agent definitions...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListAgentDefinitionsQuery(), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load agent definitions.";
                StatusMessage = "Agent definition load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} definitions.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Agent definition load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
