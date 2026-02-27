using McpServer.UI.Core.ViewModels;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Screens;

/// <summary>Terminal.Gui screen for TODO item management.</summary>
internal sealed class TodoScreen : View
{
    private readonly TodoListViewModel _listViewModel;
    private readonly TodoDetailViewModel _detailViewModel;
    private readonly DirectorMcpContext? _directorContext;
    private TableView _table = null!;
    private TextView _detailView = null!;
    private Label _detailTitleLabel = null!;
    private Label _doneValueLabel = null!;
    private Button _showCompletedToggleButton = null!;
    private TextView _statusLabel = null!;
    private TextField _sectionFilter = null!;
    private TextField _idField = null!;
    private TextField _titleField = null!;
    private TextField _editorSectionField = null!;
    private TextField _priorityField = null!;
    private TextField _estimateField = null!;
    private TextField _noteField = null!;
    private TextView _descriptionEditor = null!;
    private TextView _technicalDetailsEditor = null!;
    private TextView _implementationTasksEditor = null!;
    private List<TodoRow> _rows = [];
    private readonly SemaphoreSlim _detailLoadGate = new(1, 1);
    private int _detailLoadRequestVersion;
    private string? _lastAutoDetailTodoId;
    private bool _showCompletedItems;
    private SortField _currentSort = SortField.Priority;
    private bool _sortDescending;
    private Button _sortPriorityBtn = null!;
    private Button _sortNameBtn = null!;
    private readonly ILogger<TodoScreen> _logger;


    public TodoScreen(TodoListViewModel listViewModel, TodoDetailViewModel detailViewModel,
        ILogger<TodoScreen>? logger = null, DirectorMcpContext? directorContext = null)
    {
        _logger = logger ?? NullLogger<TodoScreen>.Instance;
        _listViewModel = listViewModel;
        _detailViewModel = detailViewModel;
        _directorContext = directorContext;
        Title = "TODO";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
        _detailViewModel.BeginNewDraft();
        SyncEditorFieldsFromViewModel();
    }

