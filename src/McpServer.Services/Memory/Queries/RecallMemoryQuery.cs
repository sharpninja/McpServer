using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-011 / FR-MCP-MEMORY-014-14 / TR-MCP-MEMORY-SEARCH-002:
/// CQRS query for <c>memory_recall</c> hybrid BM25+vector fusion, filters, and bounds.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="Query">Recall query text. Null/empty/whitespace must be 400 in S2 Green.</param>
/// <param name="MinScore">Optional minimum score in [0,1].</param>
/// <param name="TopN">Optional result cap.</param>
/// <param name="Tags">Optional tag filter (AND with other filters).</param>
/// <param name="Type">Optional type filter.</param>
/// <param name="Scope">Optional scope filter.</param>
/// <param name="ReadOnlyCaller">True when the caller authenticated with a read-only key.</param>
/// <param name="FusionBm25Weight">Optional BM25 fusion weight override.</param>
/// <param name="FusionVectorWeight">Optional vector fusion weight override.</param>
/// <param name="RerankEnabled">Optional rerank override. Default must remain off.</param>
public sealed record RecallMemoryQuery(
    string WorkspacePath,
    string? Query,
    double? MinScore = null,
    int? TopN = null,
    IReadOnlyList<string>? Tags = null,
    string? Type = null,
    MemoryScope? Scope = null,
    bool ReadOnlyCaller = false,
    double? FusionBm25Weight = null,
    double? FusionVectorWeight = null,
    bool? RerankEnabled = null) : IQuery<MemoryRecallResult>;
