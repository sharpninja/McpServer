using System.Text.Json;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for agent management: definitions, workspace agents, and events.
/// Covers: agents defs, agents ws, agents events, add, ban, unban, delete, validate.
/// </summary>
internal sealed class AgentScreen : View
{
    private readonly McpHttpClient _client;
    private TableView _defsTable = null!;
    private TableView _agentsTable = null!;
    private Label _statusLabel = null!;
    private List<AgentRow> _agentRows = [];

    public AgentScreen(McpHttpClient client)
    {
        _client = client;
        Title = "Agents";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        // Top half: Agent Definitions
        var defsFrame = new FrameView
        {
            Title = "Agent Definitions",
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(40),
        };
        _defsTable = new TableView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), FullRowSelect = true };
        defsFrame.Add(_defsTable);
        Add(defsFrame);

        // Bottom half: Workspace Agents
        var agentsFrame = new FrameView
        {
            Title = "Workspace Agents",
            X = 0, Y = Pos.Bottom(defsFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
        };
        _agentsTable = new TableView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), FullRowSelect = true };
        agentsFrame.Add(_agentsTable);
        Add(agentsFrame);

        // Status + buttons
        _statusLabel = new Label { X = 0, Y = Pos.AnchorEnd(2), Width = Dim.Fill(), Text = "" };
        Add(_statusLabel);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAllAsync);

        var addBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Add Agent" };
        addBtn.Accepting += (_, _) => ShowAddDialog();

        var banBtn = new Button { X = Pos.Right(addBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Ban" };
        banBtn.Accepting += (_, _) => ShowBanDialog();

        var unbanBtn = new Button { X = Pos.Right(banBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Unban" };
        unbanBtn.Accepting += (_, _) => _ = Task.Run(UnbanSelectedAsync);

        var deleteBtn = new Button { X = Pos.Right(unbanBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Delete" };
        deleteBtn.Accepting += (_, _) => _ = Task.Run(DeleteSelectedAsync);

        var validateBtn = new Button { X = Pos.Right(deleteBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Validate" };
        validateBtn.Accepting += (_, _) => _ = Task.Run(ValidateAsync);

        Add(refreshBtn, addBtn, banBtn, unbanBtn, deleteBtn, validateBtn);
    }

    public async Task LoadAllAsync()
    {
        await LoadDefinitionsAsync().ConfigureAwait(false);
        await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
    }

    private async Task LoadDefinitionsAsync()
    {
        SetStatus("⏳ Loading definitions...");
        try
        {
            var result = await _client.GetAsync<JsonElement>("/mcp/agents/definitions").ConfigureAwait(false);
            var items = result.GetProperty("items");
            var rows = new List<AgentDefRow>();
            foreach (var item in items.EnumerateArray())
            {
                rows.Add(new AgentDefRow(
                    item.GetProperty("id").GetString() ?? "",
                    item.GetProperty("displayName").GetString() ?? "",
                    item.TryGetProperty("isBuiltIn", out var bi) && bi.GetBoolean() ? "Yes" : "No"));
            }
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
            SetStatus($"✓ {rows.Count} definitions loaded");
        }
        catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
    }

    private async Task LoadWorkspaceAgentsAsync()
    {
        try
        {
            var path = Uri.EscapeDataString(_client.WorkspacePath);
            var result = await _client.GetAsync<JsonElement>($"/mcp/agents?workspace={path}").ConfigureAwait(false);
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
            });
        }
        catch { /* definitions load already showed status */ }
    }

    private string? GetSelectedAgentId()
    {
        var row = _agentsTable.SelectedRow;
        if (row >= 0 && row < _agentRows.Count)
            return _agentRows[row].AgentId;
        return null;
    }

    private void ShowAddDialog()
    {
        var dlg = new Dialog { Title = "Add Agent", Width = 50, Height = 10 };
        var idLabel = new Label { X = 1, Y = 1, Text = "Agent ID:" };
        var idField = new TextField { X = 12, Y = 1, Width = 30, Text = "" };
        dlg.Add(idLabel, idField);

        var okBtn = new Button { Text = "Add" };
        okBtn.Accepting += (_, _) =>
        {
            var agentId = idField.Text ?? "";
            if (!string.IsNullOrWhiteSpace(agentId))
            {
                Application.RequestStop();
                _ = Task.Run(async () =>
                {
                    SetStatus($"⏳ Adding {agentId}...");
                    try
                    {
                        var path = Uri.EscapeDataString(_client.WorkspacePath);
                        var body = new { agentId, enabled = true, agentIsolation = "worktree" };
                        await _client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}?workspace={path}", body).ConfigureAwait(false);
                        SetStatus($"✓ Agent '{agentId}' added");
                        await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
                });
            }
        };
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(okBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowBanDialog()
    {
        var agentId = GetSelectedAgentId();
        if (agentId is null) { SetStatus("Select an agent first"); return; }

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
                SetStatus($"⏳ Banning {agentId}...");
                try
                {
                    var path = Uri.EscapeDataString(_client.WorkspacePath);
                    var body = new { reason = reasonField.Text ?? "", global = false };
                    await _client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}/ban?workspace={path}", body).ConfigureAwait(false);
                    SetStatus($"✓ Agent '{agentId}' banned");
                    await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
                }
                catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
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
        if (agentId is null) { SetStatus("Select an agent first"); return; }
        SetStatus($"⏳ Unbanning {agentId}...");
        try
        {
            var path = Uri.EscapeDataString(_client.WorkspacePath);
            await _client.PostAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}/unban?workspace={path}&global=false").ConfigureAwait(false);
            SetStatus($"✓ Agent '{agentId}' unbanned");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
    }

    private async Task DeleteSelectedAsync()
    {
        var agentId = GetSelectedAgentId();
        if (agentId is null) { SetStatus("Select an agent first"); return; }
        SetStatus($"⏳ Deleting {agentId}...");
        try
        {
            var path = Uri.EscapeDataString(_client.WorkspacePath);
            await _client.DeleteAsync<JsonElement>($"/mcp/agents/{Uri.EscapeDataString(agentId)}?workspace={path}").ConfigureAwait(false);
            SetStatus($"✓ Agent '{agentId}' removed");
            await LoadWorkspaceAgentsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
    }

    private async Task ValidateAsync()
    {
        SetStatus("⏳ Validating...");
        try
        {
            var path = Uri.EscapeDataString(_client.WorkspacePath);
            var result = await _client.GetAsync<JsonElement>($"/mcp/agents/validate?workspace={path}").ConfigureAwait(false);
            var valid = result.TryGetProperty("valid", out var v) && v.GetBoolean();
            SetStatus(valid ? "✓ agents.yaml is valid" : $"✗ Validation failed: {(result.TryGetProperty("error", out var e) ? e.GetString() : "unknown")}");
        }
        catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);

    private sealed record AgentDefRow(string Id, string DisplayName, string BuiltIn);
    private sealed record AgentRow(string AgentId, string Enabled, string Banned, string Isolation);
}
