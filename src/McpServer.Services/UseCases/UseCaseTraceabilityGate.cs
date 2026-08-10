using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// FR-MCP-USECASE-010: Traceability gate entry point that reuses
/// <see cref="UseCaseFrCoverageEvaluator"/> / <see cref="UseCaseFrCoverageCore"/> so docs
/// ValidateTraceability and runtime coverage share logic.
/// </summary>
public static class UseCaseTraceabilityGate
{
    /// <summary>
    /// Evaluates Realizes UC↔FR coverage and returns human-readable findings.
    /// Empty list means full Realizes coverage for the workspace filter on <paramref name="db"/>.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ValidateRealizesCoverageAsync(
        McpDbContext db,
        CancellationToken cancellationToken = default)
    {
        var snap = await UseCaseFrCoverageEvaluator.EvaluateAsync(db, cancellationToken).ConfigureAwait(false);
        return UseCaseFrCoverageCore.FormatFindings(snap);
    }

    /// <summary>
    /// Formats findings from an already-computed snapshot (for Nuke ValidateTraceability and tests).
    /// </summary>
    public static IReadOnlyList<string> FormatFindings(UseCaseFrCoverageSnapshot snapshot)
        => UseCaseFrCoverageCore.FormatFindings(snapshot);
}
