using System.Text;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for Tool Registry tool and bucket management.
/// </summary>
internal sealed class ToolRegistryScreen : View
{
    private readonly ToolListViewModel _toolListVm;
    private readonly ToolDetailViewModel _toolDetailVm;
    private readonly BucketListViewModel _bucketListVm;
    private readonly BucketDetailViewModel _bucketDetailVm;
    private readonly ILogger<ToolRegistryScreen> _logger;

    private readonly List<ToolListItem> _toolRows = [];
    private readonly List<BucketDetail> _bucketRows = [];

    private TextField _searchField = null!;
    private TableView _toolsTable = null!;
    private TableView _bucketsTable = null!;
    private TextView _detailView = null!;
    private TextView _statusLabel = null!;

    public ToolRegistryScreen(
        ToolListViewModel toolListVm,
        ToolDetailViewModel toolDetailVm,
        BucketListViewModel bucketListVm,
        BucketDetailViewModel bucketDetailVm,
        ILogger<ToolRegistryScreen>? logger = null)
    {
        _toolListVm = toolListVm;
        _toolDetailVm = toolDetailVm;
        _bucketListVm = bucketListVm;
        _bucketDetailVm = bucketDetailVm;
        _logger = logger ?? NullLogger<ToolRegistryScreen>.Instance;

        Title = "Tool Registry";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        var searchLabel = new Label { X = 0, Y = 0, Text = "Search:" };
        _searchField = new TextField { X = Pos.Right(searchLabel) + 1, Y = 0, Width = 34, Text = "" };

        var searchBtn = new Button { X = Pos.Right(_searchField) + 1, Y = 0, Text = "Search" };
        searchBtn.Accepting += (_, _) => _ = Task.Run(SearchAsync);
        var clearBtn = new Button { X = Pos.Right(searchBtn) + 1, Y = 0, Text = "Clear" };
        clearBtn.Accepting += (_, _) =>
        {
            _searchField.Text = string.Empty;
            _ = Task.Run(SearchAsync);
        };

        Add(searchLabel, _searchField, searchBtn, clearBtn);

        var leftPane = new View
        {
            X = 0,
            Y = 1,
            Width = Dim.Percent(58),
            Height = Dim.Fill(4),
        };
        Add(leftPane);

        var detailFrame = new FrameView
        {
            Title = "Detail",
            X = Pos.Right(leftPane),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
        };
        Add(detailFrame);

        var toolsFrame = new FrameView
        {
            Title = "Tools",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(58),
        };
        _toolsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _toolsTable.SelectedCellChanged += (_, _) => _ = Task.Run(RefreshSelectedToolDetailAsync);
        toolsFrame.Add(_toolsTable);
        leftPane.Add(toolsFrame);

        var bucketsFrame = new FrameView
        {
            Title = "Buckets",
            X = 0,
            Y = Pos.Bottom(toolsFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _bucketsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _bucketsTable.SelectedCellChanged += (_, _) => RefreshSelectedBucketDetail();
        bucketsFrame.Add(_bucketsTable);
        leftPane.Add(bucketsFrame);

        _detailView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Text = "Select a tool or bucket to view details.",
        };
        detailFrame.Add(_detailView);

        _statusLabel = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        Add(_statusLabel);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAllAsync);

        var newToolBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "New Tool" };
        newToolBtn.Accepting += (_, _) => ShowCreateToolDialog();

