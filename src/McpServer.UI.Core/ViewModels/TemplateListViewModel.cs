// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for the Templates tab list/grid.
/// Queries the template endpoint and exposes items as a list.
/// </summary>
[ViewModelCommand("list-templates", Description = "List prompt templates")]
public sealed partial class TemplateListViewModel : AreaListViewModelBase<TemplateListItem>
{
    private readonly CqrsQueryCommand<ListTemplatesResult> _refreshCommand;
    private readonly ILogger<TemplateListViewModel> _logger;

    /// <summary>Initializes a new instance of the template list ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context for reacting to workspace changes.</param>
    /// <param name="logger">Logger instance.</param>
    public TemplateListViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<TemplateListViewModel> logger)
        : base(McpArea.Templates)
    {
        _logger = logger;
        _refreshCommand = new CqrsQueryCommand<ListTemplatesResult>(dispatcher, BuildQuery);
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                _ = Task.Run(() => LoadAsync());
        };
    }

    /// <summary>Filter by category.</summary>
    [ObservableProperty]
    private string? _category;

    /// <summary>Filter by tag.</summary>
    [ObservableProperty]
    private string? _tag;

    /// <summary>Keyword search filter.</summary>
    [ObservableProperty]
    private string? _keyword;

    /// <summary>Refresh command (also the primary command for exec).</summary>
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    /// <summary>Primary command alias for registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => RefreshCommand;

    /// <summary>Last query result.</summary>
    public Result<ListTemplatesResult>? LastResult => _refreshCommand.LastResult;

    /// <summary>Loads templates into the list.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading templates...";

        try
        {
            var result = await _refreshCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error loading templates.";
                StatusMessage = "Template load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} templates.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Template load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ListTemplatesQuery BuildQuery() => new()
    {
        Category = Category,
        Tag = Tag,
        Keyword = Keyword,
    };
}
