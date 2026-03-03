using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over agent-management endpoints used by UI.Core CQRS handlers.
/// </summary>
public interface IAgentApiClient
{
    /// <summary>Lists global agent definitions.</summary>
    Task<ListAgentDefinitionsResult> ListDefinitionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a specific global agent definition.</summary>
    Task<AgentDefinitionDetail?> GetDefinitionAsync(string agentType, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a global agent definition.</summary>
    Task<AgentMutationOutcome> UpsertDefinitionAsync(UpsertAgentDefinitionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deletes a global agent definition.</summary>
    Task<AgentMutationOutcome> DeleteDefinitionAsync(DeleteAgentDefinitionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Seeds built-in global agent definitions.</summary>
    Task<AgentSeedOutcome> SeedDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists workspace agent configurations.</summary>
    Task<ListWorkspaceAgentsResult> ListWorkspaceAgentsAsync(ListWorkspaceAgentsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gets a workspace agent configuration.</summary>
    Task<WorkspaceAgentDetail?> GetWorkspaceAgentAsync(GetWorkspaceAgentQuery query, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a workspace agent configuration.</summary>
    Task<AgentMutationOutcome> UpsertWorkspaceAgentAsync(UpsertWorkspaceAgentCommand command, CancellationToken cancellationToken = default);

    /// <summary>Assigns (upserts) an agent in a workspace.</summary>
    Task<AgentMutationOutcome> AssignWorkspaceAgentAsync(AssignWorkspaceAgentCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deletes a workspace agent configuration.</summary>
    Task<AgentMutationOutcome> DeleteWorkspaceAgentAsync(DeleteWorkspaceAgentCommand command, CancellationToken cancellationToken = default);

    /// <summary>Bans an agent.</summary>
    Task<AgentMutationOutcome> BanAgentAsync(BanAgentCommand command, CancellationToken cancellationToken = default);

    /// <summary>Unbans an agent.</summary>
    Task<AgentMutationOutcome> UnbanAgentAsync(UnbanAgentCommand command, CancellationToken cancellationToken = default);

    /// <summary>Logs an agent event.</summary>
    Task<AgentMutationOutcome> LogEventAsync(LogAgentEventCommand command, CancellationToken cancellationToken = default);

    /// <summary>Lists agent events.</summary>
    Task<AgentEventsResult> GetEventsAsync(GetAgentEventsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Validates agents.yaml for a workspace.</summary>
    Task<AgentValidateOutcome> ValidateAsync(ValidateAgentQuery query, CancellationToken cancellationToken = default);
}
