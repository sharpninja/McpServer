using System.Text;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for requirements (FR/TR/Test/Mapping) management and generation.
/// </summary>
internal sealed class RequirementsScreen : View
{
    private readonly FrListViewModel _frListVm;
    private readonly FrDetailViewModel _frDetailVm;
    private readonly TrListViewModel _trListVm;
    private readonly TrDetailViewModel _trDetailVm;
    private readonly TestListViewModel _testListVm;
    private readonly TestDetailViewModel _testDetailVm;
    private readonly MappingListViewModel _mappingListVm;
    private readonly RequirementsGenerateViewModel _generateVm;
    private readonly ILogger<RequirementsScreen> _logger;

    private readonly List<FunctionalRequirementItem> _frRows = [];
    private readonly List<TechnicalRequirementItem> _trRows = [];
    private readonly List<TestingRequirementItem> _testRows = [];
    private readonly List<RequirementMappingItem> _mappingRows = [];

    private TableView _frTable = null!;
    private TextView _frDetailView = null!;
    private TableView _trTable = null!;
    private TextView _trDetailView = null!;
    private TableView _testTable = null!;
    private TextView _testDetailView = null!;
    private TableView _mappingTable = null!;
    private TextView _mappingDetailView = null!;
    private TextView _statusLabel = null!;
    private TextField _docSelectorField = null!;

    public RequirementsScreen(
        FrListViewModel frListVm,
        FrDetailViewModel frDetailVm,
        TrListViewModel trListVm,
        TrDetailViewModel trDetailVm,
        TestListViewModel testListVm,
        TestDetailViewModel testDetailVm,
        MappingListViewModel mappingListVm,
        RequirementsGenerateViewModel generateVm,
        ILogger<RequirementsScreen>? logger = null)
    {
        _frListVm = frListVm;
        _frDetailVm = frDetailVm;
        _trListVm = trListVm;
        _trDetailVm = trDetailVm;
        _testListVm = testListVm;
        _testDetailVm = testDetailVm;
        _mappingListVm = mappingListVm;
        _generateVm = generateVm;
        _logger = logger ?? NullLogger<RequirementsScreen>.Instance;

        Title = "Requirements";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        var tabView = new TabView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
        };
        Add(tabView);

        tabView.AddTab(new Tab { DisplayText = "FR", View = BuildFrTab() }, andSelect: true);
        tabView.AddTab(new Tab { DisplayText = "TR", View = BuildTrTab() }, andSelect: false);
        tabView.AddTab(new Tab { DisplayText = "Test", View = BuildTestTab() }, andSelect: false);
        tabView.AddTab(new Tab { DisplayText = "Mapping", View = BuildMappingTab() }, andSelect: false);

        var selectorLabel = new Label { X = 0, Y = Pos.AnchorEnd(2), Text = "Generate Doc:" };
        _docSelectorField = new TextField { X = Pos.Right(selectorLabel) + 1, Y = Pos.AnchorEnd(2), Width = 18, Text = "all" };
        var generateBtn = new Button { X = Pos.Right(_docSelectorField) + 1, Y = Pos.AnchorEnd(2), Text = "Generate" };
        generateBtn.Accepting += (_, _) => _ = Task.Run(GenerateAsync);
        var refreshAllBtn = new Button { X = Pos.Right(generateBtn) + 1, Y = Pos.AnchorEnd(2), Text = "Refresh All" };
        refreshAllBtn.Accepting += (_, _) => _ = Task.Run(LoadAllAsync);
        Add(selectorLabel, _docSelectorField, generateBtn, refreshAllBtn);

