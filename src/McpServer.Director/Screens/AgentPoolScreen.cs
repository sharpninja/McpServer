using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for pooled runtime operations.
/// This view is intentionally passive: all mutable state and API operations are delegated to <see cref="AgentPoolViewModel"/>.
/// </summary>
internal sealed class AgentPoolScreen : View
{
    private readonly AgentPoolViewModel _vm;
    private readonly ViewModelBinder _binder = new();
    private readonly List<AgentDefinitionSummaryItem> _configuredRows = [];
    private readonly List<AgentPoolRuntimeAgentSnapshot> _agentRows = [];
    private readonly List<AgentPoolQueueItemSnapshot> _queueRows = [];

    private Label _statusLabel = null!;
    private TableView _configuredTable = null!;
    private TableView _agentsTable = null!;
    private TableView _queueTable = null!;
    private TextField _agentNameField = null!;
    private TextField _promptField = null!;

    /// <summary>Initializes a new instance of the <see cref="AgentPoolScreen"/> class.</summary>
    public AgentPoolScreen(AgentPoolViewModel vm)
    {
        _vm = vm;
        Title = "Agent Pool";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    /// <summary>Triggers initial/refresh load.</summary>
    public Task LoadAsync()
        => _vm.LoadAsync();

    private void BuildUi()
    {
        _statusLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Text = "Agent Pool",
        };
        Add(_statusLabel);

        var errorField = new TextField
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = "",
            ReadOnly = true,
            Visible = false,
        };
        var errorColorScheme = Colors.ColorSchemes.TryGetValue("Error", out var errScheme) ? errScheme : null;
        if (errorColorScheme is not null)
            errorField.ColorScheme = errorColorScheme;
        Add(errorField);

        var configuredLabel = new Label { X = 0, Y = 2, Text = "Configured Agents" };
        var newAgentBtn = new Button { X = Pos.Right(configuredLabel) + 2, Y = 2, Text = "New Agent" };
        newAgentBtn.Accepting += (_, _) => ShowNewAgentDialog();
        Add(configuredLabel, newAgentBtn);

