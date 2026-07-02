using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Service implementation for managing agent definitions, workspace configurations, and lifecycle events.
/// All data is stored in the primary instance SQLite database via EF Core.
/// </summary>
public sealed class AgentService : IAgentService
{
    private readonly McpDbContext _db;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<AgentService> _logger;
    private readonly IAgentProcessManager? _agentProcessManager;
    private readonly AgentIsolationStrategyResolver? _isolationStrategyResolver;
    private readonly AgentBranchStrategyResolver? _branchStrategyResolver;

    /// <summary>Initializes a new instance of <see cref="AgentService"/>.</summary>
    public AgentService(
        McpDbContext db,
        ILogger<AgentService> logger,
        IChangeEventBus? eventBus = null,
        IAgentProcessManager? agentProcessManager = null,
        AgentIsolationStrategyResolver? isolationStrategyResolver = null,
        AgentBranchStrategyResolver? branchStrategyResolver = null)
    {
        _db = db;
        _eventBus = eventBus;
        _logger = logger;
        _agentProcessManager = agentProcessManager;
        _isolationStrategyResolver = isolationStrategyResolver;
        _branchStrategyResolver = branchStrategyResolver;
    }

    // --- Agent Definitions ---

    /// <inheritdoc />
    public async Task<AgentDefinitionListResult> ListDefinitionsAsync(CancellationToken ct = default)
    {
        var entities = await _db.AgentDefinitions
            .OrderBy(x => x.DisplayName)
            .ToListAsync(ct).ConfigureAwait(false);

        return new AgentDefinitionListResult
        {
            Items = entities.Select(MapDefinition).ToList(),
            TotalCount = entities.Count
        };
    }

    /// <inheritdoc />
    public async Task<AgentDefinitionDto?> GetDefinitionAsync(string agentType, CancellationToken ct = default)
    {
        var entity = await _db.AgentDefinitions.FindAsync([agentType], ct).ConfigureAwait(false);
        return entity is null ? null : MapDefinition(entity);
    }

