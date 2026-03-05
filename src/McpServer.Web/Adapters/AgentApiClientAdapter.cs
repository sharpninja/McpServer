using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Web.Adapters;

internal sealed class AgentApiClientAdapter : IAgentApiClient
{
    private readonly WebMcpContext _context;
    private readonly ILogger<AgentApiClientAdapter> _logger;

    public AgentApiClientAdapter(WebMcpContext context, ILogger<AgentApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<AgentApiClientAdapter>.Instance;
    }

    public async Task<ListAgentDefinitionsResult> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.Agent.ListDefinitionsAsync(cancellationToken).ConfigureAwait(false);
        var items = response.Items
            .Select(item => new AgentDefinitionSummaryItem(item.Id, item.DisplayName, item.IsBuiltIn))
            .ToList();
        return new ListAgentDefinitionsResult(items, response.TotalCount);
    }

    public async Task<AgentDefinitionDetail?> GetDefinitionAsync(string agentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
            var definition = await client.Agent.GetDefinitionAsync(agentType, cancellationToken).ConfigureAwait(false);
            return MapDefinition(definition);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<AgentMutationOutcome> UpsertDefinitionAsync(UpsertAgentDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.UpsertDefinitionAsync(
            new AgentDefinitionRequest
            {
                Id = command.Id,
                DisplayName = command.DisplayName,
                DefaultLaunchCommand = command.DefaultLaunchCommand,
                DefaultInstructionFile = command.DefaultInstructionFile,
                DefaultModels = command.DefaultModels,
                DefaultBranchStrategy = command.DefaultBranchStrategy,
                DefaultSeedPrompt = command.DefaultSeedPrompt
            },
            cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    public async Task<AgentMutationOutcome> DeleteDefinitionAsync(DeleteAgentDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.DeleteDefinitionAsync(command.AgentType, cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    public async Task<AgentSeedOutcome> SeedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.SeedDefaultsAsync(cancellationToken).ConfigureAwait(false);
        return new AgentSeedOutcome(result.Seeded);
    }

    public async Task<ListWorkspaceAgentsResult> ListWorkspaceAgentsAsync(ListWorkspaceAgentsQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.Agent.ListWorkspaceAgentsAsync(query.WorkspacePath, cancellationToken).ConfigureAwait(false);
        var items = response.Items.Select(MapWorkspaceAgentItem).ToList();
        return new ListWorkspaceAgentsResult(items, response.TotalCount);
    }

    public async Task<WorkspaceAgentDetail?> GetWorkspaceAgentAsync(GetWorkspaceAgentQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Agent.GetWorkspaceAgentAsync(query.AgentId, query.WorkspacePath, cancellationToken).ConfigureAwait(false);
            return MapWorkspaceAgentDetail(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<AgentMutationOutcome> UpsertWorkspaceAgentAsync(UpsertWorkspaceAgentCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<AgentMutationOutcome> AssignWorkspaceAgentAsync(AssignWorkspaceAgentCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<AgentMutationOutcome> DeleteWorkspaceAgentAsync(DeleteWorkspaceAgentCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.DeleteWorkspaceAgentAsync(command.AgentId, command.WorkspacePath, cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    public async Task<AgentMutationOutcome> BanAgentAsync(BanAgentCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<AgentMutationOutcome> UnbanAgentAsync(UnbanAgentCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.UnbanAgentAsync(
            command.AgentId,
            command.WorkspacePath,
            command.Global,
            cancellationToken).ConfigureAwait(false);
        return new AgentMutationOutcome(result.Success, result.Error);
    }

    public async Task<AgentMutationOutcome> LogEventAsync(LogAgentEventCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<AgentEventsResult> GetEventsAsync(GetAgentEventsQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.GetEventsAsync(query.AgentId, query.WorkspacePath, query.Limit, cancellationToken).ConfigureAwait(false);
        var items = result.Items
            .Select(item => new AgentEventItem(
                item.Id,
                item.AgentId,
                item.WorkspacePath,
                item.EventType,
                item.UserId,
                item.Details,
                item.Timestamp))
            .ToList();
        return new AgentEventsResult(items, result.TotalCount);
    }

    public async Task<AgentValidateOutcome> ValidateAsync(ValidateAgentQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Agent.ValidateAsync(query.WorkspacePath, cancellationToken).ConfigureAwait(false);
        return new AgentValidateOutcome(result.Valid, result.Error, result.Path);
    }

    private static AgentDefinitionDetail MapDefinition(AgentDefinition definition)
        => new(
            definition.Id,
            definition.DisplayName,
            definition.DefaultLaunchCommand,
            definition.DefaultInstructionFile,
            definition.DefaultModels?.ToList() ?? [],
            definition.DefaultBranchStrategy,
            definition.DefaultSeedPrompt,
            definition.IsBuiltIn);

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
            item.ModelsOverride ?? [],
            item.BranchStrategyOverride,
            item.SeedPromptOverride,
            item.MarkerAdditions,
            item.InstructionFilesOverride ?? [],
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
            item.ModelsOverride ?? [],
            item.BranchStrategyOverride,
            item.SeedPromptOverride,
            item.MarkerAdditions,
            item.InstructionFilesOverride ?? [],
            item.AddedAt,
            item.LastLaunchedAt);
}
