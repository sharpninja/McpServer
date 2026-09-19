using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: CQRS query for <c>memory_explore</c>.
/// S3 Red ships the port only; no production handler is registered so explore tests stay Red.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="SeedId">Optional MEMORY-* seed. Unknown or soft-deleted seeds must return 404.</param>
/// <param name="Query">Optional query seed used when SeedId is omitted.</param>
/// <param name="Depth">Optional hop depth. Non-positive is 400; above max is 400.</param>
/// <param name="MaxNeighbors">Optional neighbor cap.</param>
/// <param name="HebbianEnabled">Optional Hebbian override. Null uses default off.</param>
public sealed record ExploreMemoryQuery(
    string WorkspacePath,
    string? SeedId = null,
    string? Query = null,
    int? Depth = null,
    int? MaxNeighbors = null,
    bool? HebbianEnabled = null) : IQuery<MemoryExploreResult>;
