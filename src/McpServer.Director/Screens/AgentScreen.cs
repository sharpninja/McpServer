using System.Text.Json;
using Terminal.Gui;

namespace McpServer.Director.Screens;

internal sealed class AgentScreen : View
{
    private readonly DirectorMcpContext _context;
    private TableView _defsTable = null!;
    private TableView _agentsTable = null!;
    private TextView _statusLabel = null!;

    private TextField _detailAgentIdField = null!;
    private TextField _detailWorkspaceField = null!;
    private CheckBox _detailEnabledField = null!;
    private TextField _detailIsolationField = null!;
    private Button _detailIsolationToggleBtn = null!;
    private CheckBox _detailBannedField = null!;
    private TextField _detailBannedReasonField = null!;
    private TextField _detailLaunchCommandOverrideField = null!;
    private TextField _detailModelsOverrideField = null!;
    private TextField _detailBranchStrategyOverrideField = null!;
    private TextField _detailInstructionFilesOverrideField = null!;
    private TextView _detailSeedPromptOverrideView = null!;
    private TextView _detailMarkerAdditionsView = null!;

    private List<AgentDefRow> _defRows = [];
    private List<AgentRow> _agentRows = [];
    private int _detailLoadVersion;
    private string? _detailLoadedAgentId;

    public AgentScreen(DirectorMcpContext context)
    {
        _context = context;
        Title = "Agents";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        var leftPane = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(54),
            Height = Dim.Fill(3),
        };
        Add(leftPane);

        var rightPane = new FrameView
        {
            Title = "Workspace Agent Detail",
            X = Pos.Right(leftPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
        };
        Add(rightPane);

        var defsFrame = new FrameView
        {
            Title = "Agent Definitions",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(40),
        };
        _defsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _defsTable.KeyDown += (_, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                _ = Task.Run(AssignSelectedDefinitionAsync);
                e.Handled = true;
            }
        };
        defsFrame.Add(_defsTable);
        leftPane.Add(defsFrame);

        var agentsFrame = new FrameView
        {
            Title = "Workspace Agents",
            X = 0,
            Y = Pos.Bottom(defsFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _agentsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _agentsTable.SelectedCellChanged += (_, _) => QueueSelectedAgentDetailRefresh();
        agentsFrame.Add(_agentsTable);
        leftPane.Add(agentsFrame);

        BuildDetailPane(rightPane);

        _statusLabel = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = false,
            Text = "",
        };
        Add(_statusLabel);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAllAsync);

        var assignBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Assign Selected" };
        assignBtn.Accepting += (_, _) => _ = Task.Run(AssignSelectedDefinitionAsync);

