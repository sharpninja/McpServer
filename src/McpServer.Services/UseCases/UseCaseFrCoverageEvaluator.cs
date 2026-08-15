using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// FR-MCP-USECASE-006 / FR-MCP-USECASE-010: Loads workspace data and runs
/// <see cref="UseCaseFrCoverageCore"/> (shared with ValidateTraceability).
/// </summary>
public static class UseCaseFrCoverageEvaluator
{
    /// <summary>Default link type of interest.</summary>
    public const string Realizes = UseCaseFrCoverageCore.Realizes;

    /// <summary>
    /// Computes Realizes coverage gaps for the current workspace filter on <paramref name="db"/>.
    /// </summary>
    public static async Task<UseCaseFrCoverageSnapshot> EvaluateAsync(McpDbContext db, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        var useCases = await db.UseCases.AsNoTracking()
            .Select(u => new { u.UseCaseId, u.Title })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var frIds = await db.Requirements.AsNoTracking()
            .Where(r => r.Kind == "fr")
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var realizesLinks = await db.UseCaseFrLinks.AsNoTracking()
            .Where(l => l.LinkType == Realizes)
            .Select(l => new { l.UseCaseId, l.FrId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return UseCaseFrCoverageCore.Compute(
            useCases.Select(u => (u.UseCaseId, u.Title)).ToArray(),
            frIds,
            realizesLinks.Select(l => (l.UseCaseId, l.FrId)).ToArray());
    }
}

/// <summary>Coverage item for a use case missing Realizes links.</summary>
public sealed record UseCaseFrCoverageItem(long UseCaseId, string Title);

/// <summary>Snapshot of UC↔FR Realizes coverage for API and traceability.</summary>
public sealed record UseCaseFrCoverageSnapshot(
    int TotalUseCases,
    int TotalFunctionalRequirements,
    int LinkedUseCases,
    int LinkedFunctionalRequirements,
    IReadOnlyList<UseCaseFrCoverageItem> UseCasesWithoutRealizesLink,
    IReadOnlyList<string> FunctionalRequirementsWithoutRealizesUseCase);
