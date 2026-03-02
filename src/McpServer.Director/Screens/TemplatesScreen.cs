// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for prompt template management.
/// Displays a filterable grid of templates with detail preview and test capabilities.
/// </summary>
internal sealed class TemplatesScreen : View
{
    private readonly TemplateListViewModel _listVm;
    private readonly TemplateDetailViewModel _detailVm;
    private readonly ViewModelBinder _binder = new();
    private TableView _tableView = null!;
    private Label _statusLabel = null!;
    private TextView _detailView = null!;
    private Button _refreshBtn = null!;
    private TextField _categoryFilter = null!;
    private TextField _keywordFilter = null!;

    /// <summary>Initializes a new instance of the <see cref="TemplatesScreen"/> class.</summary>
    public TemplatesScreen(TemplateListViewModel listVm, TemplateDetailViewModel detailVm)
    {
        _listVm = listVm;
        _detailVm = detailVm;
        Title = "Templates";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        // Filter bar
        var catLabel = new Label { X = 0, Y = 0, Text = "Category:" };
        _categoryFilter = new TextField
        {
            X = Pos.Right(catLabel) + 1,
            Y = 0,
            Width = 15,
            Text = "",
        };

        var kwLabel = new Label { X = Pos.Right(_categoryFilter) + 2, Y = 0, Text = "Search:" };
        _keywordFilter = new TextField
        {
            X = Pos.Right(kwLabel) + 1,
            Y = 0,
            Width = 20,
            Text = "",
        };

        var filterBtn = new Button { X = Pos.Right(_keywordFilter) + 1, Y = 0, Text = "Filter" };
        filterBtn.Accepting += (_, _) =>
        {
            _listVm.Category = string.IsNullOrWhiteSpace(_categoryFilter.Text?.ToString()) ? null : _categoryFilter.Text.ToString();
            _listVm.Keyword = string.IsNullOrWhiteSpace(_keywordFilter.Text?.ToString()) ? null : _keywordFilter.Text.ToString();
            _ = Task.Run(LoadAsync);
        };

        Add(catLabel, _categoryFilter, kwLabel, _keywordFilter, filterBtn);

        _statusLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = "Prompt Templates",
        };
        Add(_statusLabel);

