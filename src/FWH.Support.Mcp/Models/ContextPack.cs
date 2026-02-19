namespace FWH.Support.Mcp.Models;

/// <summary>
/// FR-SUPPORT-010: Deterministic collection of ranked context chunks.
/// </summary>
public sealed record ContextPack
{
    /// <summary>FR-SUPPORT-010: Query identifier for reproducibility.</summary>
    public required string QueryId { get; init; }

    /// <summary>FR-SUPPORT-010: Ordered list of context chunks.</summary>
    public required IReadOnlyList<ContextChunk> Chunks { get; init; }

    /// <summary>FR-SUPPORT-010: Source keys represented in the pack.</summary>
    public required IReadOnlyList<string> SourceKeys { get; init; }
}
