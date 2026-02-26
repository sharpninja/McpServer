using McpServer.Director.Handlers;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Screens;

internal sealed class AgentScreen : View
{
    private readonly DirectorMcpContext _context;
    private readonly AgentScreenHandler _handler;
    private TableView _defsTable = null!;
    private TableView _agentsTable = null!;
    private TextView _statusLabel = null!;
    private FrameView _detailFrame = null!;

    private TextField _detailLevelField = null!;
    private TextField _detailAgentIdField = null!;
    private TextField _detailDisplayNameField = null!;
    private CheckBox _detailBuiltInField = null!;
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
    private bool _workspaceAgentDetailRefreshScheduled;
    private bool _definitionDetailRefreshScheduled;
    private AgentDetailLevel _detailLevel;
    private static readonly object s_traceSync = new();
    private readonly ILogger<AgentScreen> _logger;


    public AgentScreen(DirectorMcpContext context,
        ILogger<AgentScreen>? logger = null)
    {
        _logger = logger ?? NullLogger<AgentScreen>.Instance;
        _context = context;
        _handler = new AgentScreenHandler(context);
        Title = "Agents";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        TraceUi("ctor");
        BuildUi();
    }

    private void BuildUi()
    {
        TraceUi("build-ui");
        var leftPane = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(54),
            Height = Dim.Fill(3),
        };
        Add(leftPane);