        var editToolBtn = new Button { X = Pos.Right(newToolBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Edit Tool" };
        editToolBtn.Accepting += (_, _) => ShowEditToolDialog();

        var deleteToolBtn = new Button { X = Pos.Right(editToolBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete Tool" };
        deleteToolBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedToolAsync);

        var addBucketBtn = new Button { X = Pos.Right(deleteToolBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Add Bucket" };
        addBucketBtn.Accepting += (_, _) => ShowAddBucketDialog();

        var removeBucketBtn = new Button { X = Pos.Right(addBucketBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Remove Bucket" };
        removeBucketBtn.Accepting += (_, _) => _ = Task.Run(RemoveSelectedBucketAsync);

        var browseBucketBtn = new Button { X = Pos.Right(removeBucketBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Browse Bucket" };
        browseBucketBtn.Accepting += (_, _) => _ = Task.Run(BrowseSelectedBucketAsync);

        var syncBucketBtn = new Button { X = Pos.Right(browseBucketBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Sync Bucket" };
        syncBucketBtn.Accepting += (_, _) => _ = Task.Run(SyncSelectedBucketAsync);

        Add(refreshBtn, newToolBtn, editToolBtn, deleteToolBtn, addBucketBtn, removeBucketBtn, browseBucketBtn, syncBucketBtn);
    }

    public async Task LoadAllAsync()
    {
        await LoadToolsAsync(_searchField.Text?.ToString()).ConfigureAwait(true);
        await LoadBucketsAsync().ConfigureAwait(true);
    }

    private Task SearchAsync()
        => LoadToolsAsync(_searchField.Text?.ToString());

    private async Task LoadToolsAsync(string? keyword)
    {
        try
        {
            await _toolListVm.LoadAsync(keyword).ConfigureAwait(true);
            Application.Invoke(() =>
            {
                _toolRows.Clear();
                _toolRows.AddRange(_toolListVm.Items);
                _toolsTable.Table = new EnumerableTableSource<ToolListItem>(
                    _toolRows,
                    new Dictionary<string, Func<ToolListItem, object>>
                    {
                        ["ID"] = t => t.Id,
                        ["Name"] = t => t.Name,
                        ["Description"] = t => t.Description,
                        ["Tags"] = t => string.Join(", ", t.Tags),
                        ["Scope"] = t => string.IsNullOrWhiteSpace(t.WorkspacePath) ? "global" : "workspace",
                    });
                if (_toolRows.Count > 0 &&
                    (_toolsTable.SelectedRow < 0 || _toolsTable.SelectedRow >= _toolRows.Count))
                {
                    _toolsTable.SelectedRow = 0;
                }
            });

            if (!string.IsNullOrWhiteSpace(_toolListVm.ErrorMessage))
                SetStatus(_toolListVm.ErrorMessage);
            else
                SetStatus(_toolListVm.StatusMessage ?? $"Loaded {_toolRows.Count} tools.");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Tool load failed: {ex.Message}");
        }
    }

    private async Task LoadBucketsAsync()
    {
        try
        {
            await _bucketListVm.LoadAsync().ConfigureAwait(true);
            Application.Invoke(() =>
            {
                _bucketRows.Clear();
                _bucketRows.AddRange(_bucketListVm.Items);
                _bucketsTable.Table = new EnumerableTableSource<BucketDetail>(
                    _bucketRows,
                    new Dictionary<string, Func<BucketDetail, object>>
                    {
                        ["Name"] = b => b.Name,
                        ["Owner"] = b => b.Owner,
                        ["Repo"] = b => b.Repo,
                        ["Branch"] = b => b.Branch,
                        ["Manifest"] = b => b.ManifestPath,
                    });
                if (_bucketRows.Count > 0 &&
                    (_bucketsTable.SelectedRow < 0 || _bucketsTable.SelectedRow >= _bucketRows.Count))
                {
                    _bucketsTable.SelectedRow = 0;
                }
            });

            if (!string.IsNullOrWhiteSpace(_bucketListVm.ErrorMessage))
                SetStatus(_bucketListVm.ErrorMessage);
            else
                SetStatus(_bucketListVm.StatusMessage ?? $"Loaded {_bucketRows.Count} buckets.");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Bucket load failed: {ex.Message}");
        }
    }

    private ToolListItem? GetSelectedTool()
    {
        var row = _toolsTable.SelectedRow;
        return row >= 0 && row < _toolRows.Count ? _toolRows[row] : null;
    }

    private BucketDetail? GetSelectedBucket()
    {
        var row = _bucketsTable.SelectedRow;
        return row >= 0 && row < _bucketRows.Count ? _bucketRows[row] : null;
    }

    private async Task RefreshSelectedToolDetailAsync()
    {
        var selected = GetSelectedTool();
        if (selected is null)
            return;

        var detail = await _toolDetailVm.LoadAsync(selected.Id).ConfigureAwait(true);
        if (detail is null)
        {
            SetStatus(_toolDetailVm.ErrorMessage ?? "Tool detail load failed.");
            return;
        }

        SetDetail(FormatToolDetail(detail));
        SetStatus(_toolDetailVm.StatusMessage ?? $"Loaded tool #{detail.Id}.");
    }

    private void RefreshSelectedBucketDetail()
    {
        var selected = GetSelectedBucket();
        if (selected is null)
            return;
        _bucketDetailVm.Detail = selected;
        SetDetail(FormatBucketDetail(selected));
    }

    private void ShowCreateToolDialog()
    {
        var dlg = new Dialog { Title = "Create Tool", Width = 92, Height = 22 };
        BuildToolEditorFields(dlg, null, out var nameField, out var descriptionField, out var tagsField,
            out var schemaView, out var commandView, out var scopeField);

        var createBtn = new Button { Text = "Create" };
        createBtn.Accepting += (_, _) =>
        {
            var cmd = new CreateToolCommand
            {
                Name = nameField.Text?.ToString()?.Trim() ?? string.Empty,
                Description = descriptionField.Text?.ToString()?.Trim() ?? string.Empty,
                Tags = ParseCsv(tagsField.Text?.ToString()),
                ParameterSchema = NullIfWhitespace(schemaView.Text?.ToString()),
                CommandTemplate = NullIfWhitespace(commandView.Text?.ToString()),
                WorkspacePath = NullIfWhitespace(scopeField.Text?.ToString()),
            };

            if (string.IsNullOrWhiteSpace(cmd.Name) || string.IsNullOrWhiteSpace(cmd.Description))
            {
                SetStatus("Tool name and description are required.");
                return;
            }

            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _toolDetailVm.CreateAsync(cmd).ConfigureAwait(true);
                if (outcome is not { Success: true })
                {
                    SetStatus(_toolDetailVm.ErrorMessage ?? outcome?.Error ?? "Tool create failed.");
                    return;
                }

                await LoadToolsAsync(_searchField.Text?.ToString()).ConfigureAwait(true);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(createBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowEditToolDialog()
    {
        var selected = GetSelectedTool();
        if (selected is null)
        {
            SetStatus("Select a tool first.");
            return;
        }

        _ = Task.Run(async () =>
        {
            var detail = await _toolDetailVm.LoadAsync(selected.Id).ConfigureAwait(true);
            if (detail is null)
            {
                SetStatus(_toolDetailVm.ErrorMessage ?? "Tool detail load failed.");
                return;
            }

            Application.Invoke(() => OpenToolEditDialog(detail));
        });
    }

    private void OpenToolEditDialog(ToolDetail detail)
    {
        var dlg = new Dialog { Title = $"Edit Tool #{detail.Id}", Width = 92, Height = 22 };
        BuildToolEditorFields(dlg, detail, out var nameField, out var descriptionField, out var tagsField,
            out var schemaView, out var commandView, out var scopeField);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var cmd = new UpdateToolCommand
            {
                ToolId = detail.Id,
                Name = nameField.Text?.ToString()?.Trim(),
                Description = descriptionField.Text?.ToString()?.Trim(),
                Tags = ParseCsvOrNull(tagsField.Text?.ToString()),
                ParameterSchema = NullIfWhitespace(schemaView.Text?.ToString()),
                CommandTemplate = NullIfWhitespace(commandView.Text?.ToString()),
                WorkspacePath = NullIfWhitespace(scopeField.Text?.ToString()),
            };

            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _toolDetailVm.UpdateAsync(cmd).ConfigureAwait(true);
                if (outcome is not { Success: true })
                {
                    SetStatus(_toolDetailVm.ErrorMessage ?? outcome?.Error ?? "Tool save failed.");
                    return;
                }

                await LoadToolsAsync(_searchField.Text?.ToString()).ConfigureAwait(true);
                await RefreshSelectedToolDetailAsync().ConfigureAwait(true);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task DeleteSelectedToolAsync()
    {
        var selected = GetSelectedTool();
        if (selected is null)
        {
            SetStatus("Select a tool first.");
            return;
        }

        var outcome = await _toolDetailVm.DeleteAsync(selected.Id).ConfigureAwait(true);
        if (outcome is not { Success: true })
        {
            SetStatus(_toolDetailVm.ErrorMessage ?? outcome?.Error ?? "Tool delete failed.");
            return;
        }

        await LoadToolsAsync(_searchField.Text?.ToString()).ConfigureAwait(true);
        SetDetail("Tool deleted.");
    }

    private void ShowAddBucketDialog()
    {
        var dlg = new Dialog { Title = "Add Bucket", Width = 76, Height = 16 };
        var row = 1;
        dlg.Add(new Label { X = 1, Y = row, Text = "Name:" });
        var nameField = new TextField { X = 18, Y = row, Width = Dim.Fill(2), Text = "" };
        dlg.Add(nameField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Owner:" });
        var ownerField = new TextField { X = 18, Y = row, Width = Dim.Fill(2), Text = "" };
        dlg.Add(ownerField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Repo:" });
        var repoField = new TextField { X = 18, Y = row, Width = Dim.Fill(2), Text = "" };
        dlg.Add(repoField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Branch:" });
        var branchField = new TextField { X = 18, Y = row, Width = Dim.Fill(2), Text = "main" };
        dlg.Add(branchField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Manifest Path:" });
        var manifestField = new TextField { X = 18, Y = row, Width = Dim.Fill(2), Text = "tools" };
        dlg.Add(manifestField);

        var addBtn = new Button { Text = "Add" };
        addBtn.Accepting += (_, _) =>
        {
            var cmd = new AddBucketCommand
            {
                Name = nameField.Text?.ToString()?.Trim() ?? string.Empty,
                Owner = ownerField.Text?.ToString()?.Trim() ?? string.Empty,
                Repo = repoField.Text?.ToString()?.Trim() ?? string.Empty,
                Branch = NullIfWhitespace(branchField.Text?.ToString()),
                ManifestPath = NullIfWhitespace(manifestField.Text?.ToString()),
            };

            if (string.IsNullOrWhiteSpace(cmd.Name) ||
                string.IsNullOrWhiteSpace(cmd.Owner) ||
                string.IsNullOrWhiteSpace(cmd.Repo))
            {
                SetStatus("Bucket name, owner, and repo are required.");
                return;
            }

            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _bucketDetailVm.AddAsync(cmd).ConfigureAwait(true);
                if (outcome is not { Success: true })
                {
                    SetStatus(_bucketDetailVm.ErrorMessage ?? outcome?.Error ?? "Add bucket failed.");
                    return;
                }

                await LoadBucketsAsync().ConfigureAwait(true);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(addBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task RemoveSelectedBucketAsync()
    {
        var selected = GetSelectedBucket();
        if (selected is null)
        {
            SetStatus("Select a bucket first.");
            return;
        }

        var outcome = await _bucketDetailVm.RemoveAsync(selected.Name, uninstallTools: false).ConfigureAwait(true);
        if (outcome is not { Success: true })
        {
            SetStatus(_bucketDetailVm.ErrorMessage ?? outcome?.Error ?? "Remove bucket failed.");
            return;
        }

        await LoadBucketsAsync().ConfigureAwait(true);
        SetDetail("Bucket removed.");
    }

    private async Task SyncSelectedBucketAsync()
    {
        var selected = GetSelectedBucket();
        if (selected is null)
        {
            SetStatus("Select a bucket first.");
            return;
        }

        var outcome = await _bucketDetailVm.SyncAsync(selected.Name).ConfigureAwait(true);
        if (outcome is null || !outcome.Success)
        {
            SetStatus(_bucketDetailVm.ErrorMessage ?? outcome?.Error ?? "Bucket sync failed.");
            return;
        }

        await LoadBucketsAsync().ConfigureAwait(true);
    }

    private async Task BrowseSelectedBucketAsync()
    {
        var selected = GetSelectedBucket();
        if (selected is null)
        {
            SetStatus("Select a bucket first.");
            return;
        }

        var outcome = await _bucketDetailVm.BrowseAsync(selected.Name).ConfigureAwait(true);
        if (outcome is null || !outcome.Success)
        {
            SetStatus(_bucketDetailVm.ErrorMessage ?? outcome?.Error ?? "Bucket browse failed.");
            return;
        }

        Application.Invoke(() => ShowBrowseResultsDialog(selected.Name, outcome.Tools));
    }

    private void ShowBrowseResultsDialog(string bucketName, IReadOnlyList<BucketToolManifest> tools)
    {
        var dlg = new Dialog
        {
            Title = $"Bucket Manifests: {bucketName}",
            Width = 100,
            Height = 24,
        };

        var listRows = tools.Select(t => $"{t.Name} — {t.Description}").ToList();
        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        listView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(listRows));
        dlg.Add(listView);

        var installBtn = new Button { Text = "Install Selected" };
        installBtn.Accepting += (_, _) =>
        {
            var index = listView.SelectedItem;
            if (index < 0 || index >= tools.Count)
                return;

            var tool = tools[index];
            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var installOutcome = await _bucketDetailVm.InstallAsync(bucketName, tool.Name).ConfigureAwait(true);
                if (installOutcome is not { Success: true })
                {
                    SetStatus(_bucketDetailVm.ErrorMessage ?? installOutcome?.Error ?? "Install failed.");
                    return;
                }

                await LoadToolsAsync(_searchField.Text?.ToString()).ConfigureAwait(true);
                SetStatus($"Installed '{tool.Name}' from '{bucketName}'.");
            });
        };

        var closeBtn = new Button { Text = "Close" };
        closeBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(installBtn);
        dlg.AddButton(closeBtn);
        Application.Run(dlg);
    }

    private static void BuildToolEditorFields(
        Dialog dlg,
        ToolDetail? detail,
        out TextField nameField,
        out TextField descriptionField,
        out TextField tagsField,
        out TextView schemaView,
        out TextView commandView,
        out TextField scopeField)
    {
        var row = 1;
        dlg.Add(new Label { X = 1, Y = row, Text = "Name:" });
        nameField = new TextField
        {
            X = 18,
            Y = row,
            Width = Dim.Fill(2),
            Text = detail?.Name ?? string.Empty,
        };
        dlg.Add(nameField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Description:" });
        descriptionField = new TextField
        {
            X = 18,
            Y = row,
            Width = Dim.Fill(2),
            Text = detail?.Description ?? string.Empty,
        };
        dlg.Add(descriptionField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Tags CSV:" });
        tagsField = new TextField
        {
            X = 18,
            Y = row,
            Width = Dim.Fill(2),
            Text = detail is null ? string.Empty : string.Join(", ", detail.Tags),
        };
        dlg.Add(tagsField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Workspace Scope:" });
        scopeField = new TextField
        {
            X = 18,
            Y = row,
            Width = Dim.Fill(2),
            Text = detail?.WorkspacePath ?? string.Empty,
        };
        dlg.Add(scopeField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Parameter Schema:" });
        row++;
        schemaView = new TextView
        {
            X = 1,
            Y = row,
            Width = Dim.Fill(2),
            Height = 4,
            WordWrap = true,
            Text = detail?.ParameterSchema ?? string.Empty,
        };
        dlg.Add(schemaView);
        row += 4;

        dlg.Add(new Label { X = 1, Y = row, Text = "Command Template:" });
        row++;
        commandView = new TextView
        {
            X = 1,
            Y = row,
            Width = Dim.Fill(2),
            Height = 4,
            WordWrap = true,
            Text = detail?.CommandTemplate ?? string.Empty,
        };
        dlg.Add(commandView);
    }

    private static string FormatToolDetail(ToolDetail detail)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Tool #{detail.Id}");
        sb.AppendLine($"Name: {detail.Name}");
        sb.AppendLine($"Description: {detail.Description}");
        sb.AppendLine($"Tags: {string.Join(", ", detail.Tags)}");
        sb.AppendLine($"Scope: {(string.IsNullOrWhiteSpace(detail.WorkspacePath) ? "global" : detail.WorkspacePath)}");
        sb.AppendLine($"Created: {detail.DateTimeCreated:O}");
        sb.AppendLine($"Modified: {detail.DateTimeModified:O}");
        sb.AppendLine("Parameter Schema:");
        sb.AppendLine(detail.ParameterSchema ?? string.Empty);
        sb.AppendLine("Command Template:");
        sb.AppendLine(detail.CommandTemplate ?? string.Empty);
        return sb.ToString().TrimEnd();
    }

    private static string FormatBucketDetail(BucketDetail detail)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Bucket #{detail.Id}");
        sb.AppendLine($"Name: {detail.Name}");
        sb.AppendLine($"Owner/Repo: {detail.Owner}/{detail.Repo}");
        sb.AppendLine($"Branch: {detail.Branch}");
        sb.AppendLine($"Manifest Path: {detail.ManifestPath}");
        sb.AppendLine($"Created: {detail.DateTimeCreated:O}");
        sb.AppendLine($"Last Synced: {(detail.DateTimeLastSynced.HasValue ? detail.DateTimeLastSynced.Value.ToString("O") : "(never)")}");
        return sb.ToString().TrimEnd();
    }

    private void SetStatus(string text)
        => Application.Invoke(() => _statusLabel.Text = text);

    private void SetDetail(string text)
        => Application.Invoke(() => _detailView.Text = text);

    private static string? NullIfWhitespace(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

    private static IReadOnlyList<string> ParseCsv(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();

    private static IReadOnlyList<string>? ParseCsvOrNull(string? raw)
    {
        var values = ParseCsv(raw);
        return values.Count == 0 ? null : values;
    }
}
