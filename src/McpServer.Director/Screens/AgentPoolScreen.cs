using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Director.Handlers;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for pooled runtime operations: agent lifecycle, one-shot queue management,
/// and ad-hoc enqueue requests.
/// </summary>
internal sealed class AgentPoolScreen : View
{
    private readonly DirectorMcpContext _directorContext;
    private readonly AgentScreenHandler _agentHandler;
    private readonly List<AgentDefinitionSummary> _configuredRows = [];
    private readonly List<AgentPoolAgentStatus> _agentRows = [];
    private readonly List<AgentPoolQueueItem> _queueRows = [];
    private bool _suppressConfiguredSelectionDialog;
    private bool _configuredSelectionArmed;
    private bool _openingConfiguredDialog;

    private Label _statusLabel = null!;
    private TableView _configuredTable = null!;
    private TableView _agentsTable = null!;
    private TableView _queueTable = null!;
    private TextField _agentNameField = null!;
    private TextField _promptField = null!;

    public AgentPoolScreen(DirectorMcpContext directorContext)
    {
        _directorContext = directorContext ?? throw new ArgumentNullException(nameof(directorContext));
        _agentHandler = new AgentScreenHandler(_directorContext);
        Title = "Agent Pool";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    public async Task LoadAsync()
    {
        SetStatus("Loading agent pool...");
        try
        {
            var api = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync().ConfigureAwait(false);
            var definitions = await _agentHandler.ListDefinitionsAsync().ConfigureAwait(false);
            var agents = await api.AgentPool.GetAgentsAsync().ConfigureAwait(false);
            var queue = await api.AgentPool.GetQueueAsync().ConfigureAwait(false);

            Application.Invoke(() =>
            {
                _suppressConfiguredSelectionDialog = true;
                _configuredSelectionArmed = false;
                try
                {
                    _configuredRows.Clear();
                    _configuredRows.AddRange(definitions);
                    _configuredTable.Table = new EnumerableTableSource<AgentDefinitionSummary>(
                        _configuredRows,
                        new Dictionary<string, Func<AgentDefinitionSummary, object>>
                        {
                            ["Id"] = x => Truncate(x.Id, 24),
                            ["Display"] = x => Truncate(x.DisplayName, 34),
                            ["Built-In"] = x => x.IsBuiltIn ? "yes" : "no",
                        });

                    _agentRows.Clear();
                    _agentRows.AddRange(agents);
                    _queueRows.Clear();
                    _queueRows.AddRange(queue);

                    _agentsTable.Table = new EnumerableTableSource<AgentPoolAgentStatus>(
                        _agentRows,
                        new Dictionary<string, Func<AgentPoolAgentStatus, object>>
                        {
                            ["Agent"] = x => Truncate(x.AgentName, 18),
                            ["Lifecycle"] = x => Truncate(x.Lifecycle, 10),
                            ["Session"] = x => Truncate(x.SessionId ?? "", 18),
                            ["Job"] = x => Truncate(x.ActiveJobId ?? "", 16),
                            ["Links"] = x => x.ActiveVoiceLinks,
                        });

                    _queueTable.Table = new EnumerableTableSource<AgentPoolQueueItem>(
                        _queueRows,
                        new Dictionary<string, Func<AgentPoolQueueItem, object>>
                        {
                            ["Job"] = x => Truncate(x.JobId, 20),
                            ["Agent"] = x => Truncate(x.AgentName ?? "", 14),
                            ["Status"] = x => Truncate(x.Status, 10),
                            ["Context"] = x => x.Context?.ToString() ?? "",
                            ["Prompt"] = x => Truncate(x.RenderedPrompt ?? "", 28),
                        });

                    _statusLabel.Text = $"Agent pool loaded ({_configuredRows.Count} configs, {_agentRows.Count} agents, {_queueRows.Count} jobs)";
                }
                finally
                {
                    _suppressConfiguredSelectionDialog = false;
                }
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Load failed: {ex.Message}");
        }
    }

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

        var configuredLabel = new Label { X = 0, Y = 1, Text = "Configured Agents" };
        var newAgentBtn = new Button { X = Pos.Right(configuredLabel) + 2, Y = 1, Text = "New Agent" };
        newAgentBtn.Accepting += (_, _) => ShowNewAgentDialog();
        Add(configuredLabel, newAgentBtn);

        _configuredTable = new TableView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = 5,
            FullRowSelect = true,
            MultiSelect = false,
        };
        _configuredTable.KeyDown += (_, _) => _configuredSelectionArmed = true;
        _configuredTable.MouseClick += (_, _) =>
        {
            _configuredSelectionArmed = true;
            OpenConfiguredAgentDialogForRow(_configuredTable.SelectedRow);
        };
        _configuredTable.SelectedCellChanged += (_, e) =>
        {
            if (_configuredSelectionArmed)
                OpenConfiguredAgentDialogForRow(e.NewRow);
        };
        Add(_configuredTable);

        var agentsLabel = new Label { X = 0, Y = Pos.Bottom(_configuredTable), Text = "Runtime Agents" };
        Add(agentsLabel);

        _agentsTable = new TableView
        {
            X = 0,
            Y = Pos.Bottom(agentsLabel),
            Width = Dim.Fill(),
            Height = Dim.Percent(28),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _agentsTable.SelectedCellChanged += (_, _) => SyncAgentNameFromSelection();
        Add(_agentsTable);

        var queueLabel = new Label { X = 0, Y = Pos.Bottom(_agentsTable), Text = "Queue" };
        Add(queueLabel);

        _queueTable = new TableView
        {
            X = 0,
            Y = Pos.Bottom(queueLabel),
            Width = Dim.Fill(),
            Height = Dim.Percent(28),
            FullRowSelect = true,
            MultiSelect = false,
        };
        Add(_queueTable);

        var agentLabel = new Label { X = 0, Y = Pos.Bottom(_queueTable), Text = "Agent:" };
        _agentNameField = new TextField
        {
            X = Pos.Right(agentLabel) + 1,
            Y = Pos.Bottom(_queueTable),
            Width = 22,
            Text = "",
        };
        Add(agentLabel, _agentNameField);

        var promptLabel = new Label { X = Pos.Right(_agentNameField) + 2, Y = Pos.Bottom(_queueTable), Text = "Ad-hoc Prompt:" };
        _promptField = new TextField
        {
            X = Pos.Right(promptLabel) + 1,
            Y = Pos.Bottom(_queueTable),
            Width = Dim.Fill(1),
            Text = "",
        };
        Add(promptLabel, _promptField);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAsync);

        var startBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Start" };
        startBtn.Accepting += (_, _) => _ = Task.Run(StartSelectedAsync);

        var stopBtn = new Button { X = Pos.Right(startBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Stop" };
        stopBtn.Accepting += (_, _) => _ = Task.Run(StopSelectedAsync);

        var recycleBtn = new Button { X = Pos.Right(stopBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Recycle" };
        recycleBtn.Accepting += (_, _) => _ = Task.Run(RecycleSelectedAsync);

        var connectBtn = new Button { X = Pos.Right(recycleBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Connect" };
        connectBtn.Accepting += (_, _) => _ = Task.Run(ConnectSelectedAsync);

        var cancelBtn = new Button { X = Pos.Right(connectBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Cancel Job" };
        cancelBtn.Accepting += (_, _) => _ = Task.Run(CancelSelectedJobAsync);

        var removeBtn = new Button { X = Pos.Right(cancelBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Remove Job" };
        removeBtn.Accepting += (_, _) => _ = Task.Run(RemoveSelectedJobAsync);

        var upBtn = new Button { X = Pos.Right(removeBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Move Up" };
        upBtn.Accepting += (_, _) => _ = Task.Run(MoveSelectedJobUpAsync);

        var downBtn = new Button { X = Pos.Right(upBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Move Down" };
        downBtn.Accepting += (_, _) => _ = Task.Run(MoveSelectedJobDownAsync);

        var enqueueBtn = new Button { X = Pos.Right(downBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Enqueue" };
        enqueueBtn.Accepting += (_, _) => _ = Task.Run(EnqueueAdHocAsync);

        Add(refreshBtn, startBtn, stopBtn, recycleBtn, connectBtn, cancelBtn, removeBtn, upBtn, downBtn, enqueueBtn);
    }

    private void OpenConfiguredAgentDialogForRow(int row)
    {
        if (_suppressConfiguredSelectionDialog || _openingConfiguredDialog || row < 0 || row >= _configuredRows.Count)
            return;

        _openingConfiguredDialog = true;
        try
        {
            _configuredSelectionArmed = false;
            ShowNewAgentDialog(_configuredRows[row].Id);
        }
        finally
        {
            _openingConfiguredDialog = false;
        }
    }

    private void SyncAgentNameFromSelection()
    {
        var selected = GetSelectedAgent();
        if (selected is null)
            return;

        Application.Invoke(() => _agentNameField.Text = selected.AgentName);
    }

    private void ShowNewAgentDialog(string? initialAgentId = null)
    {
        var definitionRows = new List<AgentDefinitionSummary>();
        try
        {
            var definitions = _agentHandler.ListDefinitionsAsync().GetAwaiter().GetResult();
            definitionRows.AddRange(definitions);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not load agent configs: {ex.Message}");
        }

        var dialog = new Dialog
        {
            Title = "New / Edit Agent",
            Width = 100,
            Height = 24,
        };

        var nameLabel = new Label { X = 1, Y = 1, Text = "Agent Name:" };
        var initialName = (initialAgentId ?? _agentNameField.Text?.ToString() ?? string.Empty).Trim();
        var nameField = new TextField { X = 22, Y = 1, Width = 76, Text = initialName };

        var pathLabel = new Label { X = 1, Y = 2, Text = "Agent Path:" };
        var pathField = new TextField { X = 22, Y = 2, Width = 76, Text = "copilot" };

        var modelLabel = new Label { X = 1, Y = 3, Text = "Agent Model:" };
        var modelField = new TextField { X = 22, Y = 3, Width = 76, Text = "gpt-5.3-codex" };

        var seedLabel = new Label { X = 1, Y = 4, Text = "Agent Seed:" };
        var seedField = new TextField { X = 22, Y = 4, Width = 76, Text = string.Empty };

        var paramsLabel = new Label { X = 1, Y = 5, Text = "Agent Parameters:" };
        var paramsField = new TextView
        {
            X = 22,
            Y = 5,
            Width = 76,
            Height = 4,
            Text = string.Empty,
            WordWrap = false,
        };
        var paramsHelpLabel = new Label { X = 22, Y = 9, Text = "Format: key=value (one per line or ';' separated)" };

        var interactiveDefault = new CheckBox
        {
            X = 22,
            Y = 10,
            Width = 34,
            Text = "Interactive Default",
            CheckedState = CheckState.UnChecked,
        };
        var planDefault = new CheckBox
        {
            X = 58,
            Y = 10,
            Width = 34,
            Text = "Plan Default",
            CheckedState = CheckState.UnChecked,
        };
        var statusDefault = new CheckBox
        {
            X = 22,
            Y = 11,
            Width = 34,
            Text = "Status Default",
            CheckedState = CheckState.UnChecked,
        };
        var implementDefault = new CheckBox
        {
            X = 58,
            Y = 11,
            Width = 34,
            Text = "Implement Default",
            CheckedState = CheckState.UnChecked,
        };

        var configsLabel = new Label { X = 1, Y = 13, Text = "Agent Configurations:" };
        var configsTable = new TableView
        {
            X = 1,
            Y = 14,
            Width = Dim.Fill(2),
            Height = 5,
            FullRowSelect = true,
            MultiSelect = false,
        };
        if (definitionRows.Count > 0)
        {
            configsTable.Table = new EnumerableTableSource<AgentDefinitionSummary>(
                definitionRows,
                new Dictionary<string, Func<AgentDefinitionSummary, object>>
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
                try
                {
                    var detail = await _agentHandler.GetDefinitionDetailAsync(agentId).ConfigureAwait(false);
                    Application.Invoke(() =>
                    {
                        nameField.Text = detail.AgentId;
                        pathField.Text = detail.DefaultLaunchCommand;
                        modelField.Text = detail.DefaultModels is { Count: > 0 } ? detail.DefaultModels[0] : string.Empty;
                        seedField.Text = detail.DefaultSeedPrompt ?? string.Empty;
                    });
                    SetStatus($"Loaded config '{agentId}' for editing.");
                }
                catch (Exception ex)
                {
                    SetStatus($"Load config failed: {ex.Message}");
                }
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
            Y = 19,
            Text = "Select a configuration row to load it into the editor fields.",
        };

        var createBtn = new Button { Text = "Save + Start" };
        createBtn.Accepting += (_, _) =>
        {
            var agentName = (nameField.Text?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(agentName))
            {
                SetStatus("Enter an agent name first.");
                return;
            }

            var agentPath = (pathField.Text?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(agentPath))
            {
                SetStatus("Enter an agent path first.");
                return;
            }

            var agentModel = (modelField.Text?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(agentModel))
            {
                SetStatus("Enter an agent model first.");
                return;
            }

            if (!TryParseAgentParameters(paramsField.Text?.ToString(), out var agentParameters, out var parameterError))
            {
                SetStatus(parameterError ?? "Invalid agent parameters.");
                return;
            }

            Application.RequestStop();
            _ = Task.Run(() => CreateAgentForPoolAsync(new NewAgentDialogValues(
                agentName,
                agentPath,
                agentModel,
                NullIfWhiteSpace(seedField.Text?.ToString()),
                agentParameters,
                IsChecked(interactiveDefault),
                IsChecked(planDefault),
                IsChecked(statusDefault),
                IsChecked(implementDefault))));
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
            paramsLabel,
            paramsField,
            paramsHelpLabel,
            interactiveDefault,
            planDefault,
            statusDefault,
            implementDefault,
            configsLabel,
            configsTable,
            configsHelpLabel);
        dialog.AddButton(createBtn);
        dialog.AddButton(cancelBtn);
        Application.Run(dialog);
    }

    private AgentPoolAgentStatus? GetSelectedAgent()
    {
        var row = _agentsTable.SelectedRow;
        return row >= 0 && row < _agentRows.Count ? _agentRows[row] : null;
    }

    private AgentPoolQueueItem? GetSelectedQueueItem()
    {
        var row = _queueTable.SelectedRow;
        return row >= 0 && row < _queueRows.Count ? _queueRows[row] : null;
    }

    private async Task StartSelectedAsync()
    {
        var agentName = GetAgentNameOrNull();
        if (agentName is null)
        {
            SetStatus("Select an agent row first.");
            return;
        }

        await RunMutationAsync(
            $"Starting '{agentName}'...",
            x => x.StartAgentAsync(agentName),
            $"Agent '{agentName}' started.").ConfigureAwait(false);
    }

    private async Task StopSelectedAsync()
    {
        var agentName = GetAgentNameOrNull();
        if (agentName is null)
        {
            SetStatus("Select an agent row first.");
            return;
        }

        await RunMutationAsync(
            $"Stopping '{agentName}'...",
            x => x.StopAgentAsync(agentName),
            $"Agent '{agentName}' stopped.").ConfigureAwait(false);
    }

    private async Task RecycleSelectedAsync()
    {
        var agentName = GetAgentNameOrNull();
        if (agentName is null)
        {
            SetStatus("Select an agent row first.");
            return;
        }

        await RunMutationAsync(
            $"Recycling '{agentName}'...",
            x => x.RecycleAgentAsync(agentName),
            $"Agent '{agentName}' recycled.").ConfigureAwait(false);
    }

    private async Task ConnectSelectedAsync()
    {
        var agentName = GetAgentNameOrNull();
        if (agentName is null)
        {
            SetStatus("Select an agent row first.");
            return;
        }

        SetStatus($"Connecting to '{agentName}'...");
        try
        {
            var api = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync().ConfigureAwait(false);
            var result = await api.AgentPool.ConnectAsync(agentName).ConfigureAwait(false);
            if (!result.Success)
            {
                SetStatus(result.Error ?? "Connect failed.");
                return;
            }

            await LoadAsync().ConfigureAwait(false);
            SetStatus($"Connected '{agentName}' (session {result.SessionId ?? "n/a"}).");
        }
        catch (Exception ex)
        {
            SetStatus($"Connect failed: {ex.Message}");
        }
    }

    private async Task CancelSelectedJobAsync()
    {
        var job = GetSelectedQueueItem();
        if (job is null)
        {
            SetStatus("Select a queue item first.");
            return;
        }

        await RunMutationAsync(
            $"Canceling '{job.JobId}'...",
            x => x.CancelQueueItemAsync(job.JobId),
            $"Queue item '{job.JobId}' canceled.").ConfigureAwait(false);
    }

    private async Task RemoveSelectedJobAsync()
    {
        var job = GetSelectedQueueItem();
        if (job is null)
        {
            SetStatus("Select a queue item first.");
            return;
        }

        await RunMutationAsync(
            $"Removing '{job.JobId}'...",
            x => x.RemoveQueueItemAsync(job.JobId),
            $"Queue item '{job.JobId}' removed.").ConfigureAwait(false);
    }

    private async Task MoveSelectedJobUpAsync()
    {
        var job = GetSelectedQueueItem();
        if (job is null)
        {
            SetStatus("Select a queue item first.");
            return;
        }

        await RunMutationAsync(
            $"Moving '{job.JobId}' up...",
            x => x.MoveQueueItemUpAsync(job.JobId),
            $"Queue item '{job.JobId}' moved up.").ConfigureAwait(false);
    }

    private async Task MoveSelectedJobDownAsync()
    {
        var job = GetSelectedQueueItem();
        if (job is null)
        {
            SetStatus("Select a queue item first.");
            return;
        }

        await RunMutationAsync(
            $"Moving '{job.JobId}' down...",
            x => x.MoveQueueItemDownAsync(job.JobId),
            $"Queue item '{job.JobId}' moved down.").ConfigureAwait(false);
    }

    private async Task EnqueueAdHocAsync()
    {
        var prompt = (_promptField.Text?.ToString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            SetStatus("Enter an ad-hoc prompt first.");
            return;
        }

        var request = new AgentPoolOneShotRequest
        {
            AgentName = GetAgentNameOrNull(),
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = prompt,
            UseWorkspaceContext = true,
        };

        SetStatus("Resolving ad-hoc prompt...");
        try
        {
            var api = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync().ConfigureAwait(false);
            var resolved = await api.AgentPool.ResolvePromptAsync(request).ConfigureAwait(false);
            if (!resolved.Success)
            {
                SetStatus(resolved.Error ?? "Resolve failed.");
                return;
            }

            var queued = await api.AgentPool.EnqueueOneShotAsync(request).ConfigureAwait(false);
            if (!queued.Success)
            {
                SetStatus(queued.Error ?? "Enqueue failed.");
                return;
            }

            await LoadAsync().ConfigureAwait(false);
            SetStatus($"Queued '{queued.JobId}' for agent '{queued.AgentName ?? "auto"}'.");
        }
        catch (Exception ex)
        {
            SetStatus($"Enqueue failed: {ex.Message}");
        }
    }

    private async Task RunMutationAsync(
        string pendingStatus,
        Func<AgentPoolClient, Task<AgentPoolMutationResult>> operation,
        string successStatus)
    {
        SetStatus(pendingStatus);
        try
        {
            var api = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync().ConfigureAwait(false);
            var result = await operation(api.AgentPool).ConfigureAwait(false);
            if (!result.Success)
            {
                SetStatus(result.Error ?? "Operation failed.");
                return;
            }

            await LoadAsync().ConfigureAwait(false);
            SetStatus(successStatus);
        }
        catch (Exception ex)
        {
            SetStatus($"Operation failed: {ex.Message}");
        }
    }

    private string? GetAgentNameOrNull()
    {
        var typed = (_agentNameField.Text?.ToString() ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(typed))
            return typed;
        return GetSelectedAgent()?.AgentName;
    }

    private async Task CreateAgentForPoolAsync(NewAgentDialogValues values)
    {
        SetStatus($"Creating agent '{values.AgentName}'...");
        try
        {
            var workspacePath = GetRequiredActiveWorkspacePath();
            await _agentHandler.SaveDefinitionAsync(new AgentDefinitionSaveRequest(
                values.AgentName,
                values.AgentName,
                values.AgentPath,
                string.Empty,
                new[] { values.AgentModel },
                string.Empty,
                values.AgentSeed ?? string.Empty)).ConfigureAwait(false);
            await _agentHandler.AssignWorkspaceAgentAsync(workspacePath, values.AgentName).ConfigureAwait(false);
            var api = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync().ConfigureAwait(false);
            var startResult = await api.AgentPool.StartAgentAsync(values.AgentName).ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
            Application.Invoke(() => _agentNameField.Text = values.AgentName);

            var usesPoolOnlyFields = values.AgentParameters.Count > 0
                || values.IsInteractiveDefault
                || values.IsTodoPlanDefault
                || values.IsTodoStatusDefault
                || values.IsTodoImplementDefault;
            var startError = startResult.Error ?? "unknown error";
            var started = startResult.Success
                || startError.Contains("already", StringComparison.OrdinalIgnoreCase);
            if (!started)
            {
                SetStatus($"Agent '{values.AgentName}' created and added to workspace, but instance start failed: {startError}");
                return;
            }

            SetStatus(usesPoolOnlyFields
                ? $"Agent '{values.AgentName}' created, added, and started. Pool defaults/parameters require pool config update."
                : $"Agent '{values.AgentName}' created, added, and started.");
        }
        catch (Exception ex)
        {
            SetStatus($"Create agent failed: {ex.Message}");
        }
    }

    private string GetRequiredActiveWorkspacePath()
        => !string.IsNullOrWhiteSpace(_directorContext.ActiveWorkspacePath)
            ? _directorContext.ActiveWorkspacePath
            : throw new InvalidOperationException("No active workspace is selected.");

    private static bool IsChecked(CheckBox checkBox)
        => checkBox.CheckedState == CheckState.Checked;

    private static bool TryParseAgentParameters(string? raw, out Dictionary<string, string> values, out string? error)
    {
        values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        var entries = raw
            .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in entries)
        {
            var equalsIndex = entry.IndexOf('=');
            if (equalsIndex <= 0)
            {
                error = "Agent Parameters must use key=value format.";
                return false;
            }

            var key = entry[..equalsIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                error = "Agent Parameters key cannot be empty.";
                return false;
            }

            var value = entry[(equalsIndex + 1)..].Trim();
            values[key] = value;
        }

        return true;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void SetStatus(string text)
    {
        Application.Invoke(() => _statusLabel.Text = text);
    }

    private static string Truncate(string value, int max)
    {
        if (value.Length <= max)
            return value;
        if (max <= 3)
            return value[..max];
        return value[..(max - 3)] + "...";
    }

    private sealed record NewAgentDialogValues(
        string AgentName,
        string AgentPath,
        string AgentModel,
        string? AgentSeed,
        Dictionary<string, string> AgentParameters,
        bool IsInteractiveDefault,
        bool IsTodoPlanDefault,
        bool IsTodoStatusDefault,
        bool IsTodoImplementDefault);
}
