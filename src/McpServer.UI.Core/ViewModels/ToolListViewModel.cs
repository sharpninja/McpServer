using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for Tool Registry list and search operations.
/// </summary>
[ViewModelCommand("tools-list", Description = "List/search tool definitions")]
public sealed partial class ToolListViewModel : AreaListViewModelBase<ToolListItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<ToolListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context.</param>
    /// <param name="logger">Logger instance.</param>
    public ToolListViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<ToolListViewModel> logger)
        : base(McpArea.ToolRegistry)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <summary>Optional keyword used for server-side tool search.</summary>
    [ObservableProperty]
    private string? _searchKeyword;

    /// <summary>
    /// Loads tools using list or search semantics based on <paramref name="keyword"/>.
    /// </summary>
    /// <param name="keyword">Optional keyword override.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(string? keyword = null, CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        var effectiveKeyword = keyword ?? SearchKeyword;
        SearchKeyword = effectiveKeyword;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = string.IsNullOrWhiteSpace(effectiveKeyword)
            ? "Loading tools..."
            : $"Searching tools for '{effectiveKeyword}'...";
        try
        {
            Result<ListToolsResult> result;
            if (string.IsNullOrWhiteSpace(effectiveKeyword))
            {
                result = await _dispatcher.QueryAsync(new ListToolsQuery(_workspaceContext.ActiveWorkspacePath), ct)
                    .ConfigureAwait(true);
            }
            else
            {
                result = await _dispatcher.QueryAsync(
                        new SearchToolsQuery(effectiveKeyword.Trim(), _workspaceContext.ActiveWorkspacePath), ct)
                    .ConfigureAwait(true);
            }

            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load tools.";
                StatusMessage = "Tool load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} tools.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Tool load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