    /// <inheritdoc />
    public async Task<AgentMutationResult> UpsertDefinitionAsync(AgentDefinitionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _db.AgentDefinitions.FindAsync([request.Id], ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        var created = existing is null;
        if (existing is not null)
        {
            existing.DisplayName = request.DisplayName;
            existing.DefaultLaunchCommand = request.DefaultLaunchCommand;
            existing.DefaultInstructionFile = request.DefaultInstructionFile;
            await _db.Entry(existing).Collection(x => x.Models).LoadAsync(ct).ConfigureAwait(false);
            existing.Models.Clear();
            foreach (var row in ToModelRows(request.DefaultModels))
                existing.Models.Add(row);
            existing.DefaultBranchStrategy = request.DefaultBranchStrategy;
            existing.DefaultSeedPrompt = request.DefaultSeedPrompt;
            existing.ModifiedAt = now;
        }
        else
        {
            _db.AgentDefinitions.Add(new AgentDefinitionEntity
            {
                Id = request.Id,
                DisplayName = request.DisplayName,
                DefaultLaunchCommand = request.DefaultLaunchCommand,
                DefaultInstructionFile = request.DefaultInstructionFile,
                Models = ToModelRows(request.DefaultModels),
                DefaultBranchStrategy = request.DefaultBranchStrategy,
                DefaultSeedPrompt = request.DefaultSeedPrompt,
                IsBuiltIn = false,
                CreatedAt = now,
                ModifiedAt = now
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Upserted agent definition '{AgentId}'", request.Id);
        await PublishChangeSafeAsync(
            created ? ChangeEventActions.Created : ChangeEventActions.Updated,
            request.Id,
            ct).ConfigureAwait(false);
        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<AgentMutationResult> DeleteDefinitionAsync(string agentType, CancellationToken ct = default)
    {
        var entity = await _db.AgentDefinitions.FindAsync([agentType], ct).ConfigureAwait(false);
        if (entity is null)
            return new AgentMutationResult { Success = false, Error = $"Agent definition '{agentType}' not found." };
        if (entity.IsBuiltIn)
            return new AgentMutationResult { Success = false, Error = $"Cannot delete built-in agent definition '{agentType}'." };

        _db.AgentDefinitions.Remove(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Deleted agent definition '{AgentId}'", agentType);
        await PublishChangeSafeAsync(ChangeEventActions.Deleted, agentType, ct).ConfigureAwait(false);
        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<int> SeedBuiltInDefaultsAsync(CancellationToken ct = default)
    {
        var defaults = AgentDefaults.GetBuiltInDefaults();
        var seeded = 0;
        var seededIds = new List<string>();

        foreach (var def in defaults)
        {
            var exists = await _db.AgentDefinitions.IgnoreQueryFilters().AnyAsync(x => x.Id == def.Id, ct).ConfigureAwait(false);
            if (!exists)
            {
                _db.AgentDefinitions.Add(def);
                seeded++;
                seededIds.Add(def.Id);
            }
        }

        if (seeded > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Seeded {Count} built-in agent definitions", seeded);
            foreach (var seededId in seededIds)
            {
                await PublishChangeSafeAsync(ChangeEventActions.Created, seededId, ct).ConfigureAwait(false);
            }
        }

        return seeded;
    }

    // --- Workspace Agent Configurations ---

    /// <inheritdoc />
    public async Task<AgentWorkspaceListResult> ListWorkspaceAgentsAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entities = await _db.AgentWorkspaces
            .Include(x => x.AgentDefinition)
            .Where(x => x.WorkspacePath == normalized)
            .OrderBy(x => x.AgentDefinitionId)
            .ToListAsync(ct).ConfigureAwait(false);

        return new AgentWorkspaceListResult
        {
            Items = entities.Select(MapWorkspaceConfig).ToList(),
            TotalCount = entities.Count
        };
    }

    /// <inheritdoc />
    public async Task<AgentWorkspaceConfigDto?> GetWorkspaceAgentAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entity = await _db.AgentWorkspaces
            .Include(x => x.AgentDefinition)
            .FirstOrDefaultAsync(x => x.WorkspacePath == normalized && x.AgentDefinitionId == agentId, ct)
            .ConfigureAwait(false);
        return entity is null ? null : MapWorkspaceConfig(entity);
    }

    /// <inheritdoc />
    public async Task<AgentMutationResult> UpsertWorkspaceAgentAsync(string workspacePath, AgentWorkspaceRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = NormalizePath(workspacePath);

        var defExists = await _db.AgentDefinitions.AnyAsync(x => x.Id == request.AgentId, ct).ConfigureAwait(false);
        if (!defExists)
            return new AgentMutationResult { Success = false, Error = $"Agent definition '{request.AgentId}' not found. Create it first." };

        var existing = await _db.AgentWorkspaces
            .FirstOrDefaultAsync(x => x.WorkspacePath == normalized && x.AgentDefinitionId == request.AgentId, ct)
            .ConfigureAwait(false);

        var created = existing is null;
        if (existing is not null)
        {
            existing.Enabled = request.Enabled;
            existing.AgentIsolation = request.AgentIsolation;
            existing.LaunchCommandOverride = request.LaunchCommandOverride;
            existing.ListItems.Clear();
            foreach (var row in ToOverrideRows(request.ModelsOverride, request.InstructionFilesOverride))
                existing.ListItems.Add(row);
            existing.BranchStrategyOverride = request.BranchStrategyOverride;
            existing.SeedPromptOverride = request.SeedPromptOverride;
            existing.MarkerAdditions = request.MarkerAdditions;
        }
        else
        {
            _db.AgentWorkspaces.Add(new AgentWorkspaceEntity
            {
                AgentDefinitionId = request.AgentId,
                WorkspacePath = normalized,
                Enabled = request.Enabled,
                AgentIsolation = request.AgentIsolation,
                LaunchCommandOverride = request.LaunchCommandOverride,
                ListItems = ToOverrideRows(request.ModelsOverride, request.InstructionFilesOverride),
                BranchStrategyOverride = request.BranchStrategyOverride,
                SeedPromptOverride = request.SeedPromptOverride,
                MarkerAdditions = request.MarkerAdditions,
                AddedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Upserted workspace agent '{AgentId}' in '{Workspace}'", request.AgentId, normalized);
        await PublishChangeSafeAsync(
            created ? ChangeEventActions.Created : ChangeEventActions.Updated,
            request.AgentId,
            ct).ConfigureAwait(false);
        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<AgentMutationResult> DeleteWorkspaceAgentAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entity = await _db.AgentWorkspaces
            .FirstOrDefaultAsync(x => x.WorkspacePath == normalized && x.AgentDefinitionId == agentId, ct)
            .ConfigureAwait(false);

        if (entity is null)
            return new AgentMutationResult { Success = false, Error = $"Agent '{agentId}' not found in workspace." };

        _db.AgentWorkspaces.Remove(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Deleted workspace agent '{AgentId}' from '{Workspace}'", agentId, normalized);
        await PublishChangeSafeAsync(ChangeEventActions.Deleted, agentId, ct).ConfigureAwait(false);
        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<AgentMutationResult> BanAgentAsync(string agentId, AgentBanRequest request, string? workspacePath = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Global || string.IsNullOrWhiteSpace(workspacePath))
        {
            var configs = await _db.AgentWorkspaces
                .Where(x => x.AgentDefinitionId == agentId)
                .ToListAsync(ct).ConfigureAwait(false);

            foreach (var config in configs)
            {
                config.Banned = true;
                config.Enabled = false;
                config.BannedReason = request.Reason;
                config.BannedUntilPr = request.BannedUntilPr;
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Globally banned agent '{AgentId}' across {Count} workspaces", agentId, configs.Count);
        }
        else
        {
            var normalized = NormalizePath(workspacePath);
            var config = await _db.AgentWorkspaces
                .FirstOrDefaultAsync(x => x.WorkspacePath == normalized && x.AgentDefinitionId == agentId, ct)
                .ConfigureAwait(false);

            if (config is null)
                return new AgentMutationResult { Success = false, Error = $"Agent '{agentId}' not found in workspace." };

            config.Banned = true;
            config.Enabled = false;
            config.BannedReason = request.Reason;
            config.BannedUntilPr = request.BannedUntilPr;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Banned agent '{AgentId}' in '{Workspace}'", agentId, normalized);
        }

        await PublishChangeSafeAsync(ChangeEventActions.Updated, agentId, ct).ConfigureAwait(false);

        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<AgentMutationResult> UnbanAgentAsync(string agentId, string? workspacePath = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            var configs = await _db.AgentWorkspaces
                .Where(x => x.AgentDefinitionId == agentId && x.Banned)
                .ToListAsync(ct).ConfigureAwait(false);

            foreach (var config in configs)
            {
                config.Banned = false;
                config.Enabled = true;
                config.BannedReason = null;
                config.BannedUntilPr = null;
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Globally unbanned agent '{AgentId}' across {Count} workspaces", agentId, configs.Count);
        }
        else
        {
            var normalized = NormalizePath(workspacePath);
            var config = await _db.AgentWorkspaces
                .FirstOrDefaultAsync(x => x.WorkspacePath == normalized && x.AgentDefinitionId == agentId, ct)
                .ConfigureAwait(false);

            if (config is null)
                return new AgentMutationResult { Success = false, Error = $"Agent '{agentId}' not found in workspace." };

            config.Banned = false;
            config.Enabled = true;
            config.BannedReason = null;
            config.BannedUntilPr = null;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Unbanned agent '{AgentId}' in '{Workspace}'", agentId, normalized);
        }

        await PublishChangeSafeAsync(ChangeEventActions.Updated, agentId, ct).ConfigureAwait(false);

        return new AgentMutationResult { Success = true };
    }

    // --- Runtime Process Lifecycle ---

    /// <inheritdoc />
    public async Task<AgentProcessInfo> LaunchAgentAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var normalizedWorkspace = NormalizePath(workspacePath);
        var runtimeDependencies = EnsureRuntimeDependencies();
        var workspaceConfig = await GetWorkspaceAgentEntityAsync(normalizedWorkspace, agentId, ct).ConfigureAwait(false);
        var definition = workspaceConfig.AgentDefinition
            ?? throw new InvalidOperationException($"Agent definition '{agentId}' was not loaded for workspace runtime launch.");

        if (!workspaceConfig.Enabled)
            throw new InvalidOperationException($"Agent '{agentId}' is disabled in workspace '{normalizedWorkspace}'.");
        if (workspaceConfig.Banned)
            throw new InvalidOperationException($"Agent '{agentId}' is banned in workspace '{normalizedWorkspace}'.");

        var isolationStrategy = runtimeDependencies.Isolation.Resolve(workspaceConfig.AgentIsolation);
        var workDirectory = await isolationStrategy.PrepareWorkDirectoryAsync(normalizedWorkspace, agentId, ct).ConfigureAwait(false);

        var branchMode = string.IsNullOrWhiteSpace(workspaceConfig.BranchStrategyOverride)
            ? definition.DefaultBranchStrategy
            : workspaceConfig.BranchStrategyOverride;
        var branchStrategy = runtimeDependencies.Branch.Resolve(branchMode);
        var branchName = await branchStrategy.PrepareBranchAsync(workDirectory, agentId, ct).ConfigureAwait(false);

        try
        {
            var resolvedCommand = AgentProcessCommandResolver.ResolveEffectiveCommand(
                workspaceConfig,
                definition,
                workDirectory,
                branchName);

            var info = await runtimeDependencies.ProcessManager
                .LaunchAsync(normalizedWorkspace, agentId, resolvedCommand, workDirectory, ct)
                .ConfigureAwait(false);

            await LogEventAsync(
                normalizedWorkspace,
                new AgentEventRequest
                {
                    AgentId = agentId,
                    EventType = AgentEventType.Launch,
                    Details = JsonSerializer.Serialize(new
                    {
                        command = resolvedCommand,
                        branchName,
                        workDirectory,
                        isolation = isolationStrategy.StrategyName,
                        branchStrategy = branchStrategy.StrategyName,
                    })
                },
                userId: null,
                ct).ConfigureAwait(false);

            workspaceConfig.LastLaunchedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return info;
        }
        catch
        {
            await branchStrategy.FinalizeBranchAsync(workDirectory, agentId, ct).ConfigureAwait(false);
            await isolationStrategy.CleanupAsync(normalizedWorkspace, agentId, ct).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> StopAgentAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var normalizedWorkspace = NormalizePath(workspacePath);
        var runtimeDependencies = EnsureRuntimeDependencies();
        var workspaceConfig = await GetWorkspaceAgentEntityAsync(normalizedWorkspace, agentId, ct).ConfigureAwait(false);
        var stopped = await runtimeDependencies.ProcessManager.StopAsync(normalizedWorkspace, agentId, ct).ConfigureAwait(false);
        if (!stopped)
            return false;

        var branchMode = string.IsNullOrWhiteSpace(workspaceConfig.BranchStrategyOverride)
            ? workspaceConfig.AgentDefinition?.DefaultBranchStrategy
            : workspaceConfig.BranchStrategyOverride;
        var branchStrategy = runtimeDependencies.Branch.Resolve(branchMode);

        var isolationStrategy = runtimeDependencies.Isolation.Resolve(workspaceConfig.AgentIsolation);
        var status = await runtimeDependencies.ProcessManager.GetStatusAsync(normalizedWorkspace, agentId, ct).ConfigureAwait(false);
        var workDirectory = status?.WorkDirectory ?? normalizedWorkspace;

        await branchStrategy.FinalizeBranchAsync(workDirectory, agentId, ct).ConfigureAwait(false);
        await isolationStrategy.CleanupAsync(normalizedWorkspace, agentId, ct).ConfigureAwait(false);

        await LogEventAsync(
            normalizedWorkspace,
            new AgentEventRequest
            {
                AgentId = agentId,
                EventType = AgentEventType.Exit,
                Details = JsonSerializer.Serialize(new
                {
                    workDirectory,
                    exitCode = status?.ExitCode,
                    status = status?.Status,
                })
            },
            userId: null,
            ct).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public Task<AgentProcessInfo?> GetAgentProcessStatusAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        var normalizedWorkspace = NormalizePath(workspacePath);
        var runtimeDependencies = EnsureRuntimeDependencies();
        return runtimeDependencies.ProcessManager.GetStatusAsync(normalizedWorkspace, agentId, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentProcessInfo>> ListRunningAgentsAsync(string? workspacePath = null, CancellationToken ct = default)
    {
        var runtimeDependencies = EnsureRuntimeDependencies();
        var normalizedWorkspace = string.IsNullOrWhiteSpace(workspacePath) ? null : NormalizePath(workspacePath);
        return runtimeDependencies.ProcessManager.ListRunningAsync(normalizedWorkspace, ct);
    }

    // --- Lifecycle Events ---

    /// <inheritdoc />
    public async Task<AgentMutationResult> LogEventAsync(string workspacePath, AgentEventRequest request, string? userId = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = NormalizePath(workspacePath);

        _db.AgentEventLogs.Add(new AgentEventLogEntity
        {
            AgentId = request.AgentId,
            WorkspacePath = normalized,
            EventType = request.EventType.ToString(),
            UserId = userId,
            DetailsJson = request.Details,
            Timestamp = DateTime.UtcNow
        });

        if (request.EventType == AgentEventType.Launch)
        {
            var config = await _db.AgentWorkspaces
                .FirstOrDefaultAsync(x => x.WorkspacePath == normalized && x.AgentDefinitionId == request.AgentId, ct)
                .ConfigureAwait(false);
            if (config is not null)
                config.LastLaunchedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Logged {EventType} event for agent '{AgentId}' in '{Workspace}'",
            request.EventType, request.AgentId, normalized);
        await PublishChangeSafeAsync(ChangeEventActions.Created, request.AgentId, ct).ConfigureAwait(false);
        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<AgentEventListResult> GetEventsAsync(string workspacePath, string? agentId = null, int limit = 50, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var query = _db.AgentEventLogs
            .Where(x => x.WorkspacePath == normalized);

        if (!string.IsNullOrWhiteSpace(agentId))
            query = query.Where(x => x.AgentId == agentId);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var entities = await query
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToListAsync(ct).ConfigureAwait(false);

        return new AgentEventListResult
        {
            Items = entities.Select(MapEvent).ToList(),
            TotalCount = total
        };
    }

    // --- Mapping helpers ---

    private AgentDefinitionDto MapDefinition(AgentDefinitionEntity e) => new()
    {
        Id = e.Id,
        DisplayName = e.DisplayName,
        DefaultLaunchCommand = e.DefaultLaunchCommand,
        DefaultInstructionFile = e.DefaultInstructionFile,
        DefaultModels = e.Models.OrderBy(m => m.Ordinal).Select(m => m.Model).ToList(),
        DefaultBranchStrategy = e.DefaultBranchStrategy,
        DefaultSeedPrompt = e.DefaultSeedPrompt,
        IsBuiltIn = e.IsBuiltIn,
        CreatedAt = e.CreatedAt,
        ModifiedAt = e.ModifiedAt
    };

    private AgentWorkspaceConfigDto MapWorkspaceConfig(AgentWorkspaceEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentDefinitionId,
        WorkspacePath = e.WorkspacePath,
        Enabled = e.Enabled,
        Banned = e.Banned,
        BannedReason = e.BannedReason,
        BannedUntilPr = e.BannedUntilPr,
        AgentIsolation = e.AgentIsolation,
        LaunchCommandOverride = e.LaunchCommandOverride,
        ModelsOverride = OverrideValues(e, ModelOverrideListType),
        BranchStrategyOverride = e.BranchStrategyOverride,
        SeedPromptOverride = e.SeedPromptOverride,
        MarkerAdditions = e.MarkerAdditions,
        InstructionFilesOverride = OverrideValues(e, InstructionFileOverrideListType),
        AddedAt = e.AddedAt,
        LastLaunchedAt = e.LastLaunchedAt
    };

    private static AgentEventDto MapEvent(AgentEventLogEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        WorkspacePath = e.WorkspacePath,
        EventType = Enum.TryParse<AgentEventType>(e.EventType, true, out var et) ? et : AgentEventType.Launch,
        UserId = e.UserId,
        Details = e.DetailsJson,
        Timestamp = e.Timestamp
    };

    private const string ModelOverrideListType = "ModelOverride";
    private const string InstructionFileOverrideListType = "InstructionFileOverride";

    /// <summary>Builds ordered 4NF default-model rows; parent key flows from EF graph fixup.</summary>
    private static List<AgentDefinitionModelEntity> ToModelRows(IEnumerable<string>? models)
        => (models ?? []).Select((model, i) => new AgentDefinitionModelEntity { Ordinal = i, Model = model }).ToList();

    /// <summary>Builds the 4NF override-list rows; row presence per list type is the override signal.</summary>
    private static List<AgentWorkspaceListItemEntity> ToOverrideRows(IEnumerable<string>? modelsOverride, IEnumerable<string>? instructionFilesOverride)
    {
        var rows = new List<AgentWorkspaceListItemEntity>();
        var ordinal = 0;
        foreach (var value in modelsOverride ?? [])
            rows.Add(new AgentWorkspaceListItemEntity { ListType = ModelOverrideListType, Ordinal = ordinal++, Value = value });
        ordinal = 0;
        foreach (var value in instructionFilesOverride ?? [])
            rows.Add(new AgentWorkspaceListItemEntity { ListType = InstructionFileOverrideListType, Ordinal = ordinal++, Value = value });
        return rows;
    }

    /// <summary>Reads an override list from the child rows; null when no rows carry the list type.</summary>
    private static IReadOnlyList<string>? OverrideValues(AgentWorkspaceEntity e, string listType)
    {
        var values = e.ListItems
            .Where(r => string.Equals(r.ListType, listType, StringComparison.Ordinal))
            .OrderBy(r => r.Ordinal)
            .Select(r => r.Value)
            .ToList();
        return values.Count > 0 ? values : null;
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private async Task PublishChangeSafeAsync(string action, string entityId, CancellationToken ct)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Agent,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/agent/{entityId}",
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing agent change event for {EntityId}", entityId);
        }
    }

    private (IAgentProcessManager ProcessManager, AgentIsolationStrategyResolver Isolation, AgentBranchStrategyResolver Branch) EnsureRuntimeDependencies()
    {
        if (_agentProcessManager is null || _isolationStrategyResolver is null || _branchStrategyResolver is null)
            throw new InvalidOperationException("Agent runtime dependencies are not fully configured.");

        return (_agentProcessManager, _isolationStrategyResolver, _branchStrategyResolver);
    }

    private async Task<AgentWorkspaceEntity> GetWorkspaceAgentEntityAsync(string workspacePath, string agentId, CancellationToken ct)
    {
        var entity = await _db.AgentWorkspaces
            .Include(x => x.AgentDefinition)
            .FirstOrDefaultAsync(x => x.WorkspacePath == workspacePath && x.AgentDefinitionId == agentId, ct)
            .ConfigureAwait(false);

        return entity ?? throw new InvalidOperationException($"Agent '{agentId}' is not configured for workspace '{workspacePath}'.");
    }
}
