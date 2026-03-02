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
    private readonly List<AgentPoolAgentStatus> _agentRows = [];
    private readonly List<AgentPoolQueueItem> _queueRows = [];

    private Label _statusLabel = null!;
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
            var agents = await api.AgentPool.GetAgentsAsync().ConfigureAwait(false);
            var queue = await api.AgentPool.GetQueueAsync().ConfigureAwait(false);

            Application.Invoke(() =>
            {
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

                _statusLabel.Text = $"Agent pool loaded ({_agentRows.Count} agents, {_queueRows.Count} jobs)";
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

        var agentsLabel = new Label { X = 0, Y = 1, Text = "Agents" };
        var newAgentBtn = new Button { X = Pos.Right(agentsLabel) + 2, Y = 1, Text = "New Agent" };
        newAgentBtn.Accepting += (_, _) => ShowNewAgentDialog();
        Add(agentsLabel, newAgentBtn);

        _agentsTable = new TableView
        {
            X = 0,
            Y = 2,
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

    private void SyncAgentNameFromSelection()
    {
        var selected = GetSelectedAgent();
        if (selected is null)
            return;

        Application.Invoke(() => _agentNameField.Text = selected.AgentName);
    }

    private void ShowNewAgentDialog()
    {
        var dialog = new Dialog
        {
            Title = "New Agent",
            Width = 60,
            Height = 10,
        };

        var nameLabel = new Label { X = 1, Y = 1, Text = "Agent Name:" };
        var initialName = (_agentNameField.Text?.ToString() ?? string.Empty).Trim();
        var nameField = new TextField { X = 13, Y = 1, Width = 44, Text = initialName };

        var createBtn = new Button { Text = "Create" };
        createBtn.Accepting += (_, _) =>
        {
            var agentName = (nameField.Text?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(agentName))
            {
                SetStatus("Enter an agent name first.");
                return;
            }

            Application.RequestStop();
            _ = Task.Run(() => CreateAgentForPoolAsync(agentName));
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();

        dialog.Add(nameLabel, nameField);
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

    private async Task CreateAgentForPoolAsync(string agentName)
    {
        SetStatus($"Creating agent '{agentName}'...");
        try
        {
            var workspacePath = GetRequiredActiveWorkspacePath();
            var createdDefinition = false;
            var definitionAlreadyExists = false;
            try
            {
                await _agentHandler.CreateDefinitionAsync(agentName).ConfigureAwait(false);
                createdDefinition = true;
            }
            catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                definitionAlreadyExists = true;
            }

            await _agentHandler.AssignWorkspaceAgentAsync(workspacePath, agentName).ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
            Application.Invoke(() => _agentNameField.Text = agentName);
            SetStatus(createdDefinition
                ? $"Agent '{agentName}' created and added to workspace."
                : definitionAlreadyExists
                    ? $"Agent '{agentName}' already existed and was added to workspace."
                    : $"Agent '{agentName}' added to workspace.");
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
}
