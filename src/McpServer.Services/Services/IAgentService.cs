using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Service interface for managing agent definitions, workspace configurations, and lifecycle events.
/// Agent definitions are stored in the primary instance SQLite database.
/// </summary>
public interface IAgentService
{
    // --- Agent Definitions (global, primary instance only) ---

    /// <summary>List all agent type definitions.</summary>
    Task<AgentDefinitionListResult> ListDefinitionsAsync(CancellationToken ct = default);

    /// <summary>Get a specific agent type definition by ID.</summary>
    Task<AgentDefinitionDto?> GetDefinitionAsync(string agentType, CancellationToken ct = default);

    /// <summary>Create or update an agent type definition.</summary>
    Task<AgentMutationResult> UpsertDefinitionAsync(AgentDefinitionRequest request, CancellationToken ct = default);

    /// <summary>Delete an agent type definition (built-in definitions cannot be deleted).</summary>
    Task<AgentMutationResult> DeleteDefinitionAsync(string agentType, CancellationToken ct = default);

    /// <summary>Seed the database with built-in agent defaults if they don't already exist.</summary>
    Task<int> SeedBuiltInDefaultsAsync(CancellationToken ct = default);

    // --- Workspace Agent Configurations ---

    /// <summary>List agents configured for a specific workspace.</summary>
    Task<AgentWorkspaceListResult> ListWorkspaceAgentsAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>Get a specific agent's workspace configuration.</summary>
    Task<AgentWorkspaceConfigDto?> GetWorkspaceAgentAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>Add or update an agent in a workspace.</summary>
    Task<AgentMutationResult> UpsertWorkspaceAgentAsync(string workspacePath, AgentWorkspaceRequest request, CancellationToken ct = default);

    /// <summary>Remove an agent from a workspace.</summary>
    Task<AgentMutationResult> DeleteWorkspaceAgentAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>Ban an agent in a specific workspace or globally across all workspaces.</summary>
    Task<AgentMutationResult> BanAgentAsync(string agentId, AgentBanRequest request, string? workspacePath = null, CancellationToken ct = default);

    /// <summary>Unban an agent in a specific workspace or globally.</summary>
    Task<AgentMutationResult> UnbanAgentAsync(string agentId, string? workspacePath = null, CancellationToken ct = default);

    // --- Runtime Process Lifecycle ---

    /// <summary>Launches a configured agent runtime for the specified workspace.</summary>
    Task<AgentProcessInfo> LaunchAgentAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>Stops a running configured agent runtime for the specified workspace.</summary>
    Task<bool> StopAgentAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>Gets runtime process status for a configured agent in the specified workspace.</summary>
    Task<AgentProcessInfo?> GetAgentProcessStatusAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>Lists running agent runtimes, optionally filtered to a single workspace.</summary>
    Task<IReadOnlyList<AgentProcessInfo>> ListRunningAgentsAsync(string? workspacePath = null, CancellationToken ct = default);

    // --- Lifecycle Events ---

    /// <summary>Log an agent lifecycle event.</summary>
    Task<AgentMutationResult> LogEventAsync(string workspacePath, AgentEventRequest request, string? userId = null, CancellationToken ct = default);

    /// <summary>Get event history for an agent in a workspace.</summary>
    Task<AgentEventListResult> GetEventsAsync(string workspacePath, string? agentId = null, int limit = 50, CancellationToken ct = default);
}