    private void BuildUi()
    {
        var filterLabel = new Label { X = 0, Y = 0, Text = "Section:" };
        _sectionFilter = new TextField { X = 10, Y = 0, Width = 30, Text = "" };
        Add(filterLabel, _sectionFilter);

        _showCompletedToggleButton = new Button { X = 43, Y = 0, Text = "Show Completed" };
        _showCompletedToggleButton.Accepting += (_, _) => ToggleShowCompletedItems();
        Add(_showCompletedToggleButton);

        _sortPriorityBtn = new Button { X = Pos.Right(_showCompletedToggleButton) + 2, Y = 0, Text = "▼ Priority" };
        _sortPriorityBtn.Accepting += (_, _) => ApplySort(SortField.Priority);
        Add(_sortPriorityBtn);

        _sortNameBtn = new Button { X = Pos.Right(_sortPriorityBtn) + 1, Y = 0, Text = "  ID" };
        _sortNameBtn.Accepting += (_, _) => ApplySort(SortField.Id);
        Add(_sortNameBtn);

        _table = new TableView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Percent(32),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _table.Style.ShowHeaders = true;
        _table.Style.ShowHorizontalHeaderOverline = true;
        _table.Style.ShowHorizontalHeaderUnderline = true;
        _table.Style.ShowVerticalCellLines = true;
        _table.Style.ShowVerticalHeaderLines = true;
        _table.Style.ExpandLastColumn = true;
        _table.Style.ColumnStyles[0] = new ColumnStyle
        {
            Alignment = Alignment.Center,
            MaxWidth = 10,
        };
        _table.Style.ColumnStyles[1] = new ColumnStyle
        {
            MaxWidth = 30,
        };
        _table.Style.ColumnStyles[2] = new ColumnStyle
        {
            MaxWidth = 200,
        };
        _table.SelectedCellChanged += (_, _) => QueueSelectedRowDetailRefresh();
        Add(_table);

        var row1Y = Pos.Bottom(_table);
        Add(new Label { X = 0, Y = row1Y, Text = "ID:" });
        _idField = new TextField { X = 4, Y = row1Y, Width = 28, Text = "", ReadOnly = true };
        Add(_idField);

        Add(new Label { X = 34, Y = row1Y, Text = "Title:" });
        _titleField = new TextField { X = 41, Y = row1Y, Width = Dim.Fill(), Text = "" };
        Add(_titleField);

        var row2Y = Pos.Bottom(_idField);
        Add(new Label { X = 0, Y = row2Y, Text = "Section:" });
        _editorSectionField = new TextField { X = 9, Y = row2Y, Width = 16, Text = "" };
        Add(_editorSectionField);

        Add(new Label { X = 27, Y = row2Y, Text = "Priority:" });
        _priorityField = new TextField { X = 37, Y = row2Y, Width = 10, Text = "" };
        Add(_priorityField);

        Add(new Label { X = 49, Y = row2Y, Text = "Estimate:" });
        _estimateField = new TextField { X = 59, Y = row2Y, Width = 16, Text = "" };
        Add(_estimateField);

        Add(new Label { X = 77, Y = row2Y, Text = "Done:" });
        _doneValueLabel = new Label { X = 83, Y = row2Y, Width = 6, Text = "false" };
        Add(_doneValueLabel);

        var toggleDoneBtn = new Button { X = 90, Y = row2Y, Text = "Toggle" };
        toggleDoneBtn.Accepting += (_, _) => ToggleDone();
        Add(toggleDoneBtn);

        var row3Y = Pos.Bottom(_editorSectionField);
        Add(new Label { X = 0, Y = row3Y, Text = "Note:" });
        _noteField = new TextField { X = 6, Y = row3Y, Width = Dim.Fill(), Text = "" };
        Add(_noteField);

        ApplyEditableScheme(_sectionFilter, _titleField, _editorSectionField,
            _priorityField, _estimateField, _noteField);

        var editorsY = Pos.Bottom(_noteField);
        var descFrame = new FrameView
        {
            Title = "Description",
            X = 0,
            Y = editorsY,
            Width = Dim.Fill(),
            Height = 4,
        };
        _descriptionEditor = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            WordWrap = true,
            Text = "",
        };
        descFrame.Add(_descriptionEditor);
        Add(descFrame);

