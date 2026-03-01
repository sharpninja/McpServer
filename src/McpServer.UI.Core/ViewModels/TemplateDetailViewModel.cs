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
/// ViewModel for viewing, editing, and testing a single prompt template.
/// </summary>
public sealed partial class TemplateDetailViewModel : AreaDetailViewModelBase<TemplateDetail>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<TemplateDetailViewModel> _logger;

    /// <summary>Whether the current editor state represents a new unsaved draft.</summary>
    [ObservableProperty] private bool _isNewDraft;

    /// <summary>Editor field for template ID.</summary>
    [ObservableProperty] private string _editorId = "";

    /// <summary>Editor field for template title.</summary>
    [ObservableProperty] private string _editorTitle = "";

    /// <summary>Editor field for template category.</summary>
    [ObservableProperty] private string _editorCategory = "";

    /// <summary>Editor field for template content.</summary>
    [ObservableProperty] private string _editorContent = "";

    /// <summary>Editor field for comma-separated tags.</summary>
    [ObservableProperty] private string _editorTags = "";

    /// <summary>Editor field for template description.</summary>
    [ObservableProperty] private string _editorDescription = "";

    /// <summary>Editor field for template engine.</summary>
    [ObservableProperty] private string _editorEngine = "handlebars";

    /// <summary>Initializes a new instance of the template detail ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public TemplateDetailViewModel(Dispatcher dispatcher,
        ILogger<TemplateDetailViewModel> logger)
        : base(McpArea.Templates)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Loads a template by ID.</summary>
    /// <param name="templateId">Template identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(string templateId, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Loading template...";

        try
        {
            var result = await _dispatcher
                .QueryAsync(new GetTemplateQuery(templateId), ct)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Unknown error loading template.";
                StatusMessage = "Template load failed.";
                return;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is not null ? $"Loaded '{result.Value.Id}'." : "Template not found.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Template load failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Deletes the currently loaded template.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deletion succeeded.</returns>
    public async Task<bool> DeleteAsync(CancellationToken ct = default)
    {
        if (Detail is null)
            return false;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _dispatcher
                .SendAsync(new DeleteTemplateCommand(Detail.Id), ct)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null || !result.Value.Success)
            {
                ErrorMessage = result.Error ?? result.Value?.Error ?? "Delete failed.";
                return false;
            }

            StatusMessage = $"Deleted '{Detail.Id}'.";
            Detail = null;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Tests the currently loaded template with sample variables.</summary>
    /// <param name="variablesJson">JSON-encoded variable values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Rendered content on success, null on failure.</returns>
    public async Task<string?> TestAsync(string variablesJson, CancellationToken ct = default)
    {
        if (Detail is null)
            return null;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var query = new TestTemplateQuery
            {
                TemplateId = Detail.Id,
                VariablesJson = variablesJson,
            };

            var result = await _dispatcher
                .QueryAsync(query, ct)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null || !result.Value.Success)
            {
                ErrorMessage = result.Error ?? result.Value?.Error ?? "Test failed.";
                return null;
            }

            StatusMessage = "Template rendered successfully.";
            return result.Value.RenderedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Creates or updates a template depending on draft state.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the save succeeded.</returns>
    public async Task<bool> SaveAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Saving template...";

        try
        {
            var tags = string.IsNullOrWhiteSpace(EditorTags)
                ? null
                : (IReadOnlyList<string>)EditorTags
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            if (IsNewDraft)
            {
                var cmd = new CreateTemplateCommand
                {
                    Id = EditorId,
                    Title = EditorTitle,
                    Category = EditorCategory,
                    Content = EditorContent,
                    Tags = tags,
                    Description = string.IsNullOrWhiteSpace(EditorDescription) ? null : EditorDescription,
                    Engine = string.IsNullOrWhiteSpace(EditorEngine) ? null : EditorEngine,
                };

                var result = await _dispatcher.SendAsync(cmd, ct).ConfigureAwait(false);

                if (!result.IsSuccess || result.Value is null || !result.Value.Success)
                {
                    ErrorMessage = result.Error ?? result.Value?.Error ?? "Create failed.";
                    StatusMessage = "Template create failed.";
                    return false;
                }

                Detail = result.Value.Item;
                IsNewDraft = false;
                StatusMessage = $"Created '{EditorId}'.";
                return true;
            }
            else
            {
                var cmd = new UpdateTemplateCommand
                {
                    TemplateId = Detail?.Id ?? EditorId,
                    Title = EditorTitle,
                    Category = EditorCategory,
                    Content = EditorContent,
                    Tags = tags,
                    Description = string.IsNullOrWhiteSpace(EditorDescription) ? null : EditorDescription,
                    Engine = string.IsNullOrWhiteSpace(EditorEngine) ? null : EditorEngine,
                };

                var result = await _dispatcher.SendAsync(cmd, ct).ConfigureAwait(false);

                if (!result.IsSuccess || result.Value is null || !result.Value.Success)
                {
                    ErrorMessage = result.Error ?? result.Value?.Error ?? "Update failed.";
                    StatusMessage = "Template update failed.";
                    return false;
                }

                Detail = result.Value.Item;
                StatusMessage = $"Saved '{cmd.TemplateId}'.";
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Template save failed.";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Clears the editor and begins a new template draft.</summary>
    public void BeginNewDraft()
    {
        Detail = null;
        IsNewDraft = true;
        EditorId = "";
        EditorTitle = "";
        EditorCategory = "";
        EditorContent = "";
        EditorTags = "";
        EditorDescription = "";
        EditorEngine = "handlebars";
        StatusMessage = "New template draft.";
        ErrorMessage = null;
    }

    /// <summary>Populates editor fields from the currently loaded detail.</summary>
    public void PopulateEditorFromDetail()
    {
        if (Detail is null) return;
        IsNewDraft = false;
        EditorId = Detail.Id;
        EditorTitle = Detail.Title;
        EditorCategory = Detail.Category;
        EditorContent = Detail.Content;
        EditorTags = string.Join(", ", Detail.Tags);
        EditorDescription = Detail.Description ?? "";
        EditorEngine = Detail.Engine;
    }
}