        _configuredTable = new TableView
        {
            X = 0,
            Y = 3,
            Width = Dim.Fill(),
            Height = 5,
            FullRowSelect = true,
            MultiSelect = false,
        };
        _configuredTable.KeyDown += (_, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                OpenConfiguredAgentDialogForRow(_configuredTable.SelectedRow);
                e.Handled = true;
            }
        };
        _configuredTable.MouseClick += (_, _) => OpenConfiguredAgentDialogForRow(_configuredTable.SelectedRow);
        _configuredTable.SelectedCellChanged += (_, e) => _vm.SelectedConfiguredIndex = e.NewRow;
        Add(_configuredTable);

        var agentsLabel = new Label { X = 0, Y = Pos.Bottom(_configuredTable), Text = "Runtime Agents" };
        Add(agentsLabel);

        _agentsTable = new TableView
        {
            X = 0,
            Y = Pos.Bottom(agentsLabel),
            Width = Dim.Fill(),
            Height = Dim.Percent(27),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _agentsTable.SelectedCellChanged += (_, e) => _vm.SelectedRuntimeIndex = e.NewRow;
        Add(_agentsTable);

        var queueLabel = new Label { X = 0, Y = Pos.Bottom(_agentsTable), Text = "Queue" };
        Add(queueLabel);

        _queueTable = new TableView
        {
            X = 0,
            Y = Pos.Bottom(queueLabel),
            Width = Dim.Fill(),
            Height = Dim.Percent(27),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _queueTable.SelectedCellChanged += (_, e) => _vm.SelectedQueueIndex = e.NewRow;
        Add(_queueTable);

        var agentLabel = new Label { X = 0, Y = Pos.Bottom(_queueTable), Text = "Agent:" };
        _agentNameField = new TextField
        {
            X = Pos.Right(agentLabel) + 1,
            Y = Pos.Bottom(_queueTable),
            Width = 22,
            Text = "",
        };
        _agentNameField.TextChanged += (_, _) => _vm.AgentNameInput = _agentNameField.Text?.ToString();
        Add(agentLabel, _agentNameField);

        var promptLabel = new Label { X = Pos.Right(_agentNameField) + 2, Y = Pos.Bottom(_queueTable), Text = "Ad-hoc Prompt:" };
        _promptField = new TextField
        {
            X = Pos.Right(promptLabel) + 1,
            Y = Pos.Bottom(_queueTable),
            Width = Dim.Fill(1),
            Text = "",
        };
        _promptField.TextChanged += (_, _) => _vm.PromptInput = _promptField.Text?.ToString();
        Add(promptLabel, _promptField);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.LoadAsync());

        var startBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Start" };
        startBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.StartSelectedAsync());

        var stopBtn = new Button { X = Pos.Right(startBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Stop" };
        stopBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.StopSelectedAsync());

        var recycleBtn = new Button { X = Pos.Right(stopBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Recycle" };
        recycleBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.RecycleSelectedAsync());

        var connectBtn = new Button { X = Pos.Right(recycleBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Connect" };
        connectBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.ConnectSelectedAsync());

        var cancelBtn = new Button { X = Pos.Right(connectBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Cancel Job" };
        cancelBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.CancelSelectedJobAsync());

        var removeBtn = new Button { X = Pos.Right(cancelBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Remove Job" };
        removeBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.RemoveSelectedJobAsync());

        var upBtn = new Button { X = Pos.Right(removeBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Move Up" };
        upBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.MoveSelectedJobUpAsync());

        var downBtn = new Button { X = Pos.Right(upBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Move Down" };
        downBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.MoveSelectedJobDownAsync());

        var enqueueBtn = new Button { X = Pos.Right(downBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Enqueue" };
        enqueueBtn.Accepting += (_, _) => _ = Task.Run(() => _vm.EnqueueAdHocAsync());

        Add(refreshBtn, startBtn, stopBtn, recycleBtn, connectBtn, cancelBtn, removeBtn, upBtn, downBtn, enqueueBtn);

        // Bindings
        _binder.BindProperty(_vm, nameof(_vm.StatusMessage), () =>
        {
            _statusLabel.Text = _vm.StatusMessage ?? "Agent Pool";
        });

        _binder.BindProperty(_vm, nameof(_vm.ErrorMessage), () =>
        {
            errorField.Visible = !string.IsNullOrWhiteSpace(_vm.ErrorMessage);
            errorField.Text = _vm.ErrorMessage ?? "";
        });

        _binder.BindProperty(_vm, nameof(_vm.IsLoading), () =>
        {
            refreshBtn.Enabled = !_vm.IsLoading;
        });

        _binder.BindProperty(_vm, nameof(_vm.AgentNameInput), () =>
        {
            SyncTextField(_agentNameField, _vm.AgentNameInput);
        });

        _binder.BindProperty(_vm, nameof(_vm.PromptInput), () =>
        {
            SyncTextField(_promptField, _vm.PromptInput);
        });

        _binder.BindCollection(_vm.ConfiguredAgents, _configuredTable, items =>
        {
            _configuredRows.Clear();
            _configuredRows.AddRange(items);
            return new EnumerableTableSource<AgentDefinitionSummaryItem>(
                items,
                new Dictionary<string, Func<AgentDefinitionSummaryItem, object>>
                {
                    ["Id"] = x => Truncate(x.Id, 24),
                    ["Display"] = x => Truncate(x.DisplayName, 34),
                    ["Built-In"] = x => x.IsBuiltIn ? "yes" : "no",
                });
        });

        _binder.BindCollection(_vm.RuntimeAgents, _agentsTable, items =>
        {
            _agentRows.Clear();
            _agentRows.AddRange(items);
            return new EnumerableTableSource<AgentPoolRuntimeAgentSnapshot>(
                items,
                new Dictionary<string, Func<AgentPoolRuntimeAgentSnapshot, object>>
                {
                    ["Agent"] = x => Truncate(x.AgentName, 18),
                    ["Lifecycle"] = x => Truncate(x.Lifecycle, 10),
                    ["Workspace"] = x => Truncate(x.WorkspacePath ?? "", 24),
                    ["Session"] = x => Truncate(x.SessionId ?? "", 18),
                    ["Job"] = x => Truncate(x.ActiveJobId ?? "", 16),
                    ["Links"] = x => x.ActiveVoiceLinks,
                });
        });

        _binder.BindCollection(_vm.QueueItems, _queueTable, items =>
        {
            _queueRows.Clear();
            _queueRows.AddRange(items);
            return new EnumerableTableSource<AgentPoolQueueItemSnapshot>(
                items,
                new Dictionary<string, Func<AgentPoolQueueItemSnapshot, object>>
                {
                    ["Job"] = x => Truncate(x.JobId, 20),
                    ["Agent"] = x => Truncate(x.AgentName ?? "", 14),
                    ["Workspace"] = x => Truncate(x.WorkspacePath ?? "", 24),
                    ["Status"] = x => Truncate(x.Status, 10),
                    ["Context"] = x => x.Context ?? "",
                    ["Prompt"] = x => Truncate(x.RenderedPrompt ?? "", 28),
                });
        });
    }

    private void OpenConfiguredAgentDialogForRow(int row)
    {
        if (row < 0 || row >= _configuredRows.Count)
            return;

        ShowNewAgentDialog(_configuredRows[row].Id);
    }

    private void ShowNewAgentDialog(string? initialAgentId = null)
    {
        var definitionRows = _configuredRows.ToList();

        var dialog = new Dialog
        {
            Title = "New / Edit Agent",
            Width = 98,
            Height = 22,
        };

        var nameLabel = new Label { X = 1, Y = 1, Text = "Agent Name:" };
        var initialName = (initialAgentId ?? _vm.AgentNameInput ?? string.Empty).Trim();
        var nameField = new TextField { X = 22, Y = 1, Width = 74, Text = initialName };

        var pathLabel = new Label { X = 1, Y = 2, Text = "Agent Path:" };
        var pathField = new TextField { X = 22, Y = 2, Width = 74, Text = "copilot" };

        var modelLabel = new Label { X = 1, Y = 3, Text = "Agent Model:" };
        var modelField = new TextField { X = 22, Y = 3, Width = 74, Text = "gpt-5.3-codex" };

        var seedLabel = new Label { X = 1, Y = 4, Text = "Agent Seed:" };
        var seedField = new TextField { X = 22, Y = 4, Width = 74, Text = string.Empty };

        var configsLabel = new Label { X = 1, Y = 6, Text = "Agent Configurations:" };
        var configsTable = new TableView
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill(2),
            Height = 8,
            FullRowSelect = true,
            MultiSelect = false,
        };
        if (definitionRows.Count > 0)
        {
            configsTable.Table = new EnumerableTableSource<AgentDefinitionSummaryItem>(
                definitionRows,
                new Dictionary<string, Func<AgentDefinitionSummaryItem, object>>
                {
                    ["Id"] = x => Truncate(x.Id, 24),
                    ["Display"] = x => Truncate(x.DisplayName, 34),
                    ["Built-In"] = x => x.IsBuiltIn ? "yes" : "no",
                });
        }

        void LoadDefinitionIntoEditors(string agentId)
        {
            _ = Task.Run(async () =>
            {
                var detail = await _vm.GetDefinitionAsync(agentId).ConfigureAwait(false);
                if (detail is null)
                    return;

                Application.Invoke(() =>
                {
                    nameField.Text = detail.Id;
                    pathField.Text = detail.DefaultLaunchCommand;
                    modelField.Text = detail.DefaultModels.FirstOrDefault() ?? string.Empty;
                    seedField.Text = detail.DefaultSeedPrompt;
                });
            });
        }

        configsTable.SelectedCellChanged += (_, e) =>
        {
            var row = e.NewRow;
            if (row < 0 || row >= definitionRows.Count)
                return;
            LoadDefinitionIntoEditors(definitionRows[row].Id);
        };

        var selectedConfigIndex = definitionRows.FindIndex(x => string.Equals(x.Id, initialName, StringComparison.OrdinalIgnoreCase));
        if (selectedConfigIndex >= 0)
        {
            configsTable.SelectedRow = selectedConfigIndex;
            LoadDefinitionIntoEditors(definitionRows[selectedConfigIndex].Id);
        }

        var configsHelpLabel = new Label
        {
            X = 1,
            Y = 16,
            Text = "Select a configuration row to load its defaults into the editor fields.",
        };

        var createBtn = new Button { Text = "Save + Start" };
        createBtn.Accepting += (_, _) =>
        {
            var agentName = (nameField.Text?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(agentName))
            {
                _vm.StatusMessage = "Enter an agent name first.";
                return;
            }

            var agentPath = (pathField.Text?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(agentPath))
            {
                _vm.StatusMessage = "Enter an agent path first.";
                return;
            }

            var agentModel = (modelField.Text?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(agentModel))
            {
                _vm.StatusMessage = "Enter an agent model first.";
                return;
            }

            Application.RequestStop();
            _ = Task.Run(() => _vm.CreateOrUpdateAgentAndStartAsync(
                agentName,
                agentPath,
                agentModel,
                NullIfWhiteSpace(seedField.Text?.ToString())));
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();

        dialog.Add(
            nameLabel,
            nameField,
            pathLabel,
            pathField,
            modelLabel,
            modelField,
            seedLabel,
            seedField,
            configsLabel,
            configsTable,
            configsHelpLabel);
        dialog.AddButton(createBtn);
        dialog.AddButton(cancelBtn);
        Application.Run(dialog);
    }

    private static void SyncTextField(TextField textField, string? value)
    {
        var next = value ?? string.Empty;
        var current = textField.Text?.ToString() ?? string.Empty;
        if (!string.Equals(current, next, StringComparison.Ordinal))
            textField.Text = next;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string value, int max)
    {
        if (value.Length <= max)
            return value;
        if (max <= 3)
            return value[..max];
        return value[..(max - 3)] + "...";
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _binder.Dispose();
        base.Dispose(disposing);
    }
}
