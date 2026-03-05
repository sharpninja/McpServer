using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>FR-MCP-026: Repository for reading and writing requirement entries backed by the four canonical Markdown files.</summary>
public interface IRequirementsRepository
{
    // -- FR --

    /// <summary>Get all Functional Requirement entries.</summary>
    Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default);

    /// <summary>Get a single FR entry by id.</summary>
    Task<FrEntry?> GetFrAsync(string id, CancellationToken ct = default);

    /// <summary>Add a new FR entry.</summary>
    Task AddFrAsync(FrEntry entry, CancellationToken ct = default);

    /// <summary>Update an existing FR entry.</summary>
    Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default);

    /// <summary>Delete an FR entry by id.</summary>
    Task DeleteFrAsync(string id, CancellationToken ct = default);

    // -- TR --

    /// <summary>Get all Technical Requirement entries.</summary>
    Task<IReadOnlyList<TrEntry>> GetAllTrAsync(CancellationToken ct = default);

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

    /// <summary>Get a single TEST entry by id.</summary>
    Task<TestEntry?> GetTestAsync(string id, CancellationToken ct = default);

    /// <summary>Add a new TEST entry.</summary>
    Task AddTestAsync(TestEntry entry, CancellationToken ct = default);

    /// <summary>Update an existing TEST entry.</summary>
    Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default);

    /// <summary>Delete a TEST entry by id.</summary>
    Task DeleteTestAsync(string id, CancellationToken ct = default);

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