        var addBtn = new Button { X = Pos.Right(assignBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Add by ID" };
        addBtn.Accepting += (_, _) => ShowAddDialog();

        var banBtn = new Button { X = Pos.Right(addBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Ban" };
        banBtn.Accepting += (_, _) => ShowBanDialog();

        var unbanBtn = new Button { X = Pos.Right(banBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Unban" };
        unbanBtn.Accepting += (_, _) => _ = Task.Run(UnbanSelectedAsync);

        var deleteBtn = new Button { X = Pos.Right(unbanBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedAsync);

        var validateBtn = new Button { X = Pos.Right(deleteBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Validate" };
        validateBtn.Accepting += (_, _) => _ = Task.Run(ValidateAsync);

        Add(refreshBtn, assignBtn, addBtn, banBtn, unbanBtn, deleteBtn, validateBtn);
        ClearDetailEditor();
    }

    private void BuildDetailPane(FrameView parent)
    {
        var row = 0;

        parent.Add(new Label { X = 0, Y = row, Text = "Agent ID:" });
        _detailAgentIdField = new TextField { X = 16, Y = row, Width = Dim.Fill(), ReadOnly = true, Text = "" };
        parent.Add(_detailAgentIdField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Workspace:" });
        _detailWorkspaceField = new TextField { X = 16, Y = row, Width = Dim.Fill(), ReadOnly = true, Text = "" };
        parent.Add(_detailWorkspaceField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Enabled:" });
        _detailEnabledField = new CheckBox
        {
            X = 16,
            Y = row,
            Width = 12,
            Text = "",
            CheckedState = CheckState.Checked,
        };
        parent.Add(_detailEnabledField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Isolation:" });
        _detailIsolationField = new TextField
        {
            X = 16,
            Y = row,
            Width = Dim.Fill(8),
            ReadOnly = true,
            Text = "worktree",
        };
        parent.Add(_detailIsolationField);
        _detailIsolationToggleBtn = new Button { X = Pos.Right(_detailIsolationField) + 1, Y = row, Text = "Toggle" };
        _detailIsolationToggleBtn.Accepting += (_, _) =>
        {
            var current = _detailIsolationField.Text?.ToString();
            _detailIsolationField.Text = NextIsolationValue(current);
        };
        parent.Add(_detailIsolationToggleBtn);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Banned:" });
        _detailBannedField = new CheckBox
        {
            X = 16,
            Y = row,
            Width = 12,
            Text = "",
            CheckedState = CheckState.UnChecked,
            Enabled = false,
        };
        parent.Add(_detailBannedField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Ban Reason:" });
        _detailBannedReasonField = new TextField { X = 16, Y = row, Width = Dim.Fill(), ReadOnly = true, Text = "" };
        parent.Add(_detailBannedReasonField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Launch Cmd:" });
        _detailLaunchCommandOverrideField = new TextField { X = 16, Y = row, Width = Dim.Fill(), Text = "" };
        parent.Add(_detailLaunchCommandOverrideField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Models CSV:" });
        _detailModelsOverrideField = new TextField { X = 16, Y = row, Width = Dim.Fill(), Text = "" };
        parent.Add(_detailModelsOverrideField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Branch:" });
        _detailBranchStrategyOverrideField = new TextField { X = 16, Y = row, Width = Dim.Fill(), Text = "" };
        parent.Add(_detailBranchStrategyOverrideField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Instr CSV:" });
        _detailInstructionFilesOverrideField = new TextField { X = 16, Y = row, Width = Dim.Fill(), Text = "" };
        parent.Add(_detailInstructionFilesOverrideField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Seed Prompt Override:" });
        row++;

        _detailSeedPromptOverrideView = new TextView { X = 0, Y = row, Width = Dim.Fill(), Height = 3, WordWrap = false, Text = "" };
        parent.Add(_detailSeedPromptOverrideView);
        row += 3;

        parent.Add(new Label { X = 0, Y = row, Text = "Marker Additions:" });
        row++;

        _detailMarkerAdditionsView = new TextView { X = 0, Y = row, Width = Dim.Fill(), Height = Dim.Fill(2), WordWrap = false, Text = "" };
        parent.Add(_detailMarkerAdditionsView);

        var saveDetailBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Save Detail" };
        saveDetailBtn.Accepting += (_, _) => QueueSaveSelectedDetail();
        var reloadDetailBtn = new Button { X = Pos.Right(saveDetailBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Reload Detail" };
        reloadDetailBtn.Accepting += (_, _) => QueueReloadSelectedDetail();
        parent.Add(saveDetailBtn, reloadDetailBtn);
    }

    public async Task LoadAllAsync()
    {
        await LoadDefinitionsAsync().ConfigureAwait(false);
        await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
    }

    private async Task LoadDefinitionsAsync()
    {
        SetStatus("Loading definitions...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var result = await client.GetAsync<JsonElement>("/mcp/agents/definitions").ConfigureAwait(false);
            var items = result.GetProperty("items");
            var rows = new List<AgentDefRow>();
            foreach (var item in items.EnumerateArray())
            {
                rows.Add(new AgentDefRow(
                    item.GetProperty("id").GetString() ?? "",
                    item.GetProperty("displayName").GetString() ?? "",
                    item.TryGetProperty("isBuiltIn", out var bi) && bi.GetBoolean() ? "Yes" : "No"));
            }

            _defRows = rows;
            Application.Invoke(() =>
            {
                _defsTable.Table = new EnumerableTableSource<AgentDefRow>(rows,
                    new Dictionary<string, Func<AgentDefRow, object>>
                    {
                        ["ID"] = r => r.Id,
                        ["Display Name"] = r => r.DisplayName,
                        ["Built-In"] = r => r.BuiltIn,
                    });
            });
            SetStatus($"{rows.Count} definitions loaded");
        }
        catch (Exception ex)
        {
            SetStatus($"Load definitions failed: {ex.Message}");
        }
    }

    private async Task LoadWorkspaceAgentsAsync()
    {
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            var result = await client.GetAsync<JsonElement>($"/mcp/agents?workspace={path}").ConfigureAwait(false);
            var items = result.GetProperty("items");
            var rows = new List<AgentRow>();
            foreach (var item in items.EnumerateArray())
            {
                rows.Add(new AgentRow(
                    item.GetProperty("agentId").GetString() ?? "",
                    item.TryGetProperty("enabled", out var en) && en.GetBoolean() ? "Yes" : "No",
                    item.TryGetProperty("banned", out var b) && b.GetBoolean() ? "Yes" : "No",
                    item.TryGetProperty("agentIsolation", out var iso) ? iso.GetString() ?? "worktree" : "worktree"));
            }

            _agentRows = rows;
            Application.Invoke(() =>
            {
                _agentsTable.Table = new EnumerableTableSource<AgentRow>(rows,
                    new Dictionary<string, Func<AgentRow, object>>
                    {
                        ["Agent ID"] = r => r.AgentId,
                        ["Enabled"] = r => r.Enabled,
                        ["Banned"] = r => r.Banned,
                        ["Isolation"] = r => r.Isolation,
                    });

                if (rows.Count == 0)
                    ClearDetailEditor();
            });

            QueueSelectedAgentDetailRefresh();
        }
        catch (Exception ex)
        {
            SetStatus($"Load workspace agents failed: {ex.Message}");
        }
    }

    private string? GetSelectedAgentId()
    {
        var row = _agentsTable.SelectedRow;
        if (row >= 0 && row < _agentRows.Count)
            return _agentRows[row].AgentId;
        return null;
    }

    private string? GetSelectedDefinitionId()
    {
        var row = _defsTable.SelectedRow;
        if (row >= 0 && row < _defRows.Count)
            return _defRows[row].Id;
        return null;
    }

    private void QueueSelectedAgentDetailRefresh()
    {
        string? agentId = null;
        Application.Invoke(() => agentId = GetSelectedAgentId());
        QueueAgentDetailRefresh(agentId);
    }

    private void QueueReloadSelectedDetail()
    {
        string? agentId = null;
        Application.Invoke(() => agentId = GetSelectedAgentId() ?? _detailLoadedAgentId ?? (_detailAgentIdField.Text?.ToString()));
        QueueAgentDetailRefresh(agentId);
    }

    private void QueueAgentDetailRefresh(string? agentId)
    {
        var version = System.Threading.Interlocked.Increment(ref _detailLoadVersion);
        if (string.IsNullOrWhiteSpace(agentId))
        {
            Application.Invoke(ClearDetailEditor);
            return;
        }

        _ = Task.Run(() => LoadAgentDetailAsync(agentId, version));
    }

    private async Task LoadAgentDetailAsync(string agentId, int version)
    {
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            var result = await client.GetAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}?workspace={path}").ConfigureAwait(false);

            var detail = new AgentDetailState
            {
                AgentId = GetString(result, "agentId") ?? agentId,
                WorkspacePath = GetString(result, "workspacePath") ?? client.WorkspacePath,
                Enabled = GetBool(result, "enabled"),
                Banned = GetBool(result, "banned"),
                BannedReason = GetString(result, "bannedReason") ?? "",
                AgentIsolation = GetString(result, "agentIsolation") ?? "worktree",
                LaunchCommandOverride = GetString(result, "launchCommandOverride"),
                ModelsOverride = GetStringArray(result, "modelsOverride"),
                BranchStrategyOverride = GetString(result, "branchStrategyOverride"),
                SeedPromptOverride = GetString(result, "seedPromptOverride"),
                MarkerAdditions = GetString(result, "markerAdditions") ?? "",
                InstructionFilesOverride = GetStringArray(result, "instructionFilesOverride"),
            };

            if (version != System.Threading.Volatile.Read(ref _detailLoadVersion))
                return;

            Application.Invoke(() => ApplyDetailEditor(detail));
        }
        catch (Exception ex)
        {
            if (version != System.Threading.Volatile.Read(ref _detailLoadVersion))
                return;

            SetStatus($"Load detail failed: {ex.Message}");
        }
    }

    private void ApplyDetailEditor(AgentDetailState detail)
    {
        _detailLoadedAgentId = detail.AgentId;
        _detailAgentIdField.Text = detail.AgentId;
        _detailWorkspaceField.Text = detail.WorkspacePath;
        _detailEnabledField.CheckedState = detail.Enabled ? CheckState.Checked : CheckState.UnChecked;
        _detailIsolationField.Text = NormalizeIsolationValue(detail.AgentIsolation);
        _detailBannedField.CheckedState = detail.Banned ? CheckState.Checked : CheckState.UnChecked;
        _detailBannedReasonField.Text = detail.BannedReason;
        _detailLaunchCommandOverrideField.Text = detail.LaunchCommandOverride ?? "";
        _detailModelsOverrideField.Text = JoinCsv(detail.ModelsOverride);
        _detailBranchStrategyOverrideField.Text = detail.BranchStrategyOverride ?? "";
        _detailInstructionFilesOverrideField.Text = JoinCsv(detail.InstructionFilesOverride);
        _detailSeedPromptOverrideView.Text = detail.SeedPromptOverride ?? "";
        _detailMarkerAdditionsView.Text = detail.MarkerAdditions ?? "";
    }

    private void ClearDetailEditor()
    {
        _detailLoadedAgentId = null;
        _detailAgentIdField.Text = "";
        _detailWorkspaceField.Text = "";
        _detailEnabledField.CheckedState = CheckState.Checked;
        _detailIsolationField.Text = "worktree";
        _detailBannedField.CheckedState = CheckState.UnChecked;
        _detailBannedReasonField.Text = "";
        _detailLaunchCommandOverrideField.Text = "";
        _detailModelsOverrideField.Text = "";
        _detailBranchStrategyOverrideField.Text = "";
        _detailInstructionFilesOverrideField.Text = "";
        _detailSeedPromptOverrideView.Text = "";
        _detailMarkerAdditionsView.Text = "";
    }

    private void QueueSaveSelectedDetail()
    {
        AgentDetailSaveRequest? request = null;
        string? error = null;

        Application.Invoke(() =>
        {
            var agentId = (_detailAgentIdField.Text?.ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(agentId))
            {
                error = "Select a workspace agent row first";
                return;
            }

            var enabled = _detailEnabledField.CheckedState == CheckState.Checked;

            var isolation = NormalizeIsolationValue(_detailIsolationField.Text?.ToString());
            if (!string.Equals(isolation, "worktree", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(isolation, "clone", StringComparison.OrdinalIgnoreCase))
            {
                error = "Isolation must be one of: worktree, clone";
                return;
            }

            request = new AgentDetailSaveRequest(
                agentId,
                enabled,
                isolation,
                NullIfWhiteSpace(_detailLaunchCommandOverrideField.Text?.ToString()),
                ParseCsvOrNull(_detailModelsOverrideField.Text?.ToString()),
                NullIfWhiteSpace(_detailBranchStrategyOverrideField.Text?.ToString()),
                NullIfWhiteSpace(_detailSeedPromptOverrideView.Text?.ToString()),
                _detailMarkerAdditionsView.Text?.ToString() ?? "",
                ParseCsvOrNull(_detailInstructionFilesOverrideField.Text?.ToString()));
        });

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetStatus(error);
            return;
        }

        if (request is null)
            return;

        _ = Task.Run(() => SaveSelectedDetailAsync(request));
    }

    private async Task SaveSelectedDetailAsync(AgentDetailSaveRequest request)
    {
        SetStatus($"Saving detail for '{request.AgentId}'...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            var body = new
            {
                agentId = request.AgentId,
                enabled = request.Enabled,
                agentIsolation = request.AgentIsolation,
                launchCommandOverride = request.LaunchCommandOverride,
                modelsOverride = request.ModelsOverride,
                branchStrategyOverride = request.BranchStrategyOverride,
                seedPromptOverride = request.SeedPromptOverride,
                markerAdditions = request.MarkerAdditions,
                instructionFilesOverride = request.InstructionFilesOverride,
            };

            await client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(request.AgentId)}?workspace={path}", body).ConfigureAwait(false);
            SetStatus($"Workspace agent '{request.AgentId}' updated");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(request.AgentId);
        }
        catch (Exception ex)
        {
            SetStatus($"Save detail failed: {ex.Message}");
        }
    }

    private async Task AssignSelectedDefinitionAsync()
    {
        var agentId = GetSelectedDefinitionId();
        if (string.IsNullOrWhiteSpace(agentId))
        {
            SetStatus("Select an agent definition first (top grid), or use Add by ID");
            return;
        }

        SetStatus($"Assigning '{agentId}' to current workspace...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            var body = new { agentId, enabled = true, agentIsolation = "worktree" };
            await client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}?workspace={path}", body).ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' assigned to workspace");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(agentId);
        }
        catch (Exception ex)
        {
            SetStatus($"Assign failed: {ex.Message}");
        }
    }

    private void ShowAddDialog()
    {
        var dlg = new Dialog { Title = "Add Agent", Width = 58, Height = 11 };
        var idLabel = new Label { X = 1, Y = 1, Text = "Agent ID:" };
        var idField = new TextField { X = 12, Y = 1, Width = 42, Text = "" };
        var helpLabel = new Label { X = 1, Y = 3, Text = "If the definition does not exist, create it first." };
        dlg.Add(idLabel, idField, helpLabel);

        var addBtn = new Button { Text = "Add" };
        addBtn.Accepting += (_, _) =>
        {
            var agentId = idField.Text ?? "";
            if (string.IsNullOrWhiteSpace(agentId))
                return;
            Application.RequestStop();
            _ = Task.Run(() => AddAgentToCurrentWorkspaceAsync(agentId));
        };

        var createDefBtn = new Button { Text = "Create Definition" };
        createDefBtn.Accepting += (_, _) =>
        {
            var agentId = idField.Text ?? "";
            if (string.IsNullOrWhiteSpace(agentId))
            {
                SetStatus("Enter an agent ID first");
                return;
            }
            _ = Task.Run(() => CreateAgentDefinitionAsync(agentId, refreshDefinitions: true));
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(addBtn);
        dlg.AddButton(createDefBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowBanDialog()
    {
        var agentId = GetSelectedAgentId();
        if (agentId is null)
        {
            SetStatus("Select a workspace agent first");
            return;
        }

        var dlg = new Dialog { Title = $"Ban {agentId}", Width = 50, Height = 10 };
        var reasonLabel = new Label { X = 1, Y = 1, Text = "Reason:" };
        var reasonField = new TextField { X = 10, Y = 1, Width = 30, Text = "" };
        dlg.Add(reasonLabel, reasonField);

        var okBtn = new Button { Text = "Ban" };
        okBtn.Accepting += (_, _) =>
        {
            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                SetStatus($"Banning {agentId}...");
                try
                {
                    var client = _context.GetRequiredActiveWorkspaceHttpClient();
                    var path = Uri.EscapeDataString(client.WorkspacePath);
                    var body = new { reason = reasonField.Text ?? "", global = false };
                    await client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}/ban?workspace={path}", body).ConfigureAwait(false);
                    SetStatus($"Agent '{agentId}' banned");
                    await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
                    QueueAgentDetailRefresh(agentId);
                }
                catch (Exception ex)
                {
                    SetStatus($"Ban failed: {ex.Message}");
                }
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(okBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task UnbanSelectedAsync()
    {
        var agentId = GetSelectedAgentId();
        if (agentId is null)
        {
            SetStatus("Select a workspace agent first");
            return;
        }

        SetStatus($"Unbanning {agentId}...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            await client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}/unban?workspace={path}&global=false").ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' unbanned");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(agentId);
        }
        catch (Exception ex)
        {
            SetStatus($"Unban failed: {ex.Message}");
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var agentId = GetSelectedAgentId();
        if (agentId is null)
        {
            SetStatus("Select a workspace agent first");
            return;
        }

        SetStatus($"Deleting {agentId}...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            await client.DeleteAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}?workspace={path}").ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' removed");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetStatus($"Delete failed: {ex.Message}");
        }
    }

    private async Task ValidateAsync()
    {
        SetStatus("Validating agents.yaml...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            var result = await client.GetAsync<JsonElement>($"/mcp/agents/validate?workspace={path}").ConfigureAwait(false);
            var valid = result.TryGetProperty("valid", out var v) && v.GetBoolean();
            SetStatus(valid
                ? "agents.yaml is valid"
                : $"Validation failed: {(result.TryGetProperty("error", out var e) ? e.GetString() : "unknown")}");
        }
        catch (Exception ex)
        {
            SetStatus($"Validate failed: {ex.Message}");
        }
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);

    private async Task AddAgentToCurrentWorkspaceAsync(string agentId)
    {
        SetStatus($"Adding {agentId}...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var path = Uri.EscapeDataString(client.WorkspacePath);
            var body = new { agentId, enabled = true, agentIsolation = "worktree" };
            await client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}?workspace={path}", body).ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' added");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(agentId);
        }
        catch (Exception ex)
        {
            var message = ex.Message.Contains("Create it first", StringComparison.OrdinalIgnoreCase)
                ? $"{ex.Message} Use 'Create Definition' in Add by ID."
                : ex.Message;
            SetStatus($"Add failed: {message}");
        }
    }

    private async Task CreateAgentDefinitionAsync(string agentId, bool refreshDefinitions)
    {
        SetStatus($"Creating definition '{agentId}'...");
        try
        {
            var client = _context.GetRequiredActiveWorkspaceHttpClient();
            var body = new { id = agentId, displayName = agentId };
            await client.PostAsync<JsonElement>("/mcp/agents/definitions", body).ConfigureAwait(false);
            if (refreshDefinitions)
                await LoadDefinitionsAsync().ConfigureAwait(false);
            SetStatus($"Definition '{agentId}' created");
        }
        catch (Exception ex)
        {
            SetStatus($"Create definition failed: {ex.Message}");
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;
        return value.ValueKind == JsonValueKind.Null ? null : value.ToString();
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return false;
        if (value.ValueKind == JsonValueKind.True) return true;
        if (value.ValueKind == JsonValueKind.False) return false;
        return bool.TryParse(value.ToString(), out var parsed) && parsed;
    }

    private static IReadOnlyList<string>? GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.Array)
            return null;

        var items = new List<string>();
        foreach (var entry in value.EnumerateArray())
        {
            var s = entry.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                items.Add(s);
        }
        return items.Count == 0 ? null : items;
    }

    private static string JoinCsv(IReadOnlyList<string>? items)
        => items is null || items.Count == 0 ? "" : string.Join(", ", items);

    private static string NormalizeIsolationValue(string? value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        return normalized switch
        {
            "clone" => "clone",
            _ => "worktree",
        };
    }

    private static string NextIsolationValue(string? current)
        => string.Equals(NormalizeIsolationValue(current), "worktree", StringComparison.Ordinal)
            ? "clone"
            : "worktree";

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string[]? ParseCsvOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
        return items.Length == 0 ? null : items;
    }

    private sealed record AgentDefRow(string Id, string DisplayName, string BuiltIn);
    private sealed record AgentRow(string AgentId, string Enabled, string Banned, string Isolation);

    private sealed class AgentDetailState
    {
        public string AgentId { get; init; } = "";
        public string WorkspacePath { get; init; } = "";
        public bool Enabled { get; init; }
        public bool Banned { get; init; }
        public string BannedReason { get; init; } = "";
        public string AgentIsolation { get; init; } = "worktree";
        public string? LaunchCommandOverride { get; init; }
        public IReadOnlyList<string>? ModelsOverride { get; init; }
        public string? BranchStrategyOverride { get; init; }
        public string? SeedPromptOverride { get; init; }
        public string? MarkerAdditions { get; init; }
        public IReadOnlyList<string>? InstructionFilesOverride { get; init; }
    }

    private sealed record AgentDetailSaveRequest(
        string AgentId,
        bool Enabled,
        string AgentIsolation,
        string? LaunchCommandOverride,
        string[]? ModelsOverride,
        string? BranchStrategyOverride,
        string? SeedPromptOverride,
        string MarkerAdditions,
        string[]? InstructionFilesOverride);
}
