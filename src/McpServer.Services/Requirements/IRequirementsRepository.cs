using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>FR-MCP-026: Repository for reading and writing requirement entries backed by the four canonical Markdown files.</summary>
public interface IRequirementsRepository
{
    // -- Scope layers --

    /// <summary>Lists the requirement scope layers for the current workspace.</summary>
    Task<IReadOnlyList<RequirementScopeLayerEntry>> GetRequirementLayersAsync(CancellationToken ct = default);

    /// <summary>Creates a requirement scope layer in the current workspace.</summary>
    Task<RequirementScopeLayerEntry> CreateRequirementLayerAsync(RequirementScopeLayerEntry entry, CancellationToken ct = default);

    /// <summary>Updates mutable metadata for a requirement scope layer in the current workspace.</summary>
    Task<RequirementScopeLayerEntry> UpdateRequirementLayerAsync(RequirementScopeLayerUpdateRequest request, CancellationToken ct = default);

    /// <summary>Gets the current requirement scope layer for the current workspace.</summary>
    Task<RequirementScopeLayerEntry> GetWorkspaceCurrentRequirementLayerAsync(CancellationToken ct = default);

    /// <summary>Sets the current requirement scope layer for the current workspace.</summary>
    Task<RequirementScopeLayerEntry> SetWorkspaceCurrentRequirementLayerAsync(string layerKey, CancellationToken ct = default);

    /// <summary>Gets requirements effective at the current workspace layer or an explicit preview layer.</summary>
    Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(string? layerKey = null, CancellationToken ct = default);

    // -- FR --

    /// <summary>Get all Functional Requirement entries.</summary>
    Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default);

    /// <summary>Query Functional Requirement entries with optional area and status filters.</summary>
    Task<IReadOnlyList<FrEntry>> QueryFrAsync(string? area = null, string? status = null, CancellationToken ct = default);

    /// <summary>Get a single FR entry by id.</summary>
    Task<FrEntry?> GetFrAsync(string id, CancellationToken ct = default);

    /// <summary>Add a new FR entry.</summary>
    Task AddFrAsync(FrEntry entry, CancellationToken ct = default);

    /// <summary>Update an existing FR entry.</summary>
    Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default);

    /// <summary>Delete an FR entry by id.</summary>
    Task DeleteFrAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Purges invalid placeholder FR entries (backfilled with non-canonical IDs).
    /// Returns the number of entries removed.
    /// </summary>
    Task<int> PurgeInvalidPlaceholdersAsync(CancellationToken ct = default);

    // -- TR --

    /// <summary>Get all Technical Requirement entries.</summary>
    Task<IReadOnlyList<TrEntry>> GetAllTrAsync(CancellationToken ct = default);

    /// <summary>Query Technical Requirement entries with optional area, subarea, and status filters.</summary>
    Task<IReadOnlyList<TrEntry>> QueryTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken ct = default);

    /// <summary>Get a single TR entry by id.</summary>
    Task<TrEntry?> GetTrAsync(string id, CancellationToken ct = default);

    /// <summary>Add a new TR entry.</summary>
    Task AddTrAsync(TrEntry entry, CancellationToken ct = default);

    /// <summary>Update an existing TR entry.</summary>
    Task UpdateTrAsync(TrEntry entry, CancellationToken ct = default);

    /// <summary>Delete a TR entry by id.</summary>
    Task DeleteTrAsync(string id, CancellationToken ct = default);

    // -- TEST --

    /// <summary>Get all Testing Requirement entries.</summary>
    Task<IReadOnlyList<TestEntry>> GetAllTestAsync(CancellationToken ct = default);

    /// <summary>Query Testing Requirement entries with optional area and status filters.</summary>
    Task<IReadOnlyList<TestEntry>> QueryTestAsync(string? area = null, string? status = null, CancellationToken ct = default);

    /// <summary>Get a single TEST entry by id.</summary>
    Task<TestEntry?> GetTestAsync(string id, CancellationToken ct = default);

    /// <summary>Add a new TEST entry.</summary>
    Task AddTestAsync(TestEntry entry, CancellationToken ct = default);

    /// <summary>Update an existing TEST entry.</summary>
    Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default);

    /// <summary>Delete a TEST entry by id.</summary>
    Task DeleteTestAsync(string id, CancellationToken ct = default);

    // -- Batch --

    /// <summary>Add FR/TR/TEST entries as one all-or-nothing batch.</summary>
    Task<RequirementsBatchEntries> AddBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default);

    /// <summary>Update FR/TR/TEST entries as one all-or-nothing batch.</summary>
    Task<RequirementsBatchEntries> UpdateBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default);

    // -- Mapping --

    /// <summary>Get all FR-to-TR mapping rows.</summary>
    Task<IReadOnlyList<FrTrMapping>> GetAllMappingsAsync(CancellationToken ct = default);

    /// <summary>Get a single mapping row by FR id.</summary>
    Task<FrTrMapping?> GetMappingAsync(string frId, CancellationToken ct = default);

    /// <summary>Add or update a mapping row for the given FR id.</summary>
    Task UpsertMappingAsync(FrTrMapping mapping, CancellationToken ct = default);

    /// <summary>Delete a mapping row by FR id.</summary>
    Task DeleteMappingAsync(string frId, CancellationToken ct = default);
}
