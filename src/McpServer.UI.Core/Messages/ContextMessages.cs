using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query for <c>POST /mcpserver/context/search</c>.</summary>
public sealed record SearchContextQuery : IQuery<ContextSearchPayload>
{
    /// <summary>Query text (required).</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Optional source type filter.</summary>
    public string? SourceType { get; init; }

    /// <summary>Maximum chunks to return (default 20).</summary>
    public int Limit { get; init; } = 20;
}

/// <summary>Command for <c>POST /mcpserver/context/rebuild-index</c>.</summary>
public sealed record RebuildContextIndexCommand : ICommand<ContextRebuildResult>;

/// <summary>Query for <c>POST /mcpserver/context/pack</c>.</summary>
public sealed record PackContextQuery : IQuery<ContextPackPayload>
{
    /// <summary>Query text (required).</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Optional deterministic query ID.</summary>
    public string? QueryId { get; init; }

    /// <summary>Maximum chunks to include (default 20).</summary>
    public int Limit { get; init; } = 20;
}

/// <summary>Query for <c>/mcpserver/context/sources</c>.</summary>
public sealed record ListContextSourcesQuery : IQuery<ContextSourcesPayload>;

/// <summary>Context search payload.</summary>
public sealed record ContextSearchPayload(
    string? Query,
    IReadOnlyList<ContextChunkView> Chunks,
    IReadOnlyList<string> SourceKeys);

/// <summary>Context pack payload.</summary>
public sealed record ContextPackPayload(
    string QueryId,
    IReadOnlyList<ContextChunkView> Chunks,
    IReadOnlyList<string> SourceKeys);

/// <summary>Context chunk row/detail payload.</summary>
public sealed record ContextChunkView(
    string Id,
    string DocumentId,
    string Content,
    int TokenCount,
    int ChunkIndex,
    double Score);

/// <summary>Indexed source row.</summary>
public sealed record ContextSourceView(string SourceKey, string SourceType, string? IngestedAt);

/// <summary>Context sources result payload.</summary>
public sealed record ContextSourcesPayload(IReadOnlyList<ContextSourceView> Sources);

/// <summary>Context rebuild result payload.</summary>
public sealed record ContextRebuildResult(string? Status, DateTimeOffset RecordedAt);
