using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IAgentApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class AgentApiClientAdapter : IAgentApiClient
{
    private readonly DirectorMcpContext _context;
    private readonly ILogger<AgentApiClientAdapter> _logger;

    /// <summary>Initializes a new instance of the <see cref="AgentApiClientAdapter"/> class.</summary>
    public AgentApiClientAdapter(
        DirectorMcpContext context,
        ILogger<AgentApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<AgentApiClientAdapter>.Instance;
    }

    /// <inheritdoc />
    public async Task<ListAgentDefinitionsResult> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.Agent.ListDefinitionsAsync(cancellationToken).ConfigureAwait(false);
        var items = response.Items
            .Select(i => new AgentDefinitionSummaryItem(i.Id, i.DisplayName, i.IsBuiltIn))
            .ToList();
        return new ListAgentDefinitionsResult(items, response.TotalCount);
    }

    /// <inheritdoc />
    public async Task<AgentDefinitionDetail?> GetDefinitionAsync(string agentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Agent.GetDefinitionAsync(agentType, cancellationToken).ConfigureAwait(false);
            return new AgentDefinitionDetail(
                item.Id,
                item.DisplayName,
                item.DefaultLaunchCommand,
                item.DefaultInstructionFile,
                item.DefaultModels.ToList(),
                item.DefaultBranchStrategy,
                item.DefaultSeedPrompt,
                item.IsBuiltIn);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AgentMutationOutcome> UpsertDefinitionAsync(
        UpsertAgentDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.UpsertDefinitionAsync(new AgentDefinitionRequest
        {
            Id = command.Id,
            DisplayName = command.DisplayName,
            DefaultLaunchCommand = command.DefaultLaunchCommand,
            DefaultInstructionFile = command.DefaultInstructionFile,
            DefaultModels = command.DefaultModels.ToList(),
            DefaultBranchStrategy = command.DefaultBranchStrategy,
            DefaultSeedPrompt = command.DefaultSeedPrompt
        }, cancellationToken).ConfigureAwait(false);

        return new AgentMutationOutcome(result.Success, result.Error);
    }

    /// <inheritdoc />
    public async Task<AgentMutationOutcome> AssignWorkspaceAgentAsync(
        AssignWorkspaceAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.UpsertWorkspaceAgentAsync(
            command.AgentId,
            new AgentWorkspaceRequest
            {
                AgentId = command.AgentId,
                Enabled = command.Enabled,
                AgentIsolation = command.AgentIsolation
            },
            command.WorkspacePath,
            cancellationToken).ConfigureAwait(false);

        return new AgentMutationOutcome(result.Success, result.Error);
    }

    private async Task<McpServerClient> GetAgentManagementClientAsync(CancellationToken cancellationToken)
    {
        if (_context.HasControlConnection)
            return await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        return await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
    }
}
