/// <summary>TR-MCP-NUKE-019 AC-7: the role the build declares for a test project (<see cref="TestProjectRoles"/>).</summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added with the test-project ownership contract for hostile validation finding R14-F4 (operator 2026-11-06: "Separate projects + AC (Recommended)"); written first in the build's infrastructure tests for mock gate ug-r14f4-s1-mockgate-1, then moved here unchanged for the production type (slice S2).</remarks>
internal enum TestProjectRole
{
    /// <summary>Unit-owned: only the unit test selection may accept the project.</summary>
    Unit,

    /// <summary>Integration-owned: only the integration test selection may accept the project.</summary>
    Integration,

    /// <summary>Infrastructure-owned (the build's own tests): accepted by neither selection.</summary>
    Infrastructure,
}

/// <summary>TR-MCP-NUKE-019 AC-7: the kind of one case in a supplied case inventory.</summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; the third kind keeps the build's infrastructure cases from being relabeled unit cases; moved here from the mock gate unchanged (slice S2).</remarks>
internal enum TestCaseKind
{
    /// <summary>A unit case.</summary>
    Unit,

    /// <summary>An integration case (Category=Integration).</summary>
    Integration,

    /// <summary>An infrastructure case of the build's own tests.</summary>
    Infrastructure,
}

/// <summary>TR-MCP-NUKE-019 AC-7: the two test selections that judge project ownership.</summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; there is no infrastructure selection, because infrastructure-owned projects are accepted by neither; moved here from the mock gate unchanged (slice S2).</remarks>
internal enum TestSelectionKind
{
    /// <summary>The unit test selection: accepts unit-owned projects whose cases are all unit cases.</summary>
    Unit,

    /// <summary>The integration test selection: accepts integration-owned projects whose cases are all integration cases.</summary>
    Integration,
}

/// <summary>TR-MCP-NUKE-019 AC-7: the verdict a selection gives one offered project.</summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; the role is judged before the cases; moved here from the mock gate unchanged (slice S2).</remarks>
internal enum TestProjectVerdict
{
    /// <summary>The project is owned by the selection's role and every case is of the selection's kind.</summary>
    Accepted,

    /// <summary>The project is owned by the other test role (a unit-owned project offered to the integration selection, or the reverse).</summary>
    RejectedOtherRole,

    /// <summary>The project is infrastructure-owned, which neither selection accepts.</summary>
    RejectedInfrastructure,

    /// <summary>The project is owned by the selection's role but holds a case of another kind (a mixed project).</summary>
    RejectedForeignCase,
}

/// <summary>
/// TR-MCP-NUKE-019 AC-8: the trait that marks a quarantined test case, a case whose behavior matches no acceptance
/// criterion in the requirement store, and the filter clause Test and MigrationIntegrationTests use to run none of them.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added after mock gate ug-qr-mockgate-1 (operator 2026-11-07: "Find the AC matching the test behavior.  If none match, quarantine them (keep, but descope them) for later manual investigation."); a quarantined case is kept, never deleted, skipped or weakened, and TestGroups still runs it when its class is named.</remarks>
internal static class TestQuarantine
{
    /// <summary>The trait name.</summary>
    internal const string Trait = "Quarantine";

    /// <summary>The trait value of a case whose behavior matches no acceptance criterion.</summary>
    internal const string NoMatchingCriterion = "NoMatchingCriterion";

    /// <summary>The filter clause that excludes quarantined cases: Quarantine!=NoMatchingCriterion.</summary>
    internal const string Exclusion = Trait + "!=" + NoMatchingCriterion;
}

/// <summary>TR-MCP-NUKE-019 AC-7: one case of a supplied case inventory.</summary>
/// <param name="Name">The case name, unique within its project.</param>
/// <param name="Kind">The case kind.</param>
/// <param name="Quarantined">TR-MCP-NUKE-019 AC-8: whether the case carries Quarantine=NoMatchingCriterion; it keeps its kind.</param>
/// <remarks>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; moved here from the mock gate unchanged (slice S2).</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: adds Quarantined, false by default, so every earlier inventory is unchanged (after mock gate ug-qr-mockgate-1).</para>
/// </remarks>
internal sealed record TestCaseEntry(string Name, TestCaseKind Kind, bool Quarantined = false);

