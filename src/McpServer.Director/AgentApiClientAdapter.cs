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
    public async Task<AgentMutationOutcome> DeleteDefinitionAsync(
        DeleteAgentDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.DeleteDefinitionAsync(command.AgentType, cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    /// <inheritdoc />
    public async Task<AgentSeedOutcome> SeedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.SeedDefaultsAsync(cancellationToken).ConfigureAwait(false);
        return new AgentSeedOutcome(result.Seeded);
    }

    /// <inheritdoc />
    public async Task<ListWorkspaceAgentsResult> ListWorkspaceAgentsAsync(
        ListWorkspaceAgentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.ListWorkspaceAgentsAsync(query.WorkspacePath, cancellationToken).ConfigureAwait(false);
        return new ListWorkspaceAgentsResult(result.Items.Select(MapWorkspaceAgentItem).ToList(), result.TotalCount);
    }

    /// <inheritdoc />
    public async Task<WorkspaceAgentDetail?> GetWorkspaceAgentAsync(
        GetWorkspaceAgentQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.Agent.GetWorkspaceAgentAsync(query.AgentId, query.WorkspacePath, cancellationToken).ConfigureAwait(false);
            return MapWorkspaceAgentDetail(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AgentMutationOutcome> UpsertWorkspaceAgentAsync(
        UpsertWorkspaceAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.UpsertWorkspaceAgentAsync(
            command.AgentId,
            new AgentWorkspaceRequest
            {
                AgentId = command.AgentId,
                Enabled = command.Enabled,
                AgentIsolation = command.AgentIsolation,
                LaunchCommandOverride = command.LaunchCommandOverride,
                ModelsOverride = command.ModelsOverride,
                BranchStrategyOverride = command.BranchStrategyOverride,
                SeedPromptOverride = command.SeedPromptOverride,
                MarkerAdditions = command.MarkerAdditions ?? string.Empty,
                InstructionFilesOverride = command.InstructionFilesOverride
            },
            command.WorkspacePath,
            cancellationToken).ConfigureAwait(false);

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

    /// <inheritdoc />
    public async Task<AgentMutationOutcome> DeleteWorkspaceAgentAsync(
        DeleteWorkspaceAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.DeleteWorkspaceAgentAsync(command.AgentId, command.WorkspacePath, cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    /// <inheritdoc />
    public async Task<AgentMutationOutcome> BanAgentAsync(
        BanAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.BanAgentAsync(
            command.AgentId,
            new AgentBanRequest
            {
                Reason = command.Reason,
                BannedUntilPr = command.BannedUntilPr,
                Global = command.Global
            },
            command.WorkspacePath,
            cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    /// <inheritdoc />
    public async Task<AgentMutationOutcome> UnbanAgentAsync(
        UnbanAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.UnbanAgentAsync(
            command.AgentId,
            command.WorkspacePath,
            command.Global,
            cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    /// <inheritdoc />
    public async Task<AgentMutationOutcome> LogEventAsync(
        LogAgentEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.LogEventAsync(
            command.AgentId,
            new AgentEventRequest
            {
                AgentId = command.AgentId,
                EventType = command.EventType,
                Details = command.Details
            },
            command.WorkspacePath,
            cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    /// <inheritdoc />
    public async Task<AgentEventsResult> GetEventsAsync(
        GetAgentEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.GetEventsAsync(query.AgentId, query.WorkspacePath, query.Limit, cancellationToken).ConfigureAwait(false);
        var items = result.Items
            .Select(e => new AgentEventItem(e.Id, e.AgentId, e.WorkspacePath, e.EventType, e.UserId, e.Details, e.Timestamp))
            .ToList();
        return new AgentEventsResult(items, result.TotalCount);
    }

    /// <inheritdoc />
    public async Task<AgentValidateOutcome> ValidateAsync(
        ValidateAgentQuery query,
        CancellationToken cancellationToken = default)
    {
        var client = await GetAgentManagementClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.ValidateAsync(query.WorkspacePath, cancellationToken).ConfigureAwait(false);
        return new AgentValidateOutcome(result.Valid, result.Error, result.Path);
    }

    private async Task<McpServerClient> GetAgentManagementClientAsync(CancellationToken cancellationToken)
    {
        if (_context.HasControlConnection)
            return await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        return await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
    }

    private static WorkspaceAgentItem MapWorkspaceAgentItem(AgentWorkspaceConfig item)
        => new(
            item.Id,
            item.AgentId,
            item.WorkspacePath,
            item.Enabled,
            item.Banned,
            item.BannedReason,
            item.BannedUntilPr,
            item.AgentIsolation,
            item.LaunchCommandOverride,
            item.ModelsOverride?.ToList() ?? [],
            item.BranchStrategyOverride,
            item.SeedPromptOverride,
            item.MarkerAdditions,
            item.InstructionFilesOverride?.ToList() ?? [],
            item.AddedAt,
            item.LastLaunchedAt);

    private static WorkspaceAgentDetail MapWorkspaceAgentDetail(AgentWorkspaceConfig item)
        => new(
            item.Id,
            item.AgentId,
            item.WorkspacePath,
            item.Enabled,
            item.Banned,
            item.BannedReason,
            item.BannedUntilPr,
            item.AgentIsolation,
            item.LaunchCommandOverride,
            item.ModelsOverride?.ToList() ?? [],
            item.BranchStrategyOverride,
            item.SeedPromptOverride,
            item.MarkerAdditions,
            item.InstructionFilesOverride?.ToList() ?? [],
            item.AddedAt,
            item.LastLaunchedAt);
}
