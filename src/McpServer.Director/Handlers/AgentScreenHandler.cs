using System.Text.Json;

namespace McpServer.Director.Handlers;

/// <summary>
/// Handles agent-management data access and request payload construction for the Director Agent screen.
/// Keeps HTTP and JSON parsing out of the Terminal.Gui view layer.
/// </summary>
internal sealed class AgentScreenHandler
{
    private readonly DirectorMcpContext _context;

    public AgentScreenHandler(DirectorMcpContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AgentDefinitionSummary>> ListDefinitionsAsync(CancellationToken ct = default)
    {
        var client = GetAgentDefinitionsHttpClient();
        var result = await client.GetAsync<JsonElement>("/mcpserver/agents/definitions", ct).ConfigureAwait(false);
        var items = result.GetProperty("items");
        var rows = new List<AgentDefinitionSummary>();
        foreach (var item in items.EnumerateArray())
        {
            rows.Add(new AgentDefinitionSummary(
                item.GetProperty("id").GetString() ?? "",
                item.GetProperty("displayName").GetString() ?? "",
                item.TryGetProperty("isBuiltIn", out var bi) && bi.GetBoolean()));
        }

        return rows;
    }

    public async Task<IReadOnlyList<WorkspaceAgentSummary>> ListWorkspaceAgentsAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
        var result = await client.GetAsync<JsonElement>($"/mcpserver/agents?workspace={path}", ct).ConfigureAwait(false);
        var items = result.GetProperty("items");
        var rows = new List<WorkspaceAgentSummary>();
        foreach (var item in items.EnumerateArray())
        {
            rows.Add(new WorkspaceAgentSummary(
                item.GetProperty("agentId").GetString() ?? "",
                item.TryGetProperty("enabled", out var en) && en.GetBoolean(),
                item.TryGetProperty("banned", out var b) && b.GetBoolean(),
                item.TryGetProperty("agentIsolation", out var iso) ? iso.GetString() ?? "worktree" : "worktree"));
        }

        return rows;
    }