        var errorField = new TextField
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Text = "",
            ReadOnly = true,
            Visible = false,
        };
        var errorColorScheme = Colors.ColorSchemes.TryGetValue("Error", out var errScheme) ? errScheme : null;
        if (errorColorScheme is not null)
            errorField.ColorScheme = errorColorScheme;
        Add(errorField);

        _tableView = new TableView
        {
            X = 0,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _tableView.SelectedCellChanged += (_, e) =>
        {
            _listVm.SelectedIndex = e.NewRow;
        };
        Add(_tableView);

        // Detail preview (lower half)
        var detailLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(_tableView),
            Text = "Content Preview:",
        };
        Add(detailLabel);

        _detailView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(detailLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ReadOnly = false,
        };
        Add(_detailView);

        // Button bar
        _refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        _refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAsync);

        var createBtn = new Button { X = Pos.Right(_refreshBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Create" };
        createBtn.Accepting += (_, _) => ShowTemplateDialog(isNew: true);

        var editBtn = new Button { X = Pos.Right(createBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Edit" };
        editBtn.Accepting += (_, _) => ShowTemplateDialog(isNew: false);

        var deleteBtn = new Button { X = Pos.Right(editBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedAsync);

        var testBtn = new Button { X = Pos.Right(deleteBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Test" };
        testBtn.Accepting += (_, _) => ShowTestDialog();

        var countLabel = new Label
        {
            X = Pos.Right(testBtn) + 2,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "",
        };

        Add(_refreshBtn, createBtn, editBtn, deleteBtn, testBtn, countLabel);

        // Bindings
        _binder.BindProperty(_listVm, nameof(_listVm.IsLoading), () =>
        {
            _statusLabel.Text = _listVm.IsLoading ? "⏳ Loading templates..." : "Prompt Templates";
            _refreshBtn.Enabled = !_listVm.IsLoading;
        });

        _binder.BindProperty(_listVm, nameof(_listVm.ErrorMessage), () =>
        {
            errorField.Visible = !string.IsNullOrEmpty(_listVm.ErrorMessage);
            errorField.Text = _listVm.ErrorMessage ?? "";
        });

        _binder.BindProperty(_listVm, nameof(_listVm.TotalCount), () =>
        {
            countLabel.Text = $"Templates: {_listVm.TotalCount}";
        });

        // SelectedItem drives detail load — pure MVVM, no race conditions
        _binder.BindProperty(_listVm, nameof(_listVm.SelectedItem), () =>
        {
            var selected = _listVm.SelectedItem;
            if (selected is null) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _detailVm.LoadAsync(selected.Id).ConfigureAwait(false);
                    var detail = _detailVm.Detail;
                    Application.Invoke(() =>
                    {
                        _detailView.Text = detail?.Content ?? "(No content)";
                        _detailView.SetNeedsDraw();
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());
                    Application.Invoke(() =>
                    {
                        _detailView.Text = $"Error: {ex.Message}";
                        _detailView.SetNeedsDraw();
                    });
                }
            });
        });

        _binder.BindCollection(_listVm.Items, _tableView, items =>
        {
            return new EnumerableTableSource<TemplateListItem>(
                items,
                new Dictionary<string, Func<TemplateListItem, object>>
                {
                    ["ID"] = t => Truncate(t.Id, 30),
                    ["Title"] = t => Truncate(t.Title, 35),
                    ["Category"] = t => Truncate(t.Category, 15),
                    ["Tags"] = t => Truncate(string.Join(", ", t.Tags), 25),
                    ["Description"] = t => Truncate(t.Description ?? "", 40),
                });
        });
    }

    /// <summary>Triggers initial data load.</summary>
    public async Task LoadAsync()
    {
        await _listVm.LoadAsync().ConfigureAwait(false);
    }

    private TemplateListItem? GetSelectedTemplate() => _listVm.SelectedItem;

    private void ShowTemplateDialog(bool isNew)
    {
        if (!isNew)
        {
            var selected = GetSelectedTemplate();
            if (selected is null) return;

            if (_detailVm.Detail is null || _detailVm.Detail.Id != selected.Id)
            {
                _ = Task.Run(async () =>
                {
                    await _detailVm.LoadAsync(selected.Id).ConfigureAwait(false);
                    _detailVm.PopulateEditorFromDetail();
                    Application.Invoke(() => OpenEditorDialog(isNew: false));
                });
                return;
            }

            _detailVm.PopulateEditorFromDetail();
        }
        else
        {
            _detailVm.BeginNewDraft();
        }

        OpenEditorDialog(isNew);
    }

    private void OpenEditorDialog(bool isNew)
    {
        var dlg = new Dialog
        {
            Title = isNew ? "Create Template" : $"Edit Template: {_detailVm.EditorId}",
            Width = Math.Min(100, Math.Max(70, Application.Top?.Frame.Width - 10 ?? 90)),
            Height = Math.Min(25, Math.Max(16, Application.Top?.Frame.Height - 6 ?? 20)),
        };

        var idLabel = new Label { X = 1, Y = 1, Text = "ID:" };
        var idField = new TextField { X = 14, Y = 1, Width = Dim.Fill(2), Text = _detailVm.EditorId, ReadOnly = !isNew };

        var titleLabel = new Label { X = 1, Y = 2, Text = "Title:" };
        var titleField = new TextField { X = 14, Y = 2, Width = Dim.Fill(2), Text = _detailVm.EditorTitle };

        var catLabel = new Label { X = 1, Y = 3, Text = "Category:" };
        var catField = new TextField { X = 14, Y = 3, Width = Dim.Fill(2), Text = _detailVm.EditorCategory };

        var tagsLabel = new Label { X = 1, Y = 4, Text = "Tags:" };
        var tagsField = new TextField { X = 14, Y = 4, Width = Dim.Fill(2), Text = _detailVm.EditorTags };

        var engineLabel = new Label { X = 1, Y = 5, Text = "Engine:" };
        var engineField = new TextField { X = 14, Y = 5, Width = Dim.Fill(2), Text = _detailVm.EditorEngine };

        var descLabel = new Label { X = 1, Y = 6, Text = "Description:" };
        var descField = new TextField { X = 14, Y = 6, Width = Dim.Fill(2), Text = _detailVm.EditorDescription };

        var contentLabel = new Label { X = 1, Y = 8, Text = "Content:" };
        var contentField = new TextView
        {
            X = 1,
            Y = 9,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            Text = _detailVm.EditorContent,
            WordWrap = true,
        };

        dlg.Add(idLabel, idField, titleLabel, titleField, catLabel, catField,
                tagsLabel, tagsField, engineLabel, engineField, descLabel, descField,
                contentLabel, contentField);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            _detailVm.EditorId = idField.Text?.ToString() ?? "";
            _detailVm.EditorTitle = titleField.Text?.ToString() ?? "";
            _detailVm.EditorCategory = catField.Text?.ToString() ?? "";
            _detailVm.EditorTags = tagsField.Text?.ToString() ?? "";
            _detailVm.EditorEngine = engineField.Text?.ToString() ?? "";
            _detailVm.EditorDescription = descField.Text?.ToString() ?? "";
            _detailVm.EditorContent = contentField.Text?.ToString() ?? "";

            _ = Task.Run(async () =>
            {
                var ok = await _detailVm.SaveAsync().ConfigureAwait(false);
                if (ok)
                {
                    Application.Invoke(() => Application.RequestStop());
                    await LoadAsync().ConfigureAwait(false);
                }
                else
                {
                    var err = _detailVm.ErrorMessage ?? "Template save failed.";
                    Application.Invoke(() => MessageBox.ErrorQuery("Save Failed", err, "OK"));
                }
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();

        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = GetSelectedTemplate();
        if (selected is null) return;

        try
        {
            if (_detailVm.Detail?.Id != selected.Id)
                await _detailVm.LoadAsync(selected.Id).ConfigureAwait(false);

            var ok = await _detailVm.DeleteAsync().ConfigureAwait(false);
            if (ok)
            {
                await LoadAsync().ConfigureAwait(false);
            }
            else
            {
                var err = _detailVm.ErrorMessage ?? "Delete failed.";
                Application.Invoke(() => MessageBox.ErrorQuery("Delete Failed", err, "OK"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            Application.Invoke(() => MessageBox.ErrorQuery("Error", ex.Message, "OK"));
        }
    }

    private void ShowTestDialog()
    {
        var selected = GetSelectedTemplate();
        if (selected is null) return;

        var dlg = new Dialog
        {
            Title = $"Test Template: {selected.Id}",
            Width = Math.Min(100, Math.Max(70, Application.Top?.Frame.Width - 10 ?? 90)),
            Height = Math.Min(20, Math.Max(14, Application.Top?.Frame.Height - 6 ?? 16)),
        };

        var varsLabel = new Label { X = 1, Y = 1, Text = "Variables (JSON):" };
        var varsField = new TextView
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2),
            Height = 4,
            Text = "{}",
        };

        var resultLabel = new Label { X = 1, Y = 7, Text = "Result:" };
        var resultField = new TextView
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            ReadOnly = true,
        };

        dlg.Add(varsLabel, varsField, resultLabel, resultField);

        var runBtn = new Button { Text = "Run Test" };
        runBtn.Accepting += (_, _) =>
        {
            var varsJson = varsField.Text?.ToString() ?? "{}";
            _ = Task.Run(async () =>
            {
                var result = await _detailVm.TestAsync(varsJson).ConfigureAwait(false);
                Application.Invoke(() =>
                {
                    resultField.Text = result ?? _detailVm.ErrorMessage ?? "No output.";
                });
            });
        };

        var closeBtn = new Button { Text = "Close" };
        closeBtn.Accepting += (_, _) => Application.RequestStop();

        dlg.AddButton(runBtn);
        dlg.AddButton(closeBtn);
        Application.Run(dlg);
    }

    private static string Truncate(string value, int maxLen)
    {
        if (value.Length <= maxLen) return value.PadRight(maxLen);
        return string.Concat(value.AsSpan(0, maxLen - 3), "...");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _binder.Dispose();
        base.Dispose(disposing);
    }
}
