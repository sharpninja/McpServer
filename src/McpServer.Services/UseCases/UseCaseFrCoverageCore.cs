namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// FR-MCP-USECASE-006 / FR-MCP-USECASE-010: Pure Realizes coverage algorithm (no DB).
/// Shared by runtime evaluator, traceability gate, and Nuke ValidateTraceability.
/// </summary>
public static class UseCaseFrCoverageCore
{
    /// <summary>Default link type of interest.</summary>
    public const string Realizes = "Realizes";

    /// <summary>
    /// Computes Realizes coverage gaps from in-memory collections.
    /// </summary>
    /// <param name="useCases">Active use cases (id + title).</param>
    /// <param name="frIds">Active functional requirement ids (kind fr).</param>
    /// <param name="realizesLinks">Active Realizes links (useCaseId + frId).</param>
    public static UseCaseFrCoverageSnapshot Compute(
        IReadOnlyList<(long UseCaseId, string Title)> useCases,
        IReadOnlyList<string> frIds,
        IReadOnlyList<(long UseCaseId, string FrId)> realizesLinks)
    {
        ArgumentNullException.ThrowIfNull(useCases);
        ArgumentNullException.ThrowIfNull(frIds);
        ArgumentNullException.ThrowIfNull(realizesLinks);

        var linkedUseCaseIds = realizesLinks.Select(l => l.UseCaseId).ToHashSet();
        var linkedFrIds = realizesLinks.Select(l => l.FrId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var useCasesWithout = useCases
            .Where(u => !linkedUseCaseIds.Contains(u.UseCaseId))
            .Select(u => new UseCaseFrCoverageItem(u.UseCaseId, u.Title))
            .ToArray();

        var frsWithout = frIds
            .Where(id => !linkedFrIds.Contains(id))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new UseCaseFrCoverageSnapshot(
            TotalUseCases: useCases.Count,
            TotalFunctionalRequirements: frIds.Count,
            LinkedUseCases: linkedUseCaseIds.Count,
            LinkedFunctionalRequirements: linkedFrIds.Count,
            UseCasesWithoutRealizesLink: useCasesWithout,
            FunctionalRequirementsWithoutRealizesUseCase: frsWithout);
    }

    /// <summary>
    /// Formats human-readable Realizes coverage findings (empty when fully covered).
    /// Shared wording for gate and Nuke ValidateTraceability.
    /// </summary>
    public static IReadOnlyList<string> FormatFindings(UseCaseFrCoverageSnapshot snap)
    {
        ArgumentNullException.ThrowIfNull(snap);
        var findings = new List<string>();
        foreach (var uc in snap.UseCasesWithoutRealizesLink)
            findings.Add($"UseCase {uc.UseCaseId} '{uc.Title}' has no Realizes FR link.");
        foreach (var frId in snap.FunctionalRequirementsWithoutRealizesUseCase)
            findings.Add($"FR {frId} has no Realizes use case link.");
        return findings;
    }
}
