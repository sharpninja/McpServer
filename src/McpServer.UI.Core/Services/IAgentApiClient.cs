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

    /// <summary>Assigns (upserts) an agent in a workspace.</summary>
    Task<AgentMutationOutcome> AssignWorkspaceAgentAsync(AssignWorkspaceAgentCommand command, CancellationToken cancellationToken = default);
}