        _detailFrame = new FrameView
        {
            Title = "Agent Details",
            X = Pos.Right(leftPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
        };
        Add(_detailFrame);

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
        _defsTable.SelectedCellChanged += (_, _) => QueueSelectedDefinitionDetailRefreshOnTimeout();
        _defsTable.MouseClick += (_, _) => QueueSelectedDefinitionDetailRefreshOnTimeout();
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
        _agentsTable.SelectedCellChanged += (_, _) => QueueSelectedAgentDetailRefreshOnTimeout();
        _agentsTable.SelectedCellChanged += (_, e) => TraceUi(
            $"agents.selected-cell-changed old=({e.OldRow},{e.OldCol}) new=({e.NewRow},{e.NewCol}) selectedRow={_agentsTable.SelectedRow}");
        _agentsTable.KeyDown += (_, e) => TraceUi($"agents.key-down key={e.KeyCode} selectedRow={_agentsTable.SelectedRow}");
        _agentsTable.KeyDownNotHandled += (_, e) => TraceUi($"agents.key-down-not-handled key={e.KeyCode} selectedRow={_agentsTable.SelectedRow}");
        _agentsTable.MouseClick += (_, e) =>
        {
            TraceUi($"agents.mouse-click flags={e.Flags} selectedRow={_agentsTable.SelectedRow}");
            QueueSelectedAgentDetailRefreshOnTimeout();
        };
        agentsFrame.Add(_agentsTable);
        leftPane.Add(agentsFrame);

        BuildDetailPane(_detailFrame);

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

        parent.Add(new Label { X = 0, Y = row, Text = "Level:" });
        _detailLevelField = new TextField { X = 16, Y = row, Width = Dim.Fill(), ReadOnly = true, Text = "" };
        parent.Add(_detailLevelField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Agent ID:" });
        _detailAgentIdField = new TextField { X = 16, Y = row, Width = Dim.Fill(), ReadOnly = true, Text = "" };
        parent.Add(_detailAgentIdField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Display Name:" });
        _detailDisplayNameField = new TextField { X = 16, Y = row, Width = Dim.Fill(), Text = "" };
        parent.Add(_detailDisplayNameField);
        row++;

        parent.Add(new Label { X = 0, Y = row, Text = "Built-In:" });
        _detailBuiltInField = new CheckBox
        {
            X = 16,
            Y = row,
            Width = 12,
            Text = "",
            CheckedState = CheckState.UnChecked,
            Enabled = false,
        };
        parent.Add(_detailBuiltInField);
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
        TraceUi("load-all.start");
        await LoadDefinitionsAsync().ConfigureAwait(false);
        await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
        TraceUi("load-all.end");
    }

    private async Task LoadDefinitionsAsync()
    {
        SetStatus("Loading definitions...");
        try
        {
            var definitions = await _handler.ListDefinitionsAsync().ConfigureAwait(false);
            var rows = definitions
                .Select(static item => new AgentDefRow(
                    item.Id,
                    item.DisplayName,
                    item.IsBuiltIn ? "Yes" : "No"))
                .ToList();

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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Load definitions failed: {ex.Message}");
        }
    }

    private async Task LoadWorkspaceAgentsAsync()
    {
        try
        {
            var workspacePath = GetRequiredActiveWorkspacePath();
            var agents = await _handler.ListWorkspaceAgentsAsync(workspacePath).ConfigureAwait(false);
            var rows = agents
                .Select(static item => new AgentRow(
                    item.AgentId,
                    item.Enabled ? "Yes" : "No",
                    item.Banned ? "Yes" : "No",
                    item.AgentIsolation))
                .ToList();

            _agentRows = rows;
            Application.Invoke(() =>
            {
                TraceUi($"load-workspace-agents.bind rows={rows.Count} selectedRow(before)={_agentsTable.SelectedRow}");
                _agentsTable.Table = new EnumerableTableSource<AgentRow>(rows,
                    new Dictionary<string, Func<AgentRow, object>>
                    {
                        ["Agent ID"] = r => r.AgentId,
                        ["Enabled"] = r => r.Enabled,
                        ["Banned"] = r => r.Banned,
                        ["Isolation"] = r => r.Isolation,
                    });

                if (rows.Count == 0)
                {
                    ClearDetailEditor();
                    return;
                }

                if (_agentsTable.SelectedRow < 0 || _agentsTable.SelectedRow >= rows.Count)
                {
                    _agentsTable.SelectedRow = 0;
                    TraceUi("load-workspace-agents.set-selected-row row=0");
                }

                TraceUi($"load-workspace-agents.queue-initial-detail selectedRow(after)={_agentsTable.SelectedRow}");
                QueueSelectedAgentDetailRefresh();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Load workspace agents failed: {ex.Message}");
        }
    }

    private string? GetSelectedAgentId()
    {
        var row = _agentsTable.SelectedRow;
        TraceUi($"get-selected-agent-id row={row} rows={_agentRows.Count}");
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

    private string GetRequiredActiveWorkspacePath()
        => _context.GetRequiredActiveWorkspaceHttpClient().WorkspacePath;

    private void QueueSelectedDefinitionDetailRefresh()
        => QueueDefinitionDetailRefresh(GetSelectedDefinitionId());

    private void QueueSelectedDefinitionDetailRefreshOnTimeout()
    {
        if (_definitionDetailRefreshScheduled)
            return;

        _definitionDetailRefreshScheduled = true;
        Application.AddTimeout(TimeSpan.FromMilliseconds(1), () =>
        {
            _definitionDetailRefreshScheduled = false;
            QueueSelectedDefinitionDetailRefresh();
            return false;
        });
    }

    private void QueueSelectedAgentDetailRefresh()
    {
        TraceUi($"queue-selected-agent-detail-refresh selectedRow={_agentsTable.SelectedRow}");
        QueueAgentDetailRefresh(GetSelectedAgentId());
    }

    private void QueueSelectedAgentDetailRefreshOnTimeout()
    {
        if (_workspaceAgentDetailRefreshScheduled)
        {
            TraceUi("queue-selected-agent-detail-refresh-on-timeout skipped(already-scheduled)");
            return;
        }

        _workspaceAgentDetailRefreshScheduled = true;
        TraceUi($"queue-selected-agent-detail-refresh-on-timeout scheduled selectedRow={_agentsTable.SelectedRow}");
        Application.AddTimeout(TimeSpan.FromMilliseconds(1), () =>
        {
            _workspaceAgentDetailRefreshScheduled = false;
            TraceUi($"queue-selected-agent-detail-refresh-on-timeout run selectedRow={_agentsTable.SelectedRow}");
            QueueSelectedAgentDetailRefresh();
            return false;
        });
    }

    private void QueueReloadSelectedDetail()
    {
        var agentId = (_detailAgentIdField.Text?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(agentId))
        {
            SetStatus("Select an agent definition or workspace agent first");
            return;
        }

        if (_detailLevel == AgentDetailLevel.Definition)
        {
            QueueDefinitionDetailRefresh(agentId);
            return;
        }

        QueueAgentDetailRefresh(agentId);
    }

    private void QueueDefinitionDetailRefresh(string? agentId)
    {
        var version = System.Threading.Interlocked.Increment(ref _detailLoadVersion);
        if (string.IsNullOrWhiteSpace(agentId))
        {
            Application.Invoke(ClearDetailEditor);
            return;
        }

        _ = Task.Run(() => LoadDefinitionDetailAsync(agentId, version));
    }

    private void QueueAgentDetailRefresh(string? agentId)
    {
        var version = System.Threading.Interlocked.Increment(ref _detailLoadVersion);
        TraceUi($"queue-agent-detail-refresh version={version} agentId={agentId ?? "(null)"}");
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
            TraceUi($"load-agent-detail.start version={version} agentId={agentId}");
            var workspacePath = GetRequiredActiveWorkspacePath();
            var detail = await _handler.GetWorkspaceAgentDetailAsync(workspacePath, agentId).ConfigureAwait(false);

            if (version != System.Threading.Volatile.Read(ref _detailLoadVersion))
            {
                TraceUi($"load-agent-detail.drop-stale version={version} latest={System.Threading.Volatile.Read(ref _detailLoadVersion)} agentId={agentId}");
                return;
            }

            TraceUi($"load-agent-detail.apply version={version} agentId={detail.AgentId}");
            Application.Invoke(() => ApplyDetailEditor(detail));
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            if (version != System.Threading.Volatile.Read(ref _detailLoadVersion))
                return;

            SetStatus($"Load detail failed: {ex.Message}");
        }
    }

    private async Task LoadDefinitionDetailAsync(string agentId, int version)
    {
        try
        {
            var detail = await _handler.GetDefinitionDetailAsync(agentId).ConfigureAwait(false);

            if (version != System.Threading.Volatile.Read(ref _detailLoadVersion))
                return;

            Application.Invoke(() => ApplyDefinitionDetailEditor(detail));
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            if (version != System.Threading.Volatile.Read(ref _detailLoadVersion))
                return;

            SetStatus($"Load definition detail failed: {ex.Message}");
        }
    }

    private void ApplyDetailEditor(AgentDetailState detail)
    {
        TraceUi($"apply-detail-editor agentId={detail.AgentId}");
        SetDetailLevel(AgentDetailLevel.WorkspaceAssignment);
        _detailLoadedAgentId = detail.AgentId;
        _detailLevelField.Text = "Workspace Assignment";
        _detailAgentIdField.Text = detail.AgentId;
        _detailDisplayNameField.Text = TryGetDefinitionDisplayName(detail.AgentId) ?? detail.AgentId;
        _detailBuiltInField.CheckedState = TryIsBuiltInDefinition(detail.AgentId) ? CheckState.Checked : CheckState.UnChecked;
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
        ApplyDetailEditability();
        SetNeedsDraw();
        SuperView?.SetNeedsDraw();
    }

    private void ApplyDefinitionDetailEditor(AgentDefinitionDetailState detail)
    {
        SetDetailLevel(AgentDetailLevel.Definition);
        _detailLoadedAgentId = detail.AgentId;
        _detailLevelField.Text = "Global Definition";
        _detailAgentIdField.Text = detail.AgentId;
        _detailDisplayNameField.Text = detail.DisplayName;
        _detailBuiltInField.CheckedState = detail.IsBuiltIn ? CheckState.Checked : CheckState.UnChecked;
        _detailWorkspaceField.Text = "(global defaults)";
        _detailEnabledField.CheckedState = CheckState.Checked;
        _detailIsolationField.Text = "worktree";
        _detailBannedField.CheckedState = CheckState.UnChecked;
        _detailBannedReasonField.Text = "";
        _detailLaunchCommandOverrideField.Text = detail.DefaultLaunchCommand;
        _detailModelsOverrideField.Text = JoinCsv(detail.DefaultModels);
        _detailBranchStrategyOverrideField.Text = detail.DefaultBranchStrategy;
        _detailInstructionFilesOverrideField.Text = detail.DefaultInstructionFile;
        _detailSeedPromptOverrideView.Text = detail.DefaultSeedPrompt;
        _detailMarkerAdditionsView.Text = "";
        ApplyDetailEditability();
        SetNeedsDraw();
        SuperView?.SetNeedsDraw();
    }

    private void ClearDetailEditor()
    {
        TraceUi("clear-detail-editor");
        SetDetailLevel(AgentDetailLevel.None);
        _detailLoadedAgentId = null;
        _detailLevelField.Text = "";
        _detailAgentIdField.Text = "";
        _detailDisplayNameField.Text = "";
        _detailBuiltInField.CheckedState = CheckState.UnChecked;
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
        ApplyDetailEditability();
        SetNeedsDraw();
        SuperView?.SetNeedsDraw();
    }

    private void SetDetailLevel(AgentDetailLevel level)
    {
        _detailLevel = level;
        _detailFrame.Title = level switch
        {
            AgentDetailLevel.Definition => "Agent Details (Global Definition)",
            AgentDetailLevel.WorkspaceAssignment => "Agent Details (Workspace Assignment)",
            _ => "Agent Details",
        };
    }

    private void ApplyDetailEditability()
    {
        var definitionMode = _detailLevel == AgentDetailLevel.Definition;
        var workspaceMode = _detailLevel == AgentDetailLevel.WorkspaceAssignment;

        _detailDisplayNameField.ReadOnly = !definitionMode;

        _detailWorkspaceField.ReadOnly = true;
        _detailEnabledField.Enabled = workspaceMode;
        _detailIsolationField.ReadOnly = true;
        _detailIsolationToggleBtn.Enabled = workspaceMode;
        _detailBannedField.Enabled = false;
        _detailBannedReasonField.ReadOnly = true;

        _detailLaunchCommandOverrideField.ReadOnly = !workspaceMode && !definitionMode;
        _detailModelsOverrideField.ReadOnly = !workspaceMode && !definitionMode;
        _detailBranchStrategyOverrideField.ReadOnly = !workspaceMode && !definitionMode;
        _detailInstructionFilesOverrideField.ReadOnly = !workspaceMode && !definitionMode;
        _detailSeedPromptOverrideView.ReadOnly = !workspaceMode && !definitionMode;

        var markerEditable = workspaceMode;
        _detailMarkerAdditionsView.ReadOnly = !markerEditable;
    }

    private string? TryGetDefinitionDisplayName(string agentId)
    {
        var row = _defRows.FirstOrDefault(x => string.Equals(x.Id, agentId, StringComparison.OrdinalIgnoreCase));
        return row?.DisplayName;
    }

    private bool TryIsBuiltInDefinition(string agentId)
    {
        var row = _defRows.FirstOrDefault(x => string.Equals(x.Id, agentId, StringComparison.OrdinalIgnoreCase));
        return string.Equals(row?.BuiltIn, "Yes", StringComparison.OrdinalIgnoreCase);
    }

    private void QueueSaveSelectedDetail()
    {
        if (_detailLevel == AgentDetailLevel.None)
        {
            SetStatus("Select an agent definition or workspace agent first");
            return;
        }

        if (_detailLevel == AgentDetailLevel.Definition)
        {
            QueueSaveDefinitionDetail();
            return;
        }

        AgentDetailSaveRequest? request = null;
        string? error = null;

        var agentId = (_detailAgentIdField.Text?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(agentId))
        {
            error = "Select a workspace agent row first";
        }
        else
        {
            var enabled = _detailEnabledField.CheckedState == CheckState.Checked;

            var isolation = NormalizeIsolationValue(_detailIsolationField.Text?.ToString());
            if (!string.Equals(isolation, "worktree", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(isolation, "clone", StringComparison.OrdinalIgnoreCase))
            {
                error = "Isolation must be one of: worktree, clone";
            }
            else
            {
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
            }
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetStatus(error);
            return;
        }

        if (request is null)
            return;

        _ = Task.Run(() => SaveSelectedDetailAsync(request));
    }

    private void QueueSaveDefinitionDetail()
    {
        var agentId = (_detailAgentIdField.Text?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(agentId))
        {
            SetStatus("Select an agent definition first");
            return;
        }

        var displayName = (_detailDisplayNameField.Text?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            SetStatus("Display Name is required for agent definitions");
            return;
        }

        var defaultModels = ParseCsvOrNull(_detailModelsOverrideField.Text?.ToString()) ?? [];
        var defaultInstructionFile = FirstCsvOrNull(_detailInstructionFilesOverrideField.Text?.ToString()) ?? "";

        var request = new AgentDefinitionSaveRequest(
            agentId,
            displayName,
            _detailLaunchCommandOverrideField.Text?.ToString() ?? "",
            defaultInstructionFile,
            defaultModels,
            _detailBranchStrategyOverrideField.Text?.ToString() ?? "",
            _detailSeedPromptOverrideView.Text?.ToString() ?? "");

        _ = Task.Run(() => SaveDefinitionDetailAsync(request));
    }

    private async Task SaveSelectedDetailAsync(AgentDetailSaveRequest request)
    {
        SetStatus($"Saving detail for '{request.AgentId}'...");
        try
        {
            var workspacePath = GetRequiredActiveWorkspacePath();
            await _handler.SaveWorkspaceAgentAsync(workspacePath, request).ConfigureAwait(false);
            SetStatus($"Workspace agent '{request.AgentId}' updated");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(request.AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Save detail failed: {ex.Message}");
        }
    }

    private async Task SaveDefinitionDetailAsync(AgentDefinitionSaveRequest request)
    {
        SetStatus($"Saving global definition '{request.AgentId}'...");
        try
        {
            await _handler.SaveDefinitionAsync(request).ConfigureAwait(false);
            await LoadDefinitionsAsync().ConfigureAwait(false);
            SetStatus($"Global definition '{request.AgentId}' updated");
            QueueDefinitionDetailRefresh(request.AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Save global definition failed: {ex.Message}");
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
            var workspacePath = GetRequiredActiveWorkspacePath();
            await _handler.AssignWorkspaceAgentAsync(workspacePath, agentId).ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' assigned to workspace");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
                    var workspacePath = GetRequiredActiveWorkspacePath();
                    await _handler.BanWorkspaceAgentAsync(workspacePath, agentId, reasonField.Text?.ToString() ?? "").ConfigureAwait(false);
                    SetStatus($"Agent '{agentId}' banned");
                    await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
                    QueueAgentDetailRefresh(agentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            var workspacePath = GetRequiredActiveWorkspacePath();
            await _handler.UnbanWorkspaceAgentAsync(workspacePath, agentId).ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' unbanned");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            var workspacePath = GetRequiredActiveWorkspacePath();
            await _handler.DeleteWorkspaceAgentAsync(workspacePath, agentId).ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' removed");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Delete failed: {ex.Message}");
        }
    }

    private async Task ValidateAsync()
    {
        SetStatus("Validating agents.yaml...");
        try
        {
            var workspacePath = GetRequiredActiveWorkspacePath();
            var result = await _handler.ValidateWorkspaceAgentsAsync(workspacePath).ConfigureAwait(false);
            SetStatus(result.Valid
                ? "agents.yaml is valid"
                : $"Validation failed: {result.Error ?? "unknown"}");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Validate failed: {ex.Message}");
        }
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);

    private async Task AddAgentToCurrentWorkspaceAsync(string agentId)
    {
        SetStatus($"Adding {agentId}...");
        try
        {
            var workspacePath = GetRequiredActiveWorkspacePath();
            await _handler.AssignWorkspaceAgentAsync(workspacePath, agentId).ConfigureAwait(false);
            SetStatus($"Agent '{agentId}' added");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
            QueueAgentDetailRefresh(agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            await _handler.CreateDefinitionAsync(agentId).ConfigureAwait(false);
            if (refreshDefinitions)
                await LoadDefinitionsAsync().ConfigureAwait(false);
            SetStatus($"Definition '{agentId}' created");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Create definition failed: {ex.Message}");
        }
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

    private static string? FirstCsvOrNull(string? value)
    {
        var items = ParseCsvOrNull(value);
        return items is null || items.Length == 0 ? null : items[0];
    }

    private sealed record AgentDefRow(string Id, string DisplayName, string BuiltIn);
    private sealed record AgentRow(string AgentId, string Enabled, string Banned, string Isolation);
    private enum AgentDetailLevel
    {
        None,
        Definition,
        WorkspaceAssignment
    }

    private static void TraceUi(string message)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("DIRECTOR_AGENT_TRACE"), "1", StringComparison.Ordinal))
            return;

        try
        {
            var path = Path.Combine(Path.GetTempPath(), "director-agent-screen-trace.log");
            var line = $"{DateTimeOffset.Now:O} [AgentScreen] {message}{Environment.NewLine}";
            lock (s_traceSync)
            {
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Ignore trace failures. This is diagnostic-only instrumentation.
        }
    }
}
