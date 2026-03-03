using System.Text.Json;
using McpServer.Support.Mcp.Models;
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
    private readonly ILogger<AgentService> _logger;

    /// <summary>Initializes a new instance of <see cref="AgentService"/>.</summary>
    public AgentService(McpDbContext db, ILogger<AgentService> logger)
    {
        _db = db;
        _logger = logger;
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

        if (existing is not null)
        {
            existing.DisplayName = request.DisplayName;
            existing.DefaultLaunchCommand = request.DefaultLaunchCommand;
            existing.DefaultInstructionFile = request.DefaultInstructionFile;
            existing.DefaultModelsJson = JsonSerializer.Serialize(request.DefaultModels);
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
                DefaultModelsJson = JsonSerializer.Serialize(request.DefaultModels),
                DefaultBranchStrategy = request.DefaultBranchStrategy,
                DefaultSeedPrompt = request.DefaultSeedPrompt,
                IsBuiltIn = false,
                CreatedAt = now,
                ModifiedAt = now
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Upserted agent definition '{AgentId}'", request.Id);
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
        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<int> SeedBuiltInDefaultsAsync(CancellationToken ct = default)
    {
        var defaults = AgentDefaults.GetBuiltInDefaults();
        var seeded = 0;

        foreach (var def in defaults)
        {
            var exists = await _db.AgentDefinitions.IgnoreQueryFilters().AnyAsync(x => x.Id == def.Id, ct).ConfigureAwait(false);
            if (!exists)
            {
                _db.AgentDefinitions.Add(def);
                seeded++;
            }
        }

        if (seeded > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Seeded {Count} built-in agent definitions", seeded);
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

        // Verify agent definition exists
        var defExists = await _db.AgentDefinitions.AnyAsync(x => x.Id == request.AgentId, ct).ConfigureAwait(false);
        if (!defExists)
            return new AgentMutationResult { Success = false, Error = $"Agent definition '{request.AgentId}' not found. Create it first." };

        var existing = await _db.AgentWorkspaces
            .FirstOrDefaultAsync(x => x.WorkspacePath == normalized && x.AgentDefinitionId == request.AgentId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Enabled = request.Enabled;
            existing.AgentIsolation = request.AgentIsolation;
            existing.LaunchCommandOverride = request.LaunchCommandOverride;
            existing.ModelsOverrideJson = request.ModelsOverride is not null ? JsonSerializer.Serialize(request.ModelsOverride) : null;
            existing.BranchStrategyOverride = request.BranchStrategyOverride;
            existing.SeedPromptOverride = request.SeedPromptOverride;
            existing.MarkerAdditions = request.MarkerAdditions;
            existing.InstructionFilesOverrideJson = request.InstructionFilesOverride is not null ? JsonSerializer.Serialize(request.InstructionFilesOverride) : null;
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
                ModelsOverrideJson = request.ModelsOverride is not null ? JsonSerializer.Serialize(request.ModelsOverride) : null,
                BranchStrategyOverride = request.BranchStrategyOverride,
                SeedPromptOverride = request.SeedPromptOverride,
                MarkerAdditions = request.MarkerAdditions,
                InstructionFilesOverrideJson = request.InstructionFilesOverride is not null ? JsonSerializer.Serialize(request.InstructionFilesOverride) : null,
                AddedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Upserted workspace agent '{AgentId}' in '{Workspace}'", request.AgentId, normalized);
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
        return new AgentMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<AgentMutationResult> BanAgentAsync(string agentId, AgentBanRequest request, string? workspacePath = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Global || string.IsNullOrWhiteSpace(workspacePath))
        {
            // Ban globally across all workspaces
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

        return new AgentMutationResult { Success = true };
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

        // Update LastLaunchedAt if this is a launch event
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
        DefaultModels = DeserializeStringList(e.DefaultModelsJson),
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
        ModelsOverride = e.ModelsOverrideJson is not null ? DeserializeStringList(e.ModelsOverrideJson) : null,
        BranchStrategyOverride = e.BranchStrategyOverride,
        SeedPromptOverride = e.SeedPromptOverride,
        MarkerAdditions = e.MarkerAdditions,
        InstructionFilesOverride = e.InstructionFilesOverrideJson is not null ? DeserializeStringList(e.InstructionFilesOverrideJson) : null,
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

    private IReadOnlyList<string> DeserializeStringList(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException ex) { _logger.LogWarning("{ExceptionDetail}", ex.ToString()); return []; }
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
