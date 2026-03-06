using System.Text;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for agent definition and workspace-assignment management.
/// Uses UI.Core ViewModels and CQRS only (no direct HTTP calls).
/// </summary>
internal sealed class AgentScreen : View
{
    private readonly AgentDefinitionListViewModel _definitionListVm;
    private readonly AgentDefinitionDetailViewModel _definitionDetailVm;
    private readonly WorkspaceAgentListViewModel _workspaceAgentListVm;
    private readonly WorkspaceAgentDetailViewModel _workspaceAgentDetailVm;
    private readonly AgentEventsViewModel _eventsVm;
    private readonly ILogger<AgentScreen> _logger;

    private readonly List<AgentDefinitionSummaryItem> _definitionRows = [];
    private readonly List<WorkspaceAgentItem> _workspaceRows = [];

    private TableView _defsTable = null!;
    private TableView _agentsTable = null!;
    private TextView _statusLabel = null!;
    private TextView _detailView = null!;

    public AgentScreen(
        AgentDefinitionListViewModel definitionListVm,
        AgentDefinitionDetailViewModel definitionDetailVm,
        WorkspaceAgentListViewModel workspaceAgentListVm,
        WorkspaceAgentDetailViewModel workspaceAgentDetailVm,
        AgentEventsViewModel eventsVm,
        ILogger<AgentScreen>? logger = null)
    {
        _definitionListVm = definitionListVm;
        _definitionDetailVm = definitionDetailVm;
        _workspaceAgentListVm = workspaceAgentListVm;
        _workspaceAgentDetailVm = workspaceAgentDetailVm;
        _eventsVm = eventsVm;
        _logger = logger ?? NullLogger<AgentScreen>.Instance;

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
            Width = Dim.Percent(55),
            Height = Dim.Fill(3),
        };
        Add(leftPane);