        _statusLabel = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        Add(_statusLabel);
    }

    private View BuildFrTab()
    {
        var root = new View { Width = Dim.Fill(), Height = Dim.Fill() };
        var tableFrame = new FrameView
        {
            Title = "Functional Requirements",
            X = 0,
            Y = 0,
            Width = Dim.Percent(55),
            Height = Dim.Fill(2),
        };
        _frTable = new TableView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), FullRowSelect = true, MultiSelect = false };
        _frTable.SelectedCellChanged += (_, _) => RefreshSelectedFrDetail();
        tableFrame.Add(_frTable);
        root.Add(tableFrame);

        var detailFrame = new FrameView
        {
            Title = "Detail",
            X = Pos.Right(tableFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        _frDetailView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true, Text = "" };
        detailFrame.Add(_frDetailView);
        root.Add(detailFrame);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadFrAsync);
        var newBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "New" };
        newBtn.Accepting += (_, _) => ShowFrEditorDialog(createMode: true);
        var editBtn = new Button { X = Pos.Right(newBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Edit" };
        editBtn.Accepting += (_, _) => ShowFrEditorDialog(createMode: false);
        var deleteBtn = new Button { X = Pos.Right(editBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedFrAsync);
        root.Add(refreshBtn, newBtn, editBtn, deleteBtn);

        return root;
    }

    private View BuildTrTab()
    {
        var root = new View { Width = Dim.Fill(), Height = Dim.Fill() };
        var tableFrame = new FrameView
        {
            Title = "Technical Requirements",
            X = 0,
            Y = 0,
            Width = Dim.Percent(55),
            Height = Dim.Fill(2),
        };
        _trTable = new TableView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), FullRowSelect = true, MultiSelect = false };
        _trTable.SelectedCellChanged += (_, _) => RefreshSelectedTrDetail();
        tableFrame.Add(_trTable);
        root.Add(tableFrame);

        var detailFrame = new FrameView
        {
            Title = "Detail",
            X = Pos.Right(tableFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        _trDetailView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true, Text = "" };
        detailFrame.Add(_trDetailView);
        root.Add(detailFrame);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadTrAsync);
        var newBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "New" };
        newBtn.Accepting += (_, _) => ShowTrEditorDialog(createMode: true);
        var editBtn = new Button { X = Pos.Right(newBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Edit" };
        editBtn.Accepting += (_, _) => ShowTrEditorDialog(createMode: false);
        var deleteBtn = new Button { X = Pos.Right(editBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedTrAsync);
        root.Add(refreshBtn, newBtn, editBtn, deleteBtn);
        return root;
    }

    private View BuildTestTab()
    {
        var root = new View { Width = Dim.Fill(), Height = Dim.Fill() };
        var tableFrame = new FrameView
        {
            Title = "Testing Requirements",
            X = 0,
            Y = 0,
            Width = Dim.Percent(55),
            Height = Dim.Fill(2),
        };
        _testTable = new TableView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), FullRowSelect = true, MultiSelect = false };
        _testTable.SelectedCellChanged += (_, _) => RefreshSelectedTestDetail();
        tableFrame.Add(_testTable);
        root.Add(tableFrame);

        var detailFrame = new FrameView
        {
            Title = "Detail",
            X = Pos.Right(tableFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        _testDetailView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true, Text = "" };
        detailFrame.Add(_testDetailView);
        root.Add(detailFrame);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadTestAsync);
        var newBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "New" };
        newBtn.Accepting += (_, _) => ShowTestEditorDialog(createMode: true);
        var editBtn = new Button { X = Pos.Right(newBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Edit" };
        editBtn.Accepting += (_, _) => ShowTestEditorDialog(createMode: false);
        var deleteBtn = new Button { X = Pos.Right(editBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedTestAsync);
        root.Add(refreshBtn, newBtn, editBtn, deleteBtn);
        return root;
    }

    private View BuildMappingTab()
    {
        var root = new View { Width = Dim.Fill(), Height = Dim.Fill() };
        var tableFrame = new FrameView
        {
            Title = "FR -> TR Mapping",
            X = 0,
            Y = 0,
            Width = Dim.Percent(55),
            Height = Dim.Fill(2),
        };
        _mappingTable = new TableView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), FullRowSelect = true, MultiSelect = false };
        _mappingTable.SelectedCellChanged += (_, _) => RefreshSelectedMappingDetail();
        tableFrame.Add(_mappingTable);
        root.Add(tableFrame);

        var detailFrame = new FrameView
        {
            Title = "Detail",
            X = Pos.Right(tableFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        _mappingDetailView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true, Text = "" };
        detailFrame.Add(_mappingDetailView);
        root.Add(detailFrame);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadMappingAsync);
        var upsertBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Upsert" };
        upsertBtn.Accepting += (_, _) => ShowMappingEditorDialog();
        var deleteBtn = new Button { X = Pos.Right(upsertBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedMappingAsync);
        root.Add(refreshBtn, upsertBtn, deleteBtn);
        return root;
    }

    public async Task LoadAllAsync()
    {
        await LoadFrAsync().ConfigureAwait(false);
        await LoadTrAsync().ConfigureAwait(false);
        await LoadTestAsync().ConfigureAwait(false);
        await LoadMappingAsync().ConfigureAwait(false);
    }

    private async Task LoadFrAsync()
    {
        try
        {
            await _frListVm.LoadAsync().ConfigureAwait(false);
            Application.Invoke(() =>
            {
                _frRows.Clear();
                _frRows.AddRange(_frListVm.Items);
                _frTable.Table = new EnumerableTableSource<FunctionalRequirementItem>(
                    _frRows,
                    new Dictionary<string, Func<FunctionalRequirementItem, object>>
                    {
                        ["ID"] = i => i.Id,
                        ["Title"] = i => i.Title,
                    });
                EnsureSelection(_frTable, _frRows.Count);
            });
            SetStatus(_frListVm.ErrorMessage ?? _frListVm.StatusMessage ?? string.Empty);
            RefreshSelectedFrDetail();
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"FR load failed: {ex.Message}");
        }
    }

    private async Task LoadTrAsync()
    {
        try
        {
            await _trListVm.LoadAsync().ConfigureAwait(false);
            Application.Invoke(() =>
            {
                _trRows.Clear();
                _trRows.AddRange(_trListVm.Items);
                _trTable.Table = new EnumerableTableSource<TechnicalRequirementItem>(
                    _trRows,
                    new Dictionary<string, Func<TechnicalRequirementItem, object>>
                    {
                        ["ID"] = i => i.Id,
                        ["Title"] = i => i.Title,
                    });
                EnsureSelection(_trTable, _trRows.Count);
            });
            SetStatus(_trListVm.ErrorMessage ?? _trListVm.StatusMessage ?? string.Empty);
            RefreshSelectedTrDetail();
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"TR load failed: {ex.Message}");
        }
    }

    private async Task LoadTestAsync()
    {
        try
        {
            await _testListVm.LoadAsync().ConfigureAwait(false);
            Application.Invoke(() =>
            {
                _testRows.Clear();
                _testRows.AddRange(_testListVm.Items);
                _testTable.Table = new EnumerableTableSource<TestingRequirementItem>(
                    _testRows,
                    new Dictionary<string, Func<TestingRequirementItem, object>>
                    {
                        ["ID"] = i => i.Id,
                        ["Condition"] = i => i.Condition,
                    });
                EnsureSelection(_testTable, _testRows.Count);
            });
            SetStatus(_testListVm.ErrorMessage ?? _testListVm.StatusMessage ?? string.Empty);
            RefreshSelectedTestDetail();
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Test load failed: {ex.Message}");
        }
    }

    private async Task LoadMappingAsync()
    {
        try
        {
            await _mappingListVm.LoadAsync().ConfigureAwait(false);
            Application.Invoke(() =>
            {
                _mappingRows.Clear();
                _mappingRows.AddRange(_mappingListVm.Items);
                _mappingTable.Table = new EnumerableTableSource<RequirementMappingItem>(
                    _mappingRows,
                    new Dictionary<string, Func<RequirementMappingItem, object>>
                    {
                        ["FR"] = i => i.FrId,
                        ["TR IDs"] = i => string.Join(", ", i.TrIds),
                    });
                EnsureSelection(_mappingTable, _mappingRows.Count);
            });
            SetStatus(_mappingListVm.ErrorMessage ?? _mappingListVm.StatusMessage ?? string.Empty);
            RefreshSelectedMappingDetail();
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Mapping load failed: {ex.Message}");
        }
    }

    private FunctionalRequirementItem? GetSelectedFr()
    {
        var idx = _frTable.SelectedRow;
        return idx >= 0 && idx < _frRows.Count ? _frRows[idx] : null;
    }

    private TechnicalRequirementItem? GetSelectedTr()
    {
        var idx = _trTable.SelectedRow;
        return idx >= 0 && idx < _trRows.Count ? _trRows[idx] : null;
    }

    private TestingRequirementItem? GetSelectedTest()
    {
        var idx = _testTable.SelectedRow;
        return idx >= 0 && idx < _testRows.Count ? _testRows[idx] : null;
    }

    private RequirementMappingItem? GetSelectedMapping()
    {
        var idx = _mappingTable.SelectedRow;
        return idx >= 0 && idx < _mappingRows.Count ? _mappingRows[idx] : null;
    }

    private void RefreshSelectedFrDetail()
    {
        var item = GetSelectedFr();
        if (item is null)
            return;
        SetText(_frDetailView, $"ID: {item.Id}{Environment.NewLine}Title: {item.Title}{Environment.NewLine}{Environment.NewLine}Body:{Environment.NewLine}{item.Body}");
    }

    private void RefreshSelectedTrDetail()
    {
        var item = GetSelectedTr();
        if (item is null)
            return;
        SetText(_trDetailView, $"ID: {item.Id}{Environment.NewLine}Title: {item.Title}{Environment.NewLine}{Environment.NewLine}Body:{Environment.NewLine}{item.Body}");
    }

    private void RefreshSelectedTestDetail()
    {
        var item = GetSelectedTest();
        if (item is null)
            return;
        SetText(_testDetailView, $"ID: {item.Id}{Environment.NewLine}{Environment.NewLine}Condition:{Environment.NewLine}{item.Condition}");
    }

    private void RefreshSelectedMappingDetail()
    {
        var item = GetSelectedMapping();
        if (item is null)
            return;
        SetText(_mappingDetailView, $"FR: {item.FrId}{Environment.NewLine}TR IDs:{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", item.TrIds)}");
    }

    private void ShowFrEditorDialog(bool createMode)
    {
        var existing = createMode ? null : GetSelectedFr();
        if (!createMode && existing is null)
        {
            SetStatus("Select an FR first.");
            return;
        }

        ShowThreeFieldDialog(
            createMode ? "New FR" : $"Edit FR {existing!.Id}",
            createMode ? string.Empty : existing!.Id,
            createMode ? string.Empty : existing!.Title,
            createMode ? string.Empty : existing!.Body,
            idReadOnly: !createMode,
            saveAction: async (id, title, body) =>
            {
                if (createMode)
                    await _frDetailVm.CreateAsync(id, title, body).ConfigureAwait(false);
                else
                    await _frDetailVm.UpdateAsync(id, title, body).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(_frDetailVm.ErrorMessage))
                {
                    SetStatus(_frDetailVm.ErrorMessage);
                    return;
                }
                await LoadFrAsync().ConfigureAwait(false);
            });
    }

    private void ShowTrEditorDialog(bool createMode)
    {
        var existing = createMode ? null : GetSelectedTr();
        if (!createMode && existing is null)
        {
            SetStatus("Select a TR first.");
            return;
        }

        ShowThreeFieldDialog(
            createMode ? "New TR" : $"Edit TR {existing!.Id}",
            createMode ? string.Empty : existing!.Id,
            createMode ? string.Empty : existing!.Title,
            createMode ? string.Empty : existing!.Body,
            idReadOnly: !createMode,
            saveAction: async (id, title, body) =>
            {
                if (createMode)
                    await _trDetailVm.CreateAsync(id, title, body).ConfigureAwait(false);
                else
                    await _trDetailVm.UpdateAsync(id, title, body).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(_trDetailVm.ErrorMessage))
                {
                    SetStatus(_trDetailVm.ErrorMessage);
                    return;
                }
                await LoadTrAsync().ConfigureAwait(false);
            });
    }

    private void ShowTestEditorDialog(bool createMode)
    {
        var existing = createMode ? null : GetSelectedTest();
        if (!createMode && existing is null)
        {
            SetStatus("Select a test requirement first.");
            return;
        }

        var dlg = new Dialog { Title = createMode ? "New Test Requirement" : $"Edit {existing!.Id}", Width = 90, Height = 14 };
        dlg.Add(new Label { X = 1, Y = 1, Text = "ID:" });
        var idField = new TextField
        {
            X = 12,
            Y = 1,
            Width = Dim.Fill(2),
            Text = createMode ? string.Empty : existing!.Id,
            ReadOnly = !createMode
        };
        dlg.Add(idField);
        dlg.Add(new Label { X = 1, Y = 3, Text = "Condition:" });
        var conditionView = new TextView
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            WordWrap = true,
            Text = createMode ? string.Empty : existing!.Condition
        };
        dlg.Add(conditionView);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var id = idField.Text?.ToString()?.Trim() ?? string.Empty;
            var condition = conditionView.Text?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(condition))
            {
                SetStatus("ID and condition are required.");
                return;
            }
            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                if (createMode)
                    await _testDetailVm.CreateAsync(id, condition).ConfigureAwait(false);
                else
                    await _testDetailVm.UpdateAsync(id, condition).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(_testDetailVm.ErrorMessage))
                {
                    SetStatus(_testDetailVm.ErrorMessage);
                    return;
                }
                await LoadTestAsync().ConfigureAwait(false);
            });
        };
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowMappingEditorDialog()
    {
        var existing = GetSelectedMapping();
        var dlg = new Dialog { Title = "Upsert Mapping", Width = 80, Height = 12 };
        dlg.Add(new Label { X = 1, Y = 1, Text = "FR ID:" });
        var frField = new TextField { X = 12, Y = 1, Width = Dim.Fill(2), Text = existing?.FrId ?? string.Empty };
        dlg.Add(frField);
        dlg.Add(new Label { X = 1, Y = 3, Text = "TR IDs CSV:" });
        var trField = new TextField { X = 12, Y = 3, Width = Dim.Fill(2), Text = existing is null ? string.Empty : string.Join(", ", existing.TrIds) };
        dlg.Add(trField);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var frId = frField.Text?.ToString()?.Trim() ?? string.Empty;
            var trIds = ParseCsv(trField.Text?.ToString());
            if (string.IsNullOrWhiteSpace(frId))
            {
                SetStatus("FR ID is required.");
                return;
            }
            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                await _mappingListVm.UpsertAsync(frId, trIds).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(_mappingListVm.ErrorMessage))
                {
                    SetStatus(_mappingListVm.ErrorMessage);
                    return;
                }
                await LoadMappingAsync().ConfigureAwait(false);
            });
        };
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task DeleteSelectedFrAsync()
    {
        var selected = GetSelectedFr();
        if (selected is null)
        {
            SetStatus("Select an FR first.");
            return;
        }

        await _frDetailVm.DeleteAsync(selected.Id).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_frDetailVm.ErrorMessage))
        {
            SetStatus(_frDetailVm.ErrorMessage);
            return;
        }
        await LoadFrAsync().ConfigureAwait(false);
    }

    private async Task DeleteSelectedTrAsync()
    {
        var selected = GetSelectedTr();
        if (selected is null)
        {
            SetStatus("Select a TR first.");
            return;
        }

        await _trDetailVm.DeleteAsync(selected.Id).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_trDetailVm.ErrorMessage))
        {
            SetStatus(_trDetailVm.ErrorMessage);
            return;
        }
        await LoadTrAsync().ConfigureAwait(false);
    }

    private async Task DeleteSelectedTestAsync()
    {
        var selected = GetSelectedTest();
        if (selected is null)
        {
            SetStatus("Select a test requirement first.");
            return;
        }

        await _testDetailVm.DeleteAsync(selected.Id).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_testDetailVm.ErrorMessage))
        {
            SetStatus(_testDetailVm.ErrorMessage);
            return;
        }
        await LoadTestAsync().ConfigureAwait(false);
    }

    private async Task DeleteSelectedMappingAsync()
    {
        var selected = GetSelectedMapping();
        if (selected is null)
        {
            SetStatus("Select a mapping first.");
            return;
        }

        await _mappingListVm.DeleteAsync(selected.FrId).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_mappingListVm.ErrorMessage))
        {
            SetStatus(_mappingListVm.ErrorMessage);
            return;
        }
        await LoadMappingAsync().ConfigureAwait(false);
    }

    private async Task GenerateAsync()
    {
        var selector = _docSelectorField.Text?.ToString() ?? "all";
        var result = await _generateVm.GenerateAsync(selector).ConfigureAwait(false);
        if (result is null)
        {
            SetStatus(_generateVm.ErrorMessage ?? "Generation failed.");
            return;
        }

        SetStatus(_generateVm.StatusMessage ?? "Generated.");
        var isText = result.ContentType?.Contains("text", StringComparison.OrdinalIgnoreCase) == true
            || result.ContentType?.Contains("markdown", StringComparison.OrdinalIgnoreCase) == true;
        if (isText)
        {
            var text = SafeDecodeUtf8(result.Content);
            ShowGeneratedPreviewDialog(selector, text);
        }
        else
        {
            SetStatus($"Generated {selector}: {result.Content.Length} bytes ({result.ContentType ?? "unknown"}).");
        }
    }

    private void ShowGeneratedPreviewDialog(string selector, string text)
    {
        Application.Invoke(() =>
        {
            var dlg = new Dialog { Title = $"Generated: {selector}", Width = 100, Height = 24 };
            var view = new TextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                ReadOnly = true,
                WordWrap = true,
                Text = text,
            };
            dlg.Add(view);
            var closeBtn = new Button { Text = "Close" };
            closeBtn.Accepting += (_, _) => Application.RequestStop();
            dlg.AddButton(closeBtn);
            Application.Run(dlg);
        });
    }

    private void ShowThreeFieldDialog(
        string title,
        string idText,
        string titleText,
        string bodyText,
        bool idReadOnly,
        Func<string, string, string, Task> saveAction)
    {
        var dlg = new Dialog { Title = title, Width = 92, Height = 20 };
        dlg.Add(new Label { X = 1, Y = 1, Text = "ID:" });
        var idField = new TextField { X = 12, Y = 1, Width = Dim.Fill(2), Text = idText, ReadOnly = idReadOnly };
        dlg.Add(idField);
        dlg.Add(new Label { X = 1, Y = 3, Text = "Title:" });
        var titleField = new TextField { X = 12, Y = 3, Width = Dim.Fill(2), Text = titleText };
        dlg.Add(titleField);
        dlg.Add(new Label { X = 1, Y = 5, Text = "Body:" });
        var bodyView = new TextView
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            WordWrap = true,
            Text = bodyText
        };
        dlg.Add(bodyView);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var id = idField.Text?.ToString()?.Trim() ?? string.Empty;
            var localTitle = titleField.Text?.ToString() ?? string.Empty;
            var body = bodyView.Text?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(localTitle))
            {
                SetStatus("ID and title are required.");
                return;
            }
            Application.RequestStop();
            _ = Task.Run(() => saveAction(id, localTitle, body));
        };
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void EnsureSelection(TableView table, int count)
    {
        if (count == 0)
            return;
        if (table.SelectedRow < 0 || table.SelectedRow >= count)
            table.SelectedRow = 0;
    }

    private static IReadOnlyList<string> ParseCsv(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();

    private static string SafeDecodeUtf8(byte[] content)
    {
        try
        {
            return Encoding.UTF8.GetString(content);
        }
        catch
        {
            return "(Unable to decode UTF-8 preview.)";
        }
    }

    private void SetStatus(string text)
        => Application.Invoke(() => _statusLabel.Text = text);

    private static void SetText(TextView view, string text)
        => Application.Invoke(() => view.Text = text);
}
