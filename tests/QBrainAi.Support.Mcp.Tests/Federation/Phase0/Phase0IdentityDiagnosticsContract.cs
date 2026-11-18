namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Identity shown for one side of the duplicate candidate; not a production wire DTO.</summary>
/// <param name="Origin">Original source node.</param><param name="OriginalId">Unchanged legacy identity.</param>
/// <param name="CanonicalId">Resolved canonical identity when available.</param>
/// <param name="ProvisionalId">Provisional identity when reservation/resolution is pending.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-PluginIdentityDiagnostics; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal sealed record Phase0IdentityDiagnosticSide(string Origin, string OriginalId, string? CanonicalId, string? ProvisionalId);

/// <summary>Read-only review observation; fields express assertions, not adoption of a public DTO.</summary>
/// <param name="ReviewId">Stable candidate review identity.</param><param name="Project">Project scope.</param>
/// <param name="Sides">Both original and canonical/provisional identities.</param>
/// <param name="Similarity">Finite similarity evidence without adopting a score scale or threshold.</param>
/// <param name="Evidence">Normalized title, description and dependency evidence.</param>
/// <param name="ReservationStatus">Observed reservation HTTP-style outcome.</param>
/// <param name="ReservationCode">Observed normalized outcome.</param>
/// <param name="RequestedHumanId">Human identity for which reservation was refused.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-004; B4; FR-MCP-182 ac-PluginIdentityDiagnostics.</remarks>
internal sealed record Phase0IdentityReviewDiagnostic(
    string ReviewId, string Project, IReadOnlyList<Phase0IdentityDiagnosticSide>? Sides,
    double Similarity, IReadOnlyDictionary<string, string>? Evidence,
    int ReservationStatus, string? ReservationCode, string RequestedHumanId);

/// <summary>Independent durable TODO row used to detect unauthorized changes during review.</summary>
/// <param name="Origin">Source node.</param><param name="OriginalId">Original identity.</param>
/// <param name="Id">Current canonical/provisional key.</param><param name="Title">Authored title.</param>
/// <param name="Description">Authored description.</param><param name="Dependencies">Exact dependency representation.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-PluginIdentityDiagnostics; TEST-MCP-230.</remarks>
internal sealed record Phase0IdentityDiagnosticRow(string Origin, string OriginalId, string Id, string Title, string Description, string Dependencies);

/// <summary>Selected diagnostic surface with independent database, alias and mutation observations.</summary>
/// <remarks>2026-11-17: FR-MCP-182 ac-PluginIdentityDiagnostics. Real installed-plugin bindings belong to Ph7.</remarks>
internal interface IPhase0IdentityDiagnosticsProbe
{
    /// <summary>Establishes the duplicate candidate and unreserved human identity, without deciding it.</summary>
    void Prepare();
    /// <summary>Reads or reopens the candidate through the selected surface.</summary>
    /// <param name="reopen">Whether to restart/reopen the review.</param><returns>Visible diagnostic fields.</returns>
    Phase0IdentityReviewDiagnostic? OpenReview(bool reopen);
    /// <summary>Reads all protected fixture rows independently of the displayed response.</summary>
    /// <returns>Complete fixture rows.</returns>
    IReadOnlyList<Phase0IdentityDiagnosticRow>? ReadRows();
    /// <summary>Reads all durable aliases independently.</summary><returns>Complete alias observations.</returns>
    IReadOnlyList<string>? ReadAliases();
    /// <summary>Reads actual mutation observations, including transient changes later undone.</summary>
    /// <returns>Number of row or alias mutations after fixture setup.</returns>
    int ReadMutationCount();
}

/// <summary>Reusable assertions for identity review visibility and the no-decision mutation boundary.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; B4; FR-MCP-182 ac-PluginIdentityDiagnostics;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. Scripted mock success proves assertion discrimination only.
/// It does not establish installed-plugin execution, a stable wire schema or complete B4 coverage.
/// </remarks>
internal static class Phase0IdentityDiagnosticsContract
{
    /// <summary>Checks both diagnostic reads, exact protected rows/aliases and zero unauthorized mutation.</summary>
    /// <param name="probe">Selected surface and independent durable readbacks.</param>
    internal static void AssertReview(IPhase0IdentityDiagnosticsProbe probe)
    {
        probe.Prepare();
        AssertProtected(probe);
        var first = probe.OpenReview(reopen: false);
        AssertDiagnostic(first);
        AssertProtected(probe);
        var reopened = probe.OpenReview(reopen: true);
        AssertDiagnostic(reopened);
        Assert.Equal(first!.ReviewId, reopened!.ReviewId);
        Assert.Equal(first.Similarity, reopened.Similarity);
        AssertProtected(probe);
    }

    private static void AssertDiagnostic(Phase0IdentityReviewDiagnostic? observation)
    {
        Assert.NotNull(observation);
        Assert.Equal("7c6474a7-0234-4c51-a72f-f1d30bd7e691", observation.ReviewId);
        Assert.Equal("Atlas", observation.Project);
        var sides = Assert.IsAssignableFrom<IReadOnlyList<Phase0IdentityDiagnosticSide>>(observation.Sides);
        Assert.Equal(2, sides.Count);
        Assert.Equal(new Phase0IdentityDiagnosticSide("LAB", "TODO-7", "fixture-canonical-lab-7", null),
            Assert.Single(sides, side => side.Origin == "LAB"));
        Assert.Equal(new Phase0IdentityDiagnosticSide("LEGION", "TODO-91", null, "fixture-provisional-legion-91"),
            Assert.Single(sides, side => side.Origin == "LEGION"));
        Assert.True(double.IsFinite(observation.Similarity));
        var evidence = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(observation.Evidence);
        Assert.True(evidence.TryGetValue("normalizedTitle", out var title));
        Assert.Equal("fix login", title);
        Assert.True(evidence.TryGetValue("normalizedDescription", out var description));
        Assert.Equal("repair café login", description);
        Assert.True(evidence.TryGetValue("normalizedDependencies", out var dependencies));
        Assert.Equal("TODO-2|TODO-3", dependencies);
        Assert.Equal(409, observation.ReservationStatus);
        Assert.Equal("human_id_unreserved", observation.ReservationCode);
        Assert.Equal("IMPL-UNITGREEN-001", observation.RequestedHumanId);
    }

    private static void AssertProtected(IPhase0IdentityDiagnosticsProbe probe)
    {
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Phase0IdentityDiagnosticRow>>(probe.ReadRows());
        Assert.Equal(2, rows.Count);
        Assert.Equal(new Phase0IdentityDiagnosticRow("LAB", "TODO-7", "fixture-canonical-lab-7", "Fix login", "repair café login", "TODO-2|TODO-3"),
            Assert.Single(rows, row => row.Origin == "LAB"));
        Assert.Equal(new Phase0IdentityDiagnosticRow("LEGION", "TODO-91", "fixture-provisional-legion-91", "Fix login", "repair café login", "TODO-2|TODO-3"),
            Assert.Single(rows, row => row.Origin == "LEGION"));
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<string>>(probe.ReadAliases()));
        Assert.Equal(0, probe.ReadMutationCount());
    }
}