        var detailFrame = new FrameView
        {
            Title = "Detail",
            X = Pos.Right(leftPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
        };
        Add(detailFrame);

        var defsFrame = new FrameView
        {
            Title = "Global Definitions",
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
        _defsTable.SelectedCellChanged += (_, _) => _ = Task.Run(RefreshDefinitionDetailAsync);
        _defsTable.KeyDown += (_, e) =>
        {
            if (e.KeyCode != KeyCode.Enter)
                return;
            _ = Task.Run(AssignSelectedDefinitionAsync);
            e.Handled = true;
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
        _agentsTable.SelectedCellChanged += (_, _) => _ = Task.Run(RefreshWorkspaceDetailAsync);
        agentsFrame.Add(_agentsTable);
        leftPane.Add(agentsFrame);

        _detailView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Text = "Select a definition or workspace agent to view details.",
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

        var assignBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Assign Selected" };
        assignBtn.Accepting += (_, _) => _ = Task.Run(AssignSelectedDefinitionAsync);

        var addBtn = new Button { X = Pos.Right(assignBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Add by ID" };
        addBtn.Accepting += (_, _) => ShowAddDialog();

        var editDefBtn = new Button { X = Pos.Right(addBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Edit Def" };
        editDefBtn.Accepting += (_, _) => ShowEditDefinitionDialog();

        var editAgentBtn = new Button { X = Pos.Right(editDefBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Edit Agent" };
        editAgentBtn.Accepting += (_, _) => ShowEditWorkspaceAgentDialog();

        var banBtn = new Button { X = Pos.Right(editAgentBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Ban" };
        banBtn.Accepting += (_, _) => ShowBanDialog();

        var unbanBtn = new Button { X = Pos.Right(banBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Unban" };
        unbanBtn.Accepting += (_, _) => _ = Task.Run(UnbanSelectedAsync);

        var deleteBtn = new Button { X = Pos.Right(unbanBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedAsync);

        var validateBtn = new Button { X = Pos.Right(deleteBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Validate" };
        validateBtn.Accepting += (_, _) => _ = Task.Run(ValidateAsync);

        var eventsBtn = new Button { X = Pos.Right(validateBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Events" };
        eventsBtn.Accepting += (_, _) => _ = Task.Run(ShowEventsDialogAsync);

        Add(refreshBtn, assignBtn, addBtn, editDefBtn, editAgentBtn, banBtn, unbanBtn, deleteBtn, validateBtn, eventsBtn);
    }

    public async Task LoadAllAsync()
    {
        SetStatus("Loading agent data...");
        await LoadDefinitionsAsync().ConfigureAwait(true);
        await LoadWorkspaceAgentsAsync().ConfigureAwait(true);

        if (GetSelectedWorkspaceAgent() is not null)
            await RefreshWorkspaceDetailAsync().ConfigureAwait(true);
        else if (GetSelectedDefinition() is not null)
            await RefreshDefinitionDetailAsync().ConfigureAwait(true);
        else
            SetDetail("No definitions or workspace agents are currently loaded.");
    }

    private async Task LoadDefinitionsAsync()
    {
        try
        {
            await _definitionListVm.LoadAsync().ConfigureAwait(true);
            Application.Invoke(() =>
            {
                _definitionRows.Clear();
                _definitionRows.AddRange(_definitionListVm.Items);
                _defsTable.Table = new EnumerableTableSource<AgentDefinitionSummaryItem>(
                    _definitionRows,
                    new Dictionary<string, Func<AgentDefinitionSummaryItem, object>>
                    {
                        ["ID"] = d => d.Id,
                        ["Display Name"] = d => d.DisplayName,
                        ["Built-In"] = d => d.IsBuiltIn ? "Yes" : "No",
                    });

                if (_definitionRows.Count > 0 &&
                    (_defsTable.SelectedRow < 0 || _defsTable.SelectedRow >= _definitionRows.Count))
                {
                    _defsTable.SelectedRow = 0;
                }
            });

            if (!string.IsNullOrWhiteSpace(_definitionListVm.ErrorMessage))
                SetStatus(_definitionListVm.ErrorMessage);
            else
                SetStatus(_definitionListVm.StatusMessage ?? $"Loaded {_definitionRows.Count} definitions.");
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
            await _workspaceAgentListVm.LoadAsync().ConfigureAwait(true);
            Application.Invoke(() =>
            {
                _workspaceRows.Clear();
                _workspaceRows.AddRange(_workspaceAgentListVm.Items);
                _agentsTable.Table = new EnumerableTableSource<WorkspaceAgentItem>(
                    _workspaceRows,
                    new Dictionary<string, Func<WorkspaceAgentItem, object>>
                    {
                        ["Agent ID"] = a => a.AgentId,
                        ["Enabled"] = a => a.Enabled ? "Yes" : "No",
                        ["Banned"] = a => a.Banned ? "Yes" : "No",
                        ["Isolation"] = a => a.AgentIsolation,
                    });

                if (_workspaceRows.Count > 0 &&
                    (_agentsTable.SelectedRow < 0 || _agentsTable.SelectedRow >= _workspaceRows.Count))
                {
                    _agentsTable.SelectedRow = 0;
                }
            });

            if (!string.IsNullOrWhiteSpace(_workspaceAgentListVm.ErrorMessage))
                SetStatus(_workspaceAgentListVm.ErrorMessage);
            else
                SetStatus(_workspaceAgentListVm.StatusMessage ?? $"Loaded {_workspaceRows.Count} workspace agents.");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Load workspace agents failed: {ex.Message}");
        }
    }

    private AgentDefinitionSummaryItem? GetSelectedDefinition()
    {
        var row = _defsTable.SelectedRow;
        return row >= 0 && row < _definitionRows.Count ? _definitionRows[row] : null;
    }

    private WorkspaceAgentItem? GetSelectedWorkspaceAgent()
    {
        var row = _agentsTable.SelectedRow;
        return row >= 0 && row < _workspaceRows.Count ? _workspaceRows[row] : null;
    }

    private async Task RefreshDefinitionDetailAsync()
    {
        var selected = GetSelectedDefinition();
        if (selected is null)
            return;

        var detail = await _definitionDetailVm.LoadAsync(selected.Id).ConfigureAwait(true);
        if (detail is null)
        {
            SetStatus(_definitionDetailVm.ErrorMessage ?? $"Definition '{selected.Id}' not found.");
            return;
        }

        SetDetail(FormatDefinitionDetail(detail));
        SetStatus(_definitionDetailVm.StatusMessage ?? $"Loaded definition '{detail.Id}'.");
    }

    private async Task RefreshWorkspaceDetailAsync()
    {
        var selected = GetSelectedWorkspaceAgent();
        if (selected is null)
            return;

        var detail = await _workspaceAgentDetailVm.LoadAsync(selected.AgentId, selected.WorkspacePath).ConfigureAwait(true);
        if (detail is null)
        {
            SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? $"Workspace agent '{selected.AgentId}' not found.");
            return;
        }

        SetDetail(FormatWorkspaceDetail(detail));
        SetStatus(_workspaceAgentDetailVm.StatusMessage ?? $"Loaded workspace agent '{detail.AgentId}'.");
    }

    private async Task AssignSelectedDefinitionAsync()
    {
        var selected = GetSelectedDefinition();
        if (selected is null)
        {
            SetStatus("Select a definition first.");
            return;
        }

        var outcome = await _workspaceAgentDetailVm.AssignAsync(selected.Id).ConfigureAwait(true);
        if (outcome is not { Success: true })
        {
            SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? outcome?.Error ?? "Assign failed.");
            return;
        }

        await LoadWorkspaceAgentsAsync().ConfigureAwait(true);
        SelectWorkspaceAgent(selected.Id);
        await RefreshWorkspaceDetailAsync().ConfigureAwait(true);
        SetStatus($"Assigned '{selected.Id}' to active workspace.");
    }

    private void ShowAddDialog()
    {
        var dlg = new Dialog
        {
            Title = "Add Agent by ID",
            Width = 64,
            Height = 11,
        };

        var idLabel = new Label { X = 1, Y = 1, Text = "Agent ID:" };
        var idField = new TextField { X = 12, Y = 1, Width = 46, Text = "" };
        dlg.Add(idLabel, idField);

        var addBtn = new Button { Text = "Assign" };
        addBtn.Accepting += (_, _) =>
        {
            var agentId = idField.Text?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(agentId))
                return;

            Application.RequestStop();
            _ = Task.Run(() => AddByIdAsync(agentId, createDefinition: false));
        };

        var createAndAddBtn = new Button { Text = "Create+Assign" };
        createAndAddBtn.Accepting += (_, _) =>
        {
            var agentId = idField.Text?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(agentId))
                return;

            Application.RequestStop();
            _ = Task.Run(() => AddByIdAsync(agentId, createDefinition: true));
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();

        dlg.AddButton(addBtn);
        dlg.AddButton(createAndAddBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task AddByIdAsync(string agentId, bool createDefinition)
    {
        var normalized = (agentId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (createDefinition)
        {
            var createOutcome = await _definitionDetailVm.CreateBasicAsync(normalized).ConfigureAwait(true);
            if (createOutcome is not { Success: true })
            {
                SetStatus(_definitionDetailVm.ErrorMessage ?? createOutcome?.Error ?? "Create definition failed.");
                return;
            }

            await LoadDefinitionsAsync().ConfigureAwait(true);
        }

        var assignOutcome = await _workspaceAgentDetailVm.AssignAsync(normalized).ConfigureAwait(true);
        if (assignOutcome is not { Success: true })
        {
            SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? assignOutcome?.Error ?? "Assign failed.");
            return;
        }

        await LoadWorkspaceAgentsAsync().ConfigureAwait(true);
        SelectWorkspaceAgent(normalized);
        await RefreshWorkspaceDetailAsync().ConfigureAwait(true);
        SetStatus($"Assigned '{normalized}'.");
    }

    private void ShowEditDefinitionDialog()
    {
        var selected = GetSelectedDefinition();
        if (selected is null)
        {
            SetStatus("Select a definition first.");
            return;
        }

        _ = Task.Run(async () =>
        {
            var detail = await _definitionDetailVm.LoadAsync(selected.Id).ConfigureAwait(true);
            if (detail is null)
            {
                SetStatus(_definitionDetailVm.ErrorMessage ?? "Definition not found.");
                return;
            }

            Application.Invoke(() => OpenDefinitionEditor(detail));
        });
    }

    private void OpenDefinitionEditor(AgentDefinitionDetail detail)
    {
        var dlg = new Dialog
        {
            Title = $"Edit Definition: {detail.Id}",
            Width = 92,
            Height = 21,
        };

        var row = 1;
        dlg.Add(new Label { X = 1, Y = row, Text = "ID:" });
        var idField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.Id, ReadOnly = true };
        dlg.Add(idField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Display Name:" });
        var displayNameField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.DisplayName };
        dlg.Add(displayNameField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Launch Cmd:" });
        var launchField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.DefaultLaunchCommand };
        dlg.Add(launchField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Instruction File:" });
        var instructionField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.DefaultInstructionFile };
        dlg.Add(instructionField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Models CSV:" });
        var modelsField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = string.Join(", ", detail.DefaultModels) };
        dlg.Add(modelsField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Branch Strategy:" });
        var branchField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.DefaultBranchStrategy };
        dlg.Add(branchField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Seed Prompt:" });
        var seedView = new TextView { X = 20, Y = row, Width = Dim.Fill(2), Height = 4, Text = detail.DefaultSeedPrompt, WordWrap = true };
        dlg.Add(seedView);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var command = new UpsertAgentDefinitionCommand
            {
                Id = detail.Id,
                DisplayName = displayNameField.Text?.ToString()?.Trim() ?? detail.Id,
                DefaultLaunchCommand = launchField.Text?.ToString() ?? string.Empty,
                DefaultInstructionFile = instructionField.Text?.ToString() ?? string.Empty,
                DefaultModels = ParseCsv(modelsField.Text?.ToString()),
                DefaultBranchStrategy = branchField.Text?.ToString() ?? string.Empty,
                DefaultSeedPrompt = seedView.Text?.ToString() ?? string.Empty,
            };

            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _definitionDetailVm.UpsertAsync(command).ConfigureAwait(true);
                if (outcome is not { Success: true })
                {
                    SetStatus(_definitionDetailVm.ErrorMessage ?? outcome?.Error ?? "Save failed.");
                    return;
                }

                await LoadDefinitionsAsync().ConfigureAwait(true);
                SelectDefinition(command.Id);
                await RefreshDefinitionDetailAsync().ConfigureAwait(true);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowEditWorkspaceAgentDialog()
    {
        var selected = GetSelectedWorkspaceAgent();
        if (selected is null)
        {
            SetStatus("Select a workspace agent first.");
            return;
        }

        _ = Task.Run(async () =>
        {
            var detail = await _workspaceAgentDetailVm.LoadAsync(selected.AgentId, selected.WorkspacePath).ConfigureAwait(true);
            if (detail is null)
            {
                SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? "Workspace detail not found.");
                return;
            }

            Application.Invoke(() => OpenWorkspaceAgentEditor(detail));
        });
    }

    private void OpenWorkspaceAgentEditor(WorkspaceAgentDetail detail)
    {
        var dlg = new Dialog
        {
            Title = $"Edit Workspace Agent: {detail.AgentId}",
            Width = 96,
            Height = 25,
        };

        var row = 1;
        dlg.Add(new Label { X = 1, Y = row, Text = "Agent ID:" });
        var idField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.AgentId, ReadOnly = true };
        dlg.Add(idField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Workspace:" });
        var workspaceField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.WorkspacePath, ReadOnly = true };
        dlg.Add(workspaceField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Enabled:" });
        var enabledField = new CheckBox { X = 20, Y = row, CheckedState = detail.Enabled ? CheckState.Checked : CheckState.UnChecked };
        dlg.Add(enabledField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Isolation:" });
        var isolationField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.AgentIsolation };
        dlg.Add(isolationField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Launch Cmd:" });
        var launchField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.LaunchCommandOverride ?? string.Empty };
        dlg.Add(launchField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Models CSV:" });
        var modelsField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = string.Join(", ", detail.ModelsOverride) };
        dlg.Add(modelsField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Branch Strategy:" });
        var branchField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = detail.BranchStrategyOverride ?? string.Empty };
        dlg.Add(branchField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Instructions CSV:" });
        var instructionsField = new TextField { X = 20, Y = row, Width = Dim.Fill(2), Text = string.Join(", ", detail.InstructionFilesOverride) };
        dlg.Add(instructionsField);
        row++;

        dlg.Add(new Label { X = 1, Y = row, Text = "Seed Prompt:" });
        var seedView = new TextView { X = 20, Y = row, Width = Dim.Fill(2), Height = 3, Text = detail.SeedPromptOverride ?? string.Empty, WordWrap = true };
        dlg.Add(seedView);
        row += 3;

        dlg.Add(new Label { X = 1, Y = row, Text = "Marker Additions:" });
        var markerView = new TextView { X = 20, Y = row, Width = Dim.Fill(2), Height = 4, Text = detail.MarkerAdditions, WordWrap = true };
        dlg.Add(markerView);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var command = new UpsertWorkspaceAgentCommand
            {
                AgentId = detail.AgentId,
                WorkspacePath = detail.WorkspacePath,
                Enabled = enabledField.CheckedState == CheckState.Checked,
                AgentIsolation = NormalizeIsolation(isolationField.Text?.ToString()),
                LaunchCommandOverride = NullIfWhitespace(launchField.Text?.ToString()),
                ModelsOverride = ParseCsvOrNull(modelsField.Text?.ToString()),
                BranchStrategyOverride = NullIfWhitespace(branchField.Text?.ToString()),
                SeedPromptOverride = NullIfWhitespace(seedView.Text?.ToString()),
                MarkerAdditions = markerView.Text?.ToString() ?? string.Empty,
                InstructionFilesOverride = ParseCsvOrNull(instructionsField.Text?.ToString()),
            };

            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _workspaceAgentDetailVm.UpsertAsync(command).ConfigureAwait(true);
                if (outcome is not { Success: true })
                {
                    SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? outcome?.Error ?? "Save failed.");
                    return;
                }

                await LoadWorkspaceAgentsAsync().ConfigureAwait(true);
                SelectWorkspaceAgent(command.AgentId);
                await RefreshWorkspaceDetailAsync().ConfigureAwait(true);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowBanDialog()
    {
        var selected = GetSelectedWorkspaceAgent();
        if (selected is null)
        {
            SetStatus("Select a workspace agent first.");
            return;
        }

        var dlg = new Dialog
        {
            Title = $"Ban {selected.AgentId}",
            Width = 60,
            Height = 9,
        };

        var reasonLabel = new Label { X = 1, Y = 1, Text = "Reason:" };
        var reasonField = new TextField { X = 10, Y = 1, Width = 46, Text = "" };
        dlg.Add(reasonLabel, reasonField);

        var banBtn = new Button { Text = "Ban" };
        banBtn.Accepting += (_, _) =>
        {
            var reason = reasonField.Text?.ToString();
            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _workspaceAgentDetailVm.BanAsync(selected.AgentId, reason, selected.WorkspacePath).ConfigureAwait(true);
                if (outcome is not { Success: true })
                {
                    SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? outcome?.Error ?? "Ban failed.");
                    return;
                }

                await LoadWorkspaceAgentsAsync().ConfigureAwait(true);
                SelectWorkspaceAgent(selected.AgentId);
                await RefreshWorkspaceDetailAsync().ConfigureAwait(true);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(banBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task UnbanSelectedAsync()
    {
        var selected = GetSelectedWorkspaceAgent();
        if (selected is null)
        {
            SetStatus("Select a workspace agent first.");
            return;
        }

        var outcome = await _workspaceAgentDetailVm.UnbanAsync(selected.AgentId, selected.WorkspacePath).ConfigureAwait(true);
        if (outcome is not { Success: true })
        {
            SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? outcome?.Error ?? "Unban failed.");
            return;
        }

        await LoadWorkspaceAgentsAsync().ConfigureAwait(true);
        SelectWorkspaceAgent(selected.AgentId);
        await RefreshWorkspaceDetailAsync().ConfigureAwait(true);
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = GetSelectedWorkspaceAgent();
        if (selected is null)
        {
            SetStatus("Select a workspace agent first.");
            return;
        }

        var outcome = await _workspaceAgentDetailVm.DeleteAsync(selected.AgentId, selected.WorkspacePath).ConfigureAwait(true);
        if (outcome is not { Success: true })
        {
            SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? outcome?.Error ?? "Delete failed.");
            return;
        }

        await LoadWorkspaceAgentsAsync().ConfigureAwait(true);
        SetDetail("Workspace agent deleted.");
    }

    private async Task ValidateAsync()
    {
        var outcome = await _workspaceAgentDetailVm.ValidateAsync().ConfigureAwait(true);
        if (outcome is null)
        {
            SetStatus(_workspaceAgentDetailVm.ErrorMessage ?? "Validation failed.");
            return;
        }

        SetStatus(outcome.Valid
            ? $"agents.yaml is valid ({outcome.Path ?? "no path reported"})."
            : $"Validation failed: {outcome.Error ?? "unknown"}");
    }

    private async Task ShowEventsDialogAsync()
    {
        var selectedAgent = GetSelectedWorkspaceAgent()?.AgentId ?? GetSelectedDefinition()?.Id;
        if (string.IsNullOrWhiteSpace(selectedAgent))
        {
            SetStatus("Select an agent first.");
            return;
        }

        var result = await _eventsVm.LoadAsync(selectedAgent).ConfigureAwait(true);
        if (result is null)
        {
            SetStatus(_eventsVm.ErrorMessage ?? "Failed to load events.");
            return;
        }

        var text = new StringBuilder();
        foreach (var entry in result.Items)
            text.AppendLine($"{entry.Timestamp:O}  type={entry.EventType}  user={entry.UserId ?? "-"}  {entry.Details ?? string.Empty}");

        Application.Invoke(() =>
        {
            var dlg = new Dialog
            {
                Title = $"Events: {selectedAgent}",
                Width = 100,
                Height = 22,
            };

            var view = new TextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                ReadOnly = true,
                WordWrap = true,
                Text = text.Length == 0 ? "(No events)" : text.ToString(),
            };
            dlg.Add(view);

            var closeBtn = new Button { Text = "Close" };
            closeBtn.Accepting += (_, _) => Application.RequestStop();
            dlg.AddButton(closeBtn);
            Application.Run(dlg);
        });
    }

    private void SelectDefinition(string agentId)
    {
        var idx = _definitionRows.FindIndex(d => string.Equals(d.Id, agentId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            Application.Invoke(() => _defsTable.SelectedRow = idx);
    }

    private void SelectWorkspaceAgent(string agentId)
    {
        var idx = _workspaceRows.FindIndex(a => string.Equals(a.AgentId, agentId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            Application.Invoke(() => _agentsTable.SelectedRow = idx);
    }

    private static string FormatDefinitionDetail(AgentDefinitionDetail detail)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Global Definition");
        sb.AppendLine($"ID: {detail.Id}");
        sb.AppendLine($"Display Name: {detail.DisplayName}");
        sb.AppendLine($"Built-In: {(detail.IsBuiltIn ? "Yes" : "No")}");
        sb.AppendLine($"Launch Command: {detail.DefaultLaunchCommand}");
        sb.AppendLine($"Instruction File: {detail.DefaultInstructionFile}");
        sb.AppendLine($"Models: {string.Join(", ", detail.DefaultModels)}");
        sb.AppendLine($"Branch Strategy: {detail.DefaultBranchStrategy}");
        sb.AppendLine("Seed Prompt:");
        sb.AppendLine(detail.DefaultSeedPrompt);
        return sb.ToString().TrimEnd();
    }

    private static string FormatWorkspaceDetail(WorkspaceAgentDetail detail)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Workspace Assignment");
        sb.AppendLine($"Agent: {detail.AgentId}");
        sb.AppendLine($"Workspace: {detail.WorkspacePath}");
        sb.AppendLine($"Enabled: {(detail.Enabled ? "Yes" : "No")}");
        sb.AppendLine($"Banned: {(detail.Banned ? "Yes" : "No")}");
        sb.AppendLine($"Ban Reason: {detail.BannedReason ?? string.Empty}");
        sb.AppendLine($"Isolation: {detail.AgentIsolation}");
        sb.AppendLine($"Launch Override: {detail.LaunchCommandOverride ?? string.Empty}");
        sb.AppendLine($"Models Override: {string.Join(", ", detail.ModelsOverride)}");
        sb.AppendLine($"Branch Override: {detail.BranchStrategyOverride ?? string.Empty}");
        sb.AppendLine($"Instruction Overrides: {string.Join(", ", detail.InstructionFilesOverride)}");
        sb.AppendLine("Seed Prompt Override:");
        sb.AppendLine(detail.SeedPromptOverride ?? string.Empty);
        sb.AppendLine("Marker Additions:");
        sb.AppendLine(detail.MarkerAdditions);
        return sb.ToString().TrimEnd();
    }

    private void SetStatus(string text)
        => Application.Invoke(() => _statusLabel.Text = text);

    private void SetDetail(string text)
        => Application.Invoke(() => _detailView.Text = text);

    private static string NormalizeIsolation(string? raw)
    {
        var value = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return value == "clone" ? "clone" : "worktree";
    }

    private static string? NullIfWhitespace(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

    private static IReadOnlyList<string> ParseCsv(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

    private static IReadOnlyList<string>? ParseCsvOrNull(string? raw)
    {
        var values = ParseCsv(raw);
        return values.Count == 0 ? null : values;
    }
}