        var techFrame = new FrameView
        {
            Title = "Technical Details",
            X = 0,
            Y = Pos.Bottom(descFrame),
            Width = Dim.Fill(),
            Height = 4,
        };
        _technicalDetailsEditor = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            WordWrap = true,
            Text = "",
        };
        techFrame.Add(_technicalDetailsEditor);
        Add(techFrame);

        var tasksFrame = new FrameView
        {
            Title = "Implementation Tasks",
            X = 0,
            Y = Pos.Bottom(techFrame),
            Width = Dim.Fill(),
            Height = 4,
        };
        _implementationTasksEditor = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            WordWrap = true,
            Text = "",
        };
        tasksFrame.Add(_implementationTasksEditor);
        Add(tasksFrame);

        ApplyEditableScheme(_descriptionEditor, _technicalDetailsEditor, _implementationTasksEditor);

        _detailTitleLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(tasksFrame),
            Width = Dim.Fill(),
            Text = "Detail: (select a row to load detail)",
        };
        Add(_detailTitleLabel);

        _detailView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(_detailTitleLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(5),
            ReadOnly = false,
            WordWrap = true,
            Text = "",
        };
        Add(_detailView);

        ApplyEditableScheme(_detailView);

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
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAsync);

        var detailBtn = new Button { X = Pos.Right(refreshBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Reload Detail" };
        detailBtn.Accepting += (_, _) => _ = Task.Run(LoadSelectedDetailAsync);

        var newBtn = new Button { X = Pos.Right(detailBtn) + 2, Y = Pos.AnchorEnd(1), Text = "New" };
        newBtn.Accepting += (_, _) => BeginNewDraft();

        var saveBtn = new Button { X = Pos.Right(newBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Save" };
        saveBtn.Accepting += (_, _) => _ = Task.Run(SaveEditorAsync);

        var deleteBtn = new Button { X = Pos.Right(saveBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteEditorAsync);

        var reqsBtn = new Button { X = Pos.Right(deleteBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Reqs" };
        reqsBtn.Accepting += (_, _) => _ = Task.Run(AnalyzeRequirementsAsync);

        var statusPromptBtn = new Button { X = Pos.Right(reqsBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Status" };
        statusPromptBtn.Accepting += (_, _) => ShowPromptResponseDialog("status", _detailViewModel.GenerateStatusPromptAsync);

        var implementPromptBtn = new Button { X = Pos.Right(statusPromptBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Implement" };
        implementPromptBtn.Accepting += (_, _) => ShowPromptResponseDialog("implement", _detailViewModel.GenerateImplementPromptAsync);

        var planPromptBtn = new Button { X = Pos.Right(implementPromptBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Plan" };
        planPromptBtn.Accepting += (_, _) => ShowPromptResponseDialog("plan", _detailViewModel.GeneratePlanPromptAsync);

        Add(refreshBtn, detailBtn, newBtn, saveBtn, deleteBtn, reqsBtn, statusPromptBtn, implementPromptBtn, planPromptBtn);
    }

    public async Task LoadAsync()
    {
        SetStatus("⏳ Loading TODO items...");
        try
        {
            var previouslySelectedTodoId = GetSelectedTodoId();
            _listViewModel.Section = _sectionFilter?.Text;
            await _listViewModel.LoadAsync().ConfigureAwait(false);

            var allItems = _listViewModel.Items.ToList();
            var visibleItems = _showCompletedItems
                ? allItems
                : allItems.Where(item => !item.Done).ToList();

            var rows = visibleItems
                .Select(item => new TodoRow(
                    item.Id,
                    item.Title,
                    item.Section,
                    item.Priority,
                    item.Done ? "✓" : "○"))
                .ToList();
            rows = SortRows(rows);
            _rows = rows;
            _lastAutoDetailTodoId = null;
            var selectedRow = rows.FindIndex(r => string.Equals(r.Id, previouslySelectedTodoId, StringComparison.Ordinal));
            if (selectedRow < 0 && rows.Count > 0)
                selectedRow = 0;

            Application.Invoke(() =>
            {
                var dt = new System.Data.DataTable();
                dt.Columns.Add("Pri", typeof(string));
                dt.Columns.Add("ID", typeof(string));
                dt.Columns.Add("Name", typeof(string));
                foreach (var r in rows)
                {
                    var pri = (r.Priority.Length > 8 ? r.Priority[..8] : r.Priority).PadRight(8);
                    var id = (r.Id.Length > 28 ? r.Id[..28] : r.Id).PadRight(28);
                    var name = r.Title.Length > 60 ? r.Title[..57] + "..." : r.Title;
                    dt.Rows.Add(pri, id, name);
                }

                _table.Table = new DataTableSource(dt);
                _table.SetNeedsDraw();

                if (selectedRow >= 0 && selectedRow < rows.Count)
                    _table.SelectedRow = selectedRow;
            });
            var hiddenCompletedCount = Math.Max(0, allItems.Count - rows.Count);
            SetStatus(_listViewModel.ErrorMessage is null
                ? BuildListStatus(rows.Count, allItems.Count, hiddenCompletedCount)
                : $"✗ {_listViewModel.ErrorMessage}");

            if (_listViewModel.ErrorMessage is null && rows.Count > 0)
            {
                await LoadTodoDetailAsync(rows[selectedRow].Id, autoLoaded: true).ConfigureAwait(false);
            }
            else if (rows.Count == 0)
            {
                ClearDetailPane(_showCompletedItems
                    ? "Detail: (no TODO items)"
                    : "Detail: (no open TODO items)");
                BeginNewDraft();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"✗ {ex.Message}");
        }
    }

    public async Task LoadSelectedDetailAsync()
    {
        var todoId = GetSelectedTodoId();
        if (string.IsNullOrWhiteSpace(todoId))
        {
            SetStatus("✗ Select a TODO row first.");
            return;
        }

        _lastAutoDetailTodoId = todoId;
        await LoadTodoDetailAsync(todoId, autoLoaded: false).ConfigureAwait(false);
    }

    public async Task SaveEditorAsync()
    {
        SyncEditorFieldsToViewModel();
        var targetId = _detailViewModel.EditorId;
        if (string.IsNullOrWhiteSpace(targetId))
        {
            SetStatus("✗ ID is required.");
            return;
        }

        await _detailViewModel.SaveAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_detailViewModel.ErrorMessage))
        {
            SetStatus($"✗ {_detailViewModel.ErrorMessage}");
            return;
        }

        SyncEditorFieldsFromViewModel();
        if (_detailViewModel.Detail is not null)
        {
            Application.Invoke(() =>
            {
                _detailTitleLabel.Text = $"Detail: {_detailViewModel.Detail.Id} - {_detailViewModel.Detail.Title}";
                _detailView.Text = FormatDetail(_detailViewModel.Detail);
            });
        }

        SetStatus(_detailViewModel.MutationMessage ?? "✓ TODO saved.");
        await LoadAsync().ConfigureAwait(false);

        // Re-load the edited item after list refresh so the detail pane stays on the target.
        var finalId = _detailViewModel.EditorId;
        if (!string.IsNullOrWhiteSpace(finalId))
            await LoadTodoDetailAsync(finalId, autoLoaded: false).ConfigureAwait(false);
    }

    public async Task DeleteEditorAsync()
    {
        SyncEditorFieldsToViewModel();
        var todoId = _detailViewModel.EditorId;
        if (string.IsNullOrWhiteSpace(todoId))
        {
            SetStatus("✗ ID is required to delete.");
            return;
        }

        var confirm = 1;
        Application.Invoke(() =>
            confirm = MessageBox.Query("Confirm Delete", $"Delete TODO '{todoId}'?", "Delete", "Cancel"));
        if (confirm != 0)
            return;

        await _detailViewModel.DeleteAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_detailViewModel.ErrorMessage))
        {
            SetStatus($"✗ {_detailViewModel.ErrorMessage}");
            return;
        }

        SyncEditorFieldsFromViewModel();
        ClearDetailPane("Detail: (deleted)");
        SetStatus(_detailViewModel.MutationMessage ?? "✓ TODO deleted.");
        await LoadAsync().ConfigureAwait(false);
    }

    public async Task AnalyzeRequirementsAsync()
    {
        SyncEditorFieldsToViewModel();
        var todoId = _detailViewModel.EditorId;
        if (string.IsNullOrWhiteSpace(todoId))
        {
            SetStatus("✗ Load or enter a TODO ID first.");
            return;
        }

        await _detailViewModel.AnalyzeRequirementsAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_detailViewModel.ErrorMessage))
        {
            SetStatus($"✗ {_detailViewModel.ErrorMessage}");
            return;
        }

        if (_detailViewModel.RequirementsAnalysis is null)
        {
            SetStatus("✗ Requirements analysis returned no result.");
            return;
        }

        // Refresh detail to show any server-updated FR/TR associations on the TODO item.
        await LoadTodoDetailAsync(todoId, autoLoaded: false).ConfigureAwait(false);
        ShowRequirementsInDetailPane(todoId, _detailViewModel.RequirementsAnalysis);
        SetStatus($"✓ Requirements analyzed for {todoId}");
    }

    public Task GenerateStatusPromptAsync() => GeneratePromptAsync(
        "status",
        _detailViewModel.GenerateStatusPromptAsync);

    public Task GenerateImplementPromptAsync() => GeneratePromptAsync(
        "implement",
        _detailViewModel.GenerateImplementPromptAsync);

    public Task GeneratePlanPromptAsync() => GeneratePromptAsync(
        "plan",
        _detailViewModel.GeneratePlanPromptAsync);

    private async Task LoadTodoDetailAsync(string todoId, bool autoLoaded)
    {
        var requestVersion = Interlocked.Increment(ref _detailLoadRequestVersion);
        await _detailLoadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            SetStatus(autoLoaded ? $"⏳ Loading detail for {todoId}..." : $"⏳ Loading selected detail ({todoId})...");
            _detailViewModel.TodoId = todoId;
            await _detailViewModel.LoadAsync().ConfigureAwait(false);

            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            if (_detailViewModel.ErrorMessage is not null)
            {
                Application.Invoke(() =>
                {
                    _detailTitleLabel.Text = $"Detail: {todoId}";
                    _detailView.Text = _detailViewModel.ErrorMessage;
                });
                SetStatus($"✗ {_detailViewModel.ErrorMessage}");
                return;
            }

            if (_detailViewModel.Detail is null)
            {
                ClearDetailPane($"Detail: {todoId} (not found)");
                SetStatus($"✗ TODO not found: {todoId}");
                return;
            }

            SyncEditorFieldsFromViewModel();

            var detail = _detailViewModel.Detail;
            Application.Invoke(() =>
            {
                _detailTitleLabel.Text = $"Detail: {detail.Id} - {detail.Title}";
                _detailView.Text = FormatDetail(detail);
            });

            SetStatus($"✓ Loaded detail for {detail.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            Application.Invoke(() =>
            {
                _detailTitleLabel.Text = $"Detail: {todoId}";
                _detailView.Text = ex.Message;
            });
            SetStatus($"✗ {ex.Message}");
        }
        finally
        {
            _detailLoadGate.Release();
        }
    }

    private void QueueSelectedRowDetailRefresh()
    {
        var todoId = GetSelectedTodoId();
        if (string.IsNullOrWhiteSpace(todoId))
            return;

        if (string.Equals(_lastAutoDetailTodoId, todoId, StringComparison.Ordinal))
            return;

        _lastAutoDetailTodoId = todoId;
        _ = Task.Run(() => LoadTodoDetailAsync(todoId, autoLoaded: true));
    }

    private async Task GeneratePromptAsync(string promptType, Func<CancellationToken, Task> generateAsync)
    {
        SyncEditorFieldsToViewModel();
        var todoId = _detailViewModel.EditorId;
        if (string.IsNullOrWhiteSpace(todoId))
        {
            SetStatus("✗ Load or enter a TODO ID first.");
            return;
        }

        try
        {
            SetStatus($"⏳ Generating {promptType} prompt for {todoId}...");
            await generateAsync(CancellationToken.None).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(_detailViewModel.ErrorMessage))
            {
                SetStatus($"✗ {_detailViewModel.ErrorMessage}");
                return;
            }

            var output = _detailViewModel.PromptOutput;
            if (output is null)
            {
                SetStatus($"✗ {promptType} prompt returned no output.");
                return;
            }

            ShowPromptInDetailPane(output);
            SetStatus($"✓ Generated {output.PromptType} prompt ({output.Lines.Count} lines)");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"✗ {ex.Message}");
        }
    }

    private void ShowPromptResponseDialog(string promptType, Func<CancellationToken, Task> fallbackGenerateAsync)
    {
        if (_directorContext is null)
        {
            _ = Task.Run(() => GeneratePromptAsync(promptType, fallbackGenerateAsync));
            return;
        }

        SyncEditorFieldsToViewModel();
        var todoId = _detailViewModel.EditorId;
        if (string.IsNullOrWhiteSpace(todoId))
        {
            SetStatus("✗ Load or enter a TODO ID first.");
            return;
        }

        var promptLabel = GetPromptDisplayName(promptType);
        var dialog = new Dialog
        {
            Title = $"Copilot {promptLabel}: {todoId}",
            Width = Math.Min(120, Math.Max(80, Application.Top?.Frame.Width - 4 ?? 96)),
            Height = Math.Min(30, Math.Max(12, Application.Top?.Frame.Height - 4 ?? 24)),
        };

        var outputView = new TextView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        dialog.Add(outputView);

        var statusView = new TextView
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(2),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = $"Preparing {promptLabel} prompt stream...",
        };
        dialog.Add(statusView);

        var closeButton = new Button { Text = "Close" };
        var cts = new CancellationTokenSource();
        McpServer.UI.Core.Messages.TodoPromptOutput? finalOutput = null;
        string? finalError = null;
        var dialogClosed = 0;

        closeButton.Accepting += (_, _) =>
        {
            cts.Cancel();
            Application.RequestStop();
        };
        dialog.AddButton(closeButton);

        dialog.Disposing += (_, _) =>
        {
            Interlocked.Exchange(ref dialogClosed, 1);
            cts.Cancel();
        };

        void SafeDialogUi(Action update)
        {
            if (Volatile.Read(ref dialogClosed) != 0)
                return;

            Application.Invoke(() =>
            {
                if (Volatile.Read(ref dialogClosed) != 0)
                    return;
                update();
            });
        }

        SetStatus($"⏳ Generating {promptType} prompt for {todoId}...");

        _ = Task.Run(async () =>
        {
            try
            {
                var lines = new List<string>();
                await foreach (var line in StreamPromptLinesAsync(todoId, promptType, cts.Token).WithCancellation(cts.Token).ConfigureAwait(false))
                {
                    lines.Add(line);
                    var currentText = string.Join(Environment.NewLine, lines);
                    var currentStatus = $"Streaming {promptLabel} prompt... {lines.Count} lines";
                    SafeDialogUi(() =>
                    {
                        outputView.Text = currentText;
                        statusView.Text = currentStatus;
                    });
                }

                var text = string.Join(Environment.NewLine, lines);
                finalOutput = new McpServer.UI.Core.Messages.TodoPromptOutput(
                    TodoId: todoId,
                    PromptType: promptType,
                    Lines: lines,
                    Text: text);

                SafeDialogUi(() =>
                {
                    outputView.Text = text;
                    statusView.Text = $"Completed {promptLabel} prompt ({lines.Count} lines).";
                });
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("{ExceptionDetail}", ex.ToString());
                SafeDialogUi(() => statusView.Text = "Canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError("{ExceptionDetail}", ex.ToString());
                finalError = ex.Message;
                SafeDialogUi(() => statusView.Text = $"Error: {ex.Message}");
            }
        });

        Application.Run(dialog);

        cts.Cancel();
        cts.Dispose();

        if (finalOutput is not null)
        {
            _detailViewModel.PromptOutput = finalOutput;
            ShowPromptInDetailPane(finalOutput);
            SetStatus($"✓ Generated {finalOutput.PromptType} prompt ({finalOutput.Lines.Count} lines)");
            return;
        }

        if (!string.IsNullOrWhiteSpace(finalError))
        {
            SetStatus($"✗ {finalError}");
            return;
        }

        SetStatus($"✗ {promptLabel} prompt canceled.");
    }

    private async IAsyncEnumerable<string> StreamPromptLinesAsync(
        string todoId,
        string promptType,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_directorContext is null)
            throw new InvalidOperationException("Prompt streaming requires a Director workspace context.");

        var client = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var stream = promptType switch
        {
            "status" => client.Todo.StreamStatusAsync(todoId, cancellationToken),
            "implement" => client.Todo.StreamImplementAsync(todoId, cancellationToken),
            "plan" => client.Todo.StreamPlanAsync(todoId, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown prompt type '{promptType}'."),
        };

        await foreach (var line in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            yield return line;
    }

    private static string GetPromptDisplayName(string promptType)
        => promptType switch
        {
            "status" => "Status",
            "implement" => "Implement",
            "plan" => "Plan",
            _ => promptType,
        };

    private void BeginNewDraft()
    {
        _detailViewModel.BeginNewDraft(defaultSection: string.IsNullOrWhiteSpace(_sectionFilter?.Text)
            ? null
            : _sectionFilter.Text);
        SyncEditorFieldsFromViewModel();
        ClearDetailPane("Detail: (new draft)");
        SetStatus("Editor set to new TODO draft.");
    }

    private void SyncEditorFieldsFromViewModel()
    {
        Application.Invoke(() =>
        {
            _idField.ReadOnly = !_detailViewModel.IsNewDraft;
            _idField.Text = _detailViewModel.EditorId ?? "";
            _titleField.Text = _detailViewModel.EditorTitle ?? "";
            _editorSectionField.Text = _detailViewModel.EditorSection ?? "";
            _priorityField.Text = _detailViewModel.EditorPriority ?? "";
            _estimateField.Text = _detailViewModel.EditorEstimate ?? "";
            _noteField.Text = _detailViewModel.EditorNote ?? "";
            _descriptionEditor.Text = _detailViewModel.EditorDescriptionText ?? "";
            _technicalDetailsEditor.Text = _detailViewModel.EditorTechnicalDetailsText ?? "";
            _implementationTasksEditor.Text = _detailViewModel.EditorImplementationTasksText ?? "";
            _doneValueLabel.Text = _detailViewModel.EditorDone ? "true" : "false";
        });
    }

    private void SyncEditorFieldsToViewModel()
    {
        _detailViewModel.EditorId = _idField.Text ?? "";
        _detailViewModel.EditorTitle = _titleField.Text ?? "";
        _detailViewModel.EditorSection = _editorSectionField.Text ?? "";
        _detailViewModel.EditorPriority = _priorityField.Text ?? "";
        _detailViewModel.EditorEstimate = _estimateField.Text;
        _detailViewModel.EditorNote = _noteField.Text;
        _detailViewModel.EditorDescriptionText = _descriptionEditor.Text?.ToString();
        _detailViewModel.EditorTechnicalDetailsText = _technicalDetailsEditor.Text?.ToString();
        _detailViewModel.EditorImplementationTasksText = _implementationTasksEditor.Text?.ToString();
        _detailViewModel.IsDirty = true;
    }

    private void ToggleDone()
    {
        _detailViewModel.EditorDone = !_detailViewModel.EditorDone;
        _detailViewModel.IsDirty = true;
        Application.Invoke(() => _doneValueLabel.Text = _detailViewModel.EditorDone ? "true" : "false");
        _ = Task.Run(SaveEditorAsync);
    }

    private void ToggleShowCompletedItems()
    {
        _showCompletedItems = !_showCompletedItems;
        UpdateShowCompletedToggleButtonText();
        _ = Task.Run(LoadAsync);
    }

    private void UpdateShowCompletedToggleButtonText()
    {
        Application.Invoke(() =>
        {
            _showCompletedToggleButton.Text = _showCompletedItems ? "Hide Completed" : "Show Completed";
        });
    }

    private void ApplySort(SortField field)
    {
        if (_currentSort == field)
            _sortDescending = !_sortDescending;
        else
        {
            _currentSort = field;
            _sortDescending = false;
        }

        UpdateSortButtonLabels();
        _ = Task.Run(LoadAsync);
    }

    private void UpdateSortButtonLabels()
    {
        var arrow = _sortDescending ? "▲" : "▼";
        Application.Invoke(() =>
        {
            _sortPriorityBtn.Text = _currentSort == SortField.Priority ? $"{arrow} Priority" : "  Priority";
            _sortNameBtn.Text = _currentSort == SortField.Id ? $"{arrow} ID" : "  ID";
        });
    }

    private List<TodoRow> SortRows(List<TodoRow> rows)
    {
        return _currentSort switch
        {
            SortField.Priority => (_sortDescending
                ? rows.OrderByDescending(r => PriorityRank(r.Priority))
                : rows.OrderBy(r => PriorityRank(r.Priority)))
                .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            SortField.Id => (_sortDescending
                ? rows.OrderByDescending(r => r.Id, StringComparer.OrdinalIgnoreCase)
                : rows.OrderBy(r => r.Id, StringComparer.OrdinalIgnoreCase))
                .ToList(),
            _ => rows,
        };
    }

    private static int PriorityRank(string priority) => priority.ToLowerInvariant() switch
    {
        "high" => 0,
        "medium" => 1,
        "low" => 2,
        _ => 3,
    };

    private string? GetSelectedTodoId()
    {
        var row = _table.SelectedRow;
        if (row >= 0 && row < _rows.Count)
            return _rows[row].Id;

        return _rows.Count > 0 ? _rows[0].Id : null;
    }

    private void ClearDetailPane(string title)
    {
        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = title;
            _detailView.Text = "";
        });
    }

    private void ShowRequirementsInDetailPane(string todoId, McpServer.UI.Core.Messages.TodoRequirementsAnalysis analysis)
    {
        var lines = new List<string>
        {
            $"TODO: {todoId}",
            $"Success: {analysis.Success}",
        };

        if (!string.IsNullOrWhiteSpace(analysis.Error))
            lines.Add($"Error: {analysis.Error}");

        if (analysis.FunctionalRequirements.Count > 0)
        {
            lines.Add("");
            lines.Add("Functional Requirements:");
            lines.AddRange(analysis.FunctionalRequirements.Select(v => $"  - {v}"));
        }

        if (analysis.TechnicalRequirements.Count > 0)
        {
            lines.Add("");
            lines.Add("Technical Requirements:");
            lines.AddRange(analysis.TechnicalRequirements.Select(v => $"  - {v}"));
        }

        if (!string.IsNullOrWhiteSpace(analysis.CopilotResponse))
        {
            lines.Add("");
            lines.Add("Copilot Response:");
            lines.Add(analysis.CopilotResponse);
        }

        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = $"Requirements: {todoId}";
            _detailView.Text = string.Join(Environment.NewLine, lines);
        });
    }

    private void ShowPromptInDetailPane(McpServer.UI.Core.Messages.TodoPromptOutput output)
    {
        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = $"Prompt ({output.PromptType}): {output.TodoId}";
            _detailView.Text = string.IsNullOrWhiteSpace(output.Text)
                ? "(no output)"
                : output.Text;
        });
    }

    private static string FormatDetail(McpServer.UI.Core.Messages.TodoDetail detail)
    {
        var lines = new List<string>
        {
            $"ID: {detail.Id}",
            $"Title: {detail.Title}",
            $"Section: {detail.Section}",
            $"Priority: {detail.Priority}",
            $"Done: {detail.Done}",
            $"Estimate: {detail.Estimate ?? ""}",
        };

        if (!string.IsNullOrWhiteSpace(detail.Note))
            lines.Add($"Note: {detail.Note}");
        if (!string.IsNullOrWhiteSpace(detail.Reference))
            lines.Add($"Reference: {detail.Reference}");
        if (!string.IsNullOrWhiteSpace(detail.CompletedDate))
            lines.Add($"Completed: {detail.CompletedDate}");
        if (!string.IsNullOrWhiteSpace(detail.DoneSummary))
            lines.Add($"Done Summary: {detail.DoneSummary}");
        if (!string.IsNullOrWhiteSpace(detail.Remaining))
            lines.Add($"Remaining: {detail.Remaining}");
        if (!string.IsNullOrWhiteSpace(detail.PriorityNote))
            lines.Add($"Priority Note: {detail.PriorityNote}");

        AppendSection(lines, "Description", detail.Description);
        AppendSection(lines, "Technical Details", detail.TechnicalDetails);
        AppendSection(lines, "Depends On", detail.DependsOn);
        AppendSection(lines, "Functional Requirements", detail.FunctionalRequirements);
        AppendSection(lines, "Technical Requirements", detail.TechnicalRequirements);

        if (detail.ImplementationTasks.Count > 0)
        {
            lines.Add("");
            lines.Add("Implementation Tasks:");
            lines.AddRange(detail.ImplementationTasks.Select(t => $"  {(t.Done ? "[x]" : "[ ]")} {t.Task}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendSection(List<string> lines, string title, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
            return;

        lines.Add("");
        lines.Add($"{title}:");
        lines.AddRange(values.Select(v => $"  - {v}"));
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);

    private string BuildListStatus(int visibleCount, int totalCount, int hiddenCompletedCount)
    {
        if (_showCompletedItems)
            return $"✓ {visibleCount} items (including completed)";

        return hiddenCompletedCount > 0
            ? $"✓ {visibleCount}/{totalCount} items (completed hidden: {hiddenCompletedCount})"
            : $"✓ {visibleCount} items";
    }

    private sealed record TodoRow(string Id, string Title, string Section, string Priority, string Done);

    private enum SortField { Priority, Id }

    private static void ApplyEditableScheme(params View[] views)
    {
        if (!Colors.ColorSchemes.TryGetValue("Editable", out var scheme))
            return;
        foreach (var v in views)
            v.ColorScheme = scheme;
    }
}