/// <summary>TR-MCP-NUKE-019 AC-7: one test project offered to a selection: its name, declared role and case inventory.</summary>
/// <param name="Name">The project name, unique within one offer.</param>
/// <param name="Role">The role the build declares for the project.</param>
/// <param name="Cases">The project's supplied case inventory.</param>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; moved here from the mock gate unchanged (slice S2).</remarks>
internal sealed record TestProjectInventory(string Name, TestProjectRole Role, IReadOnlyList<TestCaseEntry> Cases);

/// <summary>TR-MCP-NUKE-019 AC-7: the verdict for one offered project.</summary>
/// <param name="Project">The project name.</param>
/// <param name="Verdict">The verdict.</param>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; moved here from the mock gate unchanged (slice S2).</remarks>
internal sealed record TestProjectDecision(string Project, TestProjectVerdict Verdict);

/// <summary>TR-MCP-NUKE-019 AC-7: one case a selection runs, identified by its project and case name.</summary>
/// <param name="Project">The project that holds the case.</param>
/// <param name="Case">The case name.</param>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; moved here from the mock gate unchanged (slice S2).</remarks>
internal sealed record SelectedTestCase(string Project, string Case);

/// <summary>TR-MCP-NUKE-019 AC-7: a selection's outcome: one verdict per offered project, in offer order, and the cases it runs.</summary>
/// <param name="Projects">One verdict per offered project, in offer order.</param>
/// <param name="Cases">The cases of the accepted projects that are not quarantined, each exactly once.</param>
/// <remarks>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; moved here from the mock gate unchanged (slice S2).</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: Cases now holds no quarantined case, and <see cref="Quarantined"/> counts the accepted projects' quarantined cases apart (after mock gate ug-qr-mockgate-1).</para>
/// </remarks>
internal sealed record TestOwnershipOutcome(IReadOnlyList<TestProjectDecision> Projects, IReadOnlyList<SelectedTestCase> Cases)
{
    /// <summary>
    /// TR-MCP-NUKE-019 AC-8: the quarantined cases of the accepted projects, each exactly once, counted apart from
    /// <see cref="TestOwnershipOutcome.Cases"/>; empty when no case is quarantined.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    public IReadOnlyList<SelectedTestCase> Quarantined { get; init; } = [];
}

/// <summary>
/// TR-MCP-NUKE-019 AC-7: decides which offered test projects a selection accepts, from each project's declared role and
/// supplied case inventory, and which cases the selection runs.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added as the contract the BDPv4 mock gate (TestProjectOwnershipMockGateTests) and the binding (TestProjectOwnershipTests) run; moved here from the mock gate unchanged (slice S2); the production type is <see cref="TestProjectOwnership"/>.</remarks>
internal interface ITestProjectOwnership
{
    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: judges every offered project for <paramref name="selection"/>: infrastructure-owned projects
    /// are rejected, projects of the other test role are rejected, projects of the selection's role that hold a case of
    /// another kind are rejected, and every other project is accepted. The outcome names one verdict per offered project
    /// in offer order and selects every case of every accepted project exactly once, and no case of a rejected project.
    /// TR-MCP-NUKE-019 AC-8: a quarantined case keeps its kind for the judgment; an accepted project's quarantined cases
    /// are not selected but counted apart in <see cref="TestOwnershipOutcome.Quarantined"/>, each exactly once, so every
    /// case of an accepted project is selected or counted, never both and never neither.
    /// </summary>
    /// <param name="selection">The unit or integration selection.</param>
    /// <param name="offered">The projects offered to the selection, each with its declared role and case inventory.</param>
    /// <returns>The verdicts, the selected cases and the quarantined cases counted apart.</returns>
    /// <exception cref="ArgumentException">A project is offered twice, or a project lists a case name twice; the message names it.</exception>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; moved here from the mock gate unchanged (slice S2).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: quarantined cases are counted apart and not selected (after mock gate ug-qr-mockgate-1).</para>
    /// </remarks>
    TestOwnershipOutcome Decide(TestSelectionKind selection, IReadOnlyList<TestProjectInventory> offered);
}