    public async Task<AgentDetailState> GetWorkspaceAgentDetailAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
        var result = await client.GetAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}?workspace={path}", ct).ConfigureAwait(false);

        return new AgentDetailState
        {
            AgentId = GetString(result, "agentId") ?? agentId,
            WorkspacePath = GetString(result, "workspacePath") ?? workspacePath,
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
    }

    public async Task<AgentDefinitionDetailState> GetDefinitionDetailAsync(string agentId, CancellationToken ct = default)
    {
        var client = GetAgentDefinitionsHttpClient();
        var result = await client.GetAsync<JsonElement>($"/mcpserver/agents/definitions/{Uri.EscapeDataString(agentId)}", ct).ConfigureAwait(false);

        return new AgentDefinitionDetailState
        {
            AgentId = GetString(result, "id") ?? agentId,
            DisplayName = GetString(result, "displayName") ?? agentId,
            IsBuiltIn = GetBool(result, "isBuiltIn"),
            DefaultLaunchCommand = GetString(result, "defaultLaunchCommand") ?? "",
            DefaultModels = GetStringArray(result, "defaultModels"),
            DefaultBranchStrategy = GetString(result, "defaultBranchStrategy") ?? "",
            DefaultSeedPrompt = GetString(result, "defaultSeedPrompt") ?? "",
            DefaultInstructionFile = GetString(result, "defaultInstructionFile") ?? "",
        };
    }

    public async Task SaveWorkspaceAgentAsync(string workspacePath, AgentDetailSaveRequest request, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
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

        await client.PostAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(request.AgentId)}?workspace={path}", body, ct)
            .ConfigureAwait(false);
    }

    public async Task SaveDefinitionAsync(AgentDefinitionSaveRequest request, CancellationToken ct = default)
    {
        var client = GetAgentDefinitionsHttpClient();
        var body = new
        {
            id = request.AgentId,
            displayName = request.DisplayName,
            defaultLaunchCommand = request.DefaultLaunchCommand,
            defaultInstructionFile = request.DefaultInstructionFile,
            defaultModels = request.DefaultModels,
            defaultBranchStrategy = request.DefaultBranchStrategy,
            defaultSeedPrompt = request.DefaultSeedPrompt,
        };

        await client.PostAsync<JsonElement>("/mcpserver/agents/definitions", body, ct).ConfigureAwait(false);
    }

    public async Task AssignWorkspaceAgentAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
        var body = new { agentId, enabled = true, agentIsolation = "worktree" };
        await client.PostAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}?workspace={path}", body, ct)
            .ConfigureAwait(false);
    }

    public async Task BanWorkspaceAgentAsync(string workspacePath, string agentId, string reason, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
        var body = new { reason, global = false };
        await client.PostAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}/ban?workspace={path}", body, ct)
            .ConfigureAwait(false);
    }

    public async Task UnbanWorkspaceAgentAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
        await client.PostAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}/unban?workspace={path}&global=false", ct: ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteWorkspaceAgentAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
        await client.DeleteAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}?workspace={path}", ct)
            .ConfigureAwait(false);
    }

    public async Task<AgentValidationResult> ValidateWorkspaceAgentsAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = GetAgentWorkspaceManagementHttpClient();
        var path = Uri.EscapeDataString(workspacePath);
        var result = await client.GetAsync<JsonElement>($"/mcpserver/agents/validate?workspace={path}", ct).ConfigureAwait(false);
        var valid = result.TryGetProperty("valid", out var v) && v.GetBoolean();
        var error = result.TryGetProperty("error", out var e) ? e.GetString() : null;
        return new AgentValidationResult(valid, error);
    }

    public async Task CreateDefinitionAsync(string agentId, CancellationToken ct = default)
    {
        var client = GetAgentDefinitionsHttpClient();
        var body = new { id = agentId, displayName = agentId };
        await client.PostAsync<JsonElement>("/mcpserver/agents/definitions", body, ct).ConfigureAwait(false);
    }

    private McpHttpClient GetAgentDefinitionsHttpClient()
    {
        if (_context.HasControlConnection)
            return _context.GetRequiredControlHttpClient();

        return _context.GetRequiredActiveWorkspaceHttpClient();
    }

    private McpHttpClient GetAgentWorkspaceManagementHttpClient()
        => GetAgentDefinitionsHttpClient();

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
}

internal sealed record AgentDefinitionSummary(string Id, string DisplayName, bool IsBuiltIn);

internal sealed record WorkspaceAgentSummary(string AgentId, bool Enabled, bool Banned, string AgentIsolation);

internal sealed record AgentValidationResult(bool Valid, string? Error);

internal sealed class AgentDetailState
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

internal sealed class AgentDefinitionDetailState
{
    public string AgentId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool IsBuiltIn { get; init; }
    public string DefaultLaunchCommand { get; init; } = "";
    public IReadOnlyList<string>? DefaultModels { get; init; }
    public string DefaultBranchStrategy { get; init; } = "";
    public string DefaultSeedPrompt { get; init; } = "";
    public string DefaultInstructionFile { get; init; } = "";
}

internal sealed record AgentDetailSaveRequest(
    string AgentId,
    bool Enabled,
    string AgentIsolation,
    string? LaunchCommandOverride,
    string[]? ModelsOverride,
    string? BranchStrategyOverride,
    string? SeedPromptOverride,
    string MarkerAdditions,
    string[]? InstructionFilesOverride);

internal sealed record AgentDefinitionSaveRequest(
    string AgentId,
    string DisplayName,
    string DefaultLaunchCommand,
    string DefaultInstructionFile,
    IReadOnlyList<string> DefaultModels,
    string DefaultBranchStrategy,
    string DefaultSeedPrompt);
