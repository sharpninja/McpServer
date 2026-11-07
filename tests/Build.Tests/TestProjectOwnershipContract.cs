namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-7 (hostile validation round 14, finding R14-F4): contract cases and assertions for
/// <see cref="ITestProjectOwnership"/> over scripted inventories. Every row states the selection, the offered projects
/// with their declared roles and case inventories, the verdict the criterion requires for each project, and the cases
/// the selection must run. The scripted project names are generic and name no real project. The same assertions run
/// against the independent scripted reference and the wrong implementations of the BDPv4 mock gate
/// (TestProjectOwnershipMockGateTests) and against the production type in the binding.
/// </summary>
/// <remarks>
/// 2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added the ownership contract (operator 2026-11-06: "Separate
/// projects + AC (Recommended)"; MEMORY-BESTPRACTICE-001 rules 4.1 and 4.4). Slice S2, same date: the contract
/// vocabulary (the role, case kind, selection and verdict enums, the inventory and outcome records and
/// <see cref="ITestProjectOwnership"/>) moved unchanged from this file to build/TestProjectOwnershipContracts.cs after
/// mock gate ug-r14f4-s1-mockgate-1, so the production type implements the same contract; the rows and assertions are unchanged.
/// </remarks>
internal static class TestProjectOwnershipContract
{
    /// <summary>A unit-owned project whose cases are all unit cases.</summary>
    internal static readonly TestProjectInventory UnitPure = Project("Unit.Pure", TestProjectRole.Unit, UnitCase("u1"), UnitCase("u2"));

    /// <summary>A unit-owned project holding one unit case and one integration case.</summary>
    internal static readonly TestProjectInventory UnitMixed = Project("Unit.Mixed", TestProjectRole.Unit, UnitCase("u3"), IntegrationCase("i1"));

    /// <summary>A unit-owned project holding only integration cases.</summary>
    internal static readonly TestProjectInventory UnitIntegrationOnly = Project("Unit.IntegrationOnly", TestProjectRole.Unit, IntegrationCase("i7"));

    /// <summary>A unit-owned project holding one unit case and one infrastructure case.</summary>
    internal static readonly TestProjectInventory UnitWithInfrastructureCase = Project("Unit.WithInfrastructureCase", TestProjectRole.Unit, UnitCase("u4"), InfrastructureCase("x4"));

    /// <summary>An integration-owned project whose cases are all integration cases.</summary>
    internal static readonly TestProjectInventory IntegrationPure = Project("Integration.Pure", TestProjectRole.Integration, IntegrationCase("i2"), IntegrationCase("i3"));

    /// <summary>An integration-owned project holding one integration case and one unit case.</summary>
    internal static readonly TestProjectInventory IntegrationMixed = Project("Integration.Mixed", TestProjectRole.Integration, IntegrationCase("i4"), UnitCase("u5"));

    /// <summary>An integration-owned project holding only unit cases.</summary>
    internal static readonly TestProjectInventory IntegrationUnitOnly = Project("Integration.UnitOnly", TestProjectRole.Integration, UnitCase("u7"));

    /// <summary>An integration-owned project holding one integration case and one infrastructure case.</summary>
    internal static readonly TestProjectInventory IntegrationWithInfrastructureCase = Project("Integration.WithInfrastructureCase", TestProjectRole.Integration, IntegrationCase("i8"), InfrastructureCase("x5"));

    /// <summary>An infrastructure-owned project holding only infrastructure cases.</summary>
    internal static readonly TestProjectInventory InfrastructureOwnCases = Project("Infrastructure.Own", TestProjectRole.Infrastructure, InfrastructureCase("x1"), InfrastructureCase("x2"));

    /// <summary>An infrastructure-owned project holding one infrastructure case and one integration case.</summary>
    internal static readonly TestProjectInventory InfrastructureMixed = Project("Infrastructure.Mixed", TestProjectRole.Infrastructure, InfrastructureCase("x3"), IntegrationCase("i5"));

    /// <summary>An infrastructure-owned project holding only unit cases (a probe that only its role rejects under the unit selection).</summary>
    internal static readonly TestProjectInventory InfrastructureUnitKind = Project("Infrastructure.UnitKind", TestProjectRole.Infrastructure, UnitCase("u6"));

    /// <summary>An infrastructure-owned project holding only integration cases (a probe that only its role rejects under the integration selection).</summary>
    internal static readonly TestProjectInventory InfrastructureIntegrationKind = Project("Infrastructure.IntegrationKind", TestProjectRole.Infrastructure, IntegrationCase("i6"));

    /// <summary>A separated unit-owned project; its case SameName also appears in other separated projects.</summary>
    internal static readonly TestProjectInventory SeparatedUnitA = Project("Separated.Unit.A", TestProjectRole.Unit, UnitCase("SameName"), UnitCase("a2"), UnitCase("a3"));

    /// <summary>A second separated unit-owned project, holding a case named like one of <see cref="SeparatedUnitA"/>.</summary>
    internal static readonly TestProjectInventory SeparatedUnitB = Project("Separated.Unit.B", TestProjectRole.Unit, UnitCase("SameName"));

    /// <summary>A separated integration-owned project.</summary>
    internal static readonly TestProjectInventory SeparatedIntegrationA = Project("Separated.Integration.A", TestProjectRole.Integration, IntegrationCase("SameName"), IntegrationCase("c2"));

    /// <summary>A second separated integration-owned project.</summary>
    internal static readonly TestProjectInventory SeparatedIntegrationB = Project("Separated.Integration.B", TestProjectRole.Integration, IntegrationCase("d1"));

    /// <summary>The separated layout's infrastructure-owned project, whose cases neither selection runs.</summary>
    internal static readonly TestProjectInventory SeparatedInfrastructure = Project("Separated.Infrastructure", TestProjectRole.Infrastructure, InfrastructureCase("SameName"), IntegrationCase("e2"));

    /// <summary>The separated layout, interleaved: every project owns cases of its own kind only, and one project is infrastructure-owned.</summary>
    internal static readonly TestProjectInventory[] SeparatedLayout =
        [SeparatedUnitA, SeparatedIntegrationA, SeparatedInfrastructure, SeparatedUnitB, SeparatedIntegrationB];

    /// <summary>Unit selection rows: a unit-owned project holding an integration case is rejected, beside an accepted pure one.</summary>
    internal static readonly Row[] UnitProjectWithIntegrationCaseRows =
    [
        new("a unit-owned project holding an integration case is rejected, the pure one beside it is accepted",
            TestSelectionKind.Unit, [UnitPure, UnitMixed],
            [Verdict(UnitPure, TestProjectVerdict.Accepted), Verdict(UnitMixed, TestProjectVerdict.RejectedForeignCase)],
            CasesOf(UnitPure)),
        new("a unit-owned project holding only integration cases is rejected",
            TestSelectionKind.Unit, [UnitIntegrationOnly],
            [Verdict(UnitIntegrationOnly, TestProjectVerdict.RejectedForeignCase)],
            []),
    ];

    /// <summary>Integration selection rows: an integration-owned project holding a unit case is rejected, beside an accepted pure one.</summary>
    internal static readonly Row[] IntegrationProjectWithUnitCaseRows =
    [
        new("an integration-owned project holding a unit case is rejected, the pure one beside it is accepted",
            TestSelectionKind.Integration, [IntegrationPure, IntegrationMixed],
            [Verdict(IntegrationPure, TestProjectVerdict.Accepted), Verdict(IntegrationMixed, TestProjectVerdict.RejectedForeignCase)],
            CasesOf(IntegrationPure)),
        new("an integration-owned project holding only unit cases is rejected",
            TestSelectionKind.Integration, [IntegrationUnitOnly],
            [Verdict(IntegrationUnitOnly, TestProjectVerdict.RejectedForeignCase)],
            []),
    ];

    /// <summary>Rows for both selections: infrastructure-owned projects are rejected whatever their cases, and run no case.</summary>
    internal static readonly Row[] InfrastructureProjectRows =
    [
        new("the unit selection rejects every infrastructure-owned project, even one holding only unit cases",
            TestSelectionKind.Unit, [InfrastructureUnitKind, InfrastructureOwnCases, InfrastructureMixed],
            [Verdict(InfrastructureUnitKind, TestProjectVerdict.RejectedInfrastructure), Verdict(InfrastructureOwnCases, TestProjectVerdict.RejectedInfrastructure), Verdict(InfrastructureMixed, TestProjectVerdict.RejectedInfrastructure)],
            []),
        new("the integration selection rejects every infrastructure-owned project, even one holding only integration cases",
            TestSelectionKind.Integration, [InfrastructureIntegrationKind, InfrastructureOwnCases, InfrastructureMixed],
            [Verdict(InfrastructureIntegrationKind, TestProjectVerdict.RejectedInfrastructure), Verdict(InfrastructureOwnCases, TestProjectVerdict.RejectedInfrastructure), Verdict(InfrastructureMixed, TestProjectVerdict.RejectedInfrastructure)],
            []),
        new("an infrastructure-owned project beside an accepted unit project adds no case to the unit selection",
            TestSelectionKind.Unit, [UnitPure, InfrastructureUnitKind],
            [Verdict(UnitPure, TestProjectVerdict.Accepted), Verdict(InfrastructureUnitKind, TestProjectVerdict.RejectedInfrastructure)],
            CasesOf(UnitPure)),
        new("an infrastructure-owned project beside an accepted integration project adds no case to the integration selection",
            TestSelectionKind.Integration, [InfrastructureIntegrationKind, IntegrationPure],
            [Verdict(InfrastructureIntegrationKind, TestProjectVerdict.RejectedInfrastructure), Verdict(IntegrationPure, TestProjectVerdict.Accepted)],
            CasesOf(IntegrationPure)),
    ];

    /// <summary>Rows where a project of the other test role is offered: it is rejected for its role, before its cases are judged.</summary>
    internal static readonly Row[] OtherRoleRows =
    [
        new("the integration selection rejects a unit-owned project even when its cases are all integration cases",
            TestSelectionKind.Integration, [UnitIntegrationOnly, IntegrationPure],
            [Verdict(UnitIntegrationOnly, TestProjectVerdict.RejectedOtherRole), Verdict(IntegrationPure, TestProjectVerdict.Accepted)],
            CasesOf(IntegrationPure)),
        new("the unit selection rejects an integration-owned project even when its cases are all unit cases",
            TestSelectionKind.Unit, [IntegrationUnitOnly, UnitPure],
            [Verdict(IntegrationUnitOnly, TestProjectVerdict.RejectedOtherRole), Verdict(UnitPure, TestProjectVerdict.Accepted)],
            CasesOf(UnitPure)),
        new("a mixed unit-owned project offered to the integration selection is rejected for its role",
            TestSelectionKind.Integration, [UnitMixed],
            [Verdict(UnitMixed, TestProjectVerdict.RejectedOtherRole)],
            []),
        new("a mixed integration-owned project offered to the unit selection is rejected for its role",
            TestSelectionKind.Unit, [IntegrationMixed],
            [Verdict(IntegrationMixed, TestProjectVerdict.RejectedOtherRole)],
            []),
    ];

    /// <summary>Rows where a project of the selection's role holds an infrastructure case: its cases are not all of the selection's kind.</summary>
    internal static readonly Row[] InfrastructureCaseRows =
    [
        new("a unit-owned project holding an infrastructure case is rejected",
            TestSelectionKind.Unit, [UnitWithInfrastructureCase],
            [Verdict(UnitWithInfrastructureCase, TestProjectVerdict.RejectedForeignCase)],
            []),
        new("an integration-owned project holding an infrastructure case is rejected",
            TestSelectionKind.Integration, [IntegrationWithInfrastructureCase],
            [Verdict(IntegrationWithInfrastructureCase, TestProjectVerdict.RejectedForeignCase)],
            []),
    ];

    /// <summary>Rows over the separated layout: each selection accepts its own projects and runs each of their cases exactly once.</summary>
    internal static readonly Row[] SeparatedRows =
    [
        new("the unit selection accepts the separated unit projects and runs every case once, the shared case name in both",
            TestSelectionKind.Unit, [SeparatedUnitA, SeparatedUnitB],
            [Verdict(SeparatedUnitA, TestProjectVerdict.Accepted), Verdict(SeparatedUnitB, TestProjectVerdict.Accepted)],
            CasesOf(SeparatedUnitA, SeparatedUnitB)),
        new("the integration selection accepts the separated integration projects and runs every case once",
            TestSelectionKind.Integration, [SeparatedIntegrationA, SeparatedIntegrationB],
            [Verdict(SeparatedIntegrationA, TestProjectVerdict.Accepted), Verdict(SeparatedIntegrationB, TestProjectVerdict.Accepted)],
            CasesOf(SeparatedIntegrationA, SeparatedIntegrationB)),
    ];

    /// <summary>Malformed offers the decision refuses, each with the name its message must hold.</summary>
    internal static readonly (string Name, TestSelectionKind Selection, TestProjectInventory[] Offered, string Named)[] RefusalRows =
    [
        ("a project offered twice is refused", TestSelectionKind.Unit, [UnitPure, UnitPure], UnitPure.Name),
        ("a project listing a case twice is refused", TestSelectionKind.Unit, [Project("Unit.Repeated", TestProjectRole.Unit, UnitCase("repeated"), UnitCase("repeated"))], "repeated"),
    ];

    /// <summary>TR-MCP-NUKE-019 AC-7: a unit-owned project holding an integration case is rejected by the unit selection.</summary>
    /// <param name="ownership">The decision under test.</param>
    internal static void AssertUnitProjectWithIntegrationCaseIsRejected(ITestProjectOwnership ownership) => AssertRows(ownership, UnitProjectWithIntegrationCaseRows);

    /// <summary>TR-MCP-NUKE-019 AC-7: an integration-owned project holding a unit case is rejected by the integration selection.</summary>
    /// <param name="ownership">The decision under test.</param>
    internal static void AssertIntegrationProjectWithUnitCaseIsRejected(ITestProjectOwnership ownership) => AssertRows(ownership, IntegrationProjectWithUnitCaseRows);

    /// <summary>TR-MCP-NUKE-019 AC-7: infrastructure-owned projects are accepted by neither selection.</summary>
    /// <param name="ownership">The decision under test.</param>
    internal static void AssertInfrastructureProjectIsAcceptedByNeitherSelection(ITestProjectOwnership ownership) => AssertRows(ownership, InfrastructureProjectRows);

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: over the separated layout each selection accepts its own projects, and the unit and
    /// integration selections together run every unit and integration case exactly once (none omitted, none duplicated,
    /// a case name shared by two projects counted for each) and no case of the infrastructure-owned project.
    /// </summary>
    /// <param name="ownership">The decision under test.</param>
    internal static void AssertSeparatedProjectsPreserveSelectedCases(ITestProjectOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        AssertRows(ownership, SeparatedRows);

        var unit = ownership.Decide(TestSelectionKind.Unit, SeparatedLayout.Where(project => project.Role == TestProjectRole.Unit).ToArray());
        var integration = ownership.Decide(TestSelectionKind.Integration, SeparatedLayout.Where(project => project.Role == TestProjectRole.Integration).ToArray());
        var notAccepted = unit.Projects.Concat(integration.Projects).Where(decision => decision.Verdict != TestProjectVerdict.Accepted).ToArray();
        Assert.True(notAccepted.Length == 0, $"Separated layout: projects not accepted: [{Describe(notAccepted)}].");

        var selected = unit.Cases.Concat(integration.Cases).ToArray();
        var intended = CasesOf(SeparatedLayout.Where(project => project.Role != TestProjectRole.Infrastructure).ToArray());
        AssertExactlyOnce("separated layout, both selections", selected, intended);
        var infrastructure = selected.Where(item => item.Project == SeparatedInfrastructure.Name).ToArray();
        Assert.True(infrastructure.Length == 0, $"Separated layout: infrastructure cases selected: [{Describe(infrastructure)}].");
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: asserts every row and refusal of the contract: the four named groups, the other-role rows,
    /// the infrastructure-case rows, the separated layout and the refusals of malformed offers.
    /// </summary>
    /// <param name="ownership">The decision under test.</param>
    internal static void AssertContract(ITestProjectOwnership ownership)
    {
        AssertUnitProjectWithIntegrationCaseIsRejected(ownership);
        AssertIntegrationProjectWithUnitCaseIsRejected(ownership);
        AssertInfrastructureProjectIsAcceptedByNeitherSelection(ownership);
        AssertRows(ownership, OtherRoleRows);
        AssertRows(ownership, InfrastructureCaseRows);
        AssertSeparatedProjectsPreserveSelectedCases(ownership);
        AssertRefusals(ownership);
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: a malformed offer (a project twice, or a case twice in one project) is refused, naming it.</summary>
    /// <param name="ownership">The decision under test.</param>
    internal static void AssertRefusals(ITestProjectOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        foreach (var (_, selection, offered, named) in RefusalRows)
        {
            var refusal = Assert.Throws<ArgumentException>(() => ownership.Decide(selection, offered));
            Assert.Contains(named, refusal.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: asserts each row: one verdict per offered project, in offer order, as the row states; the
    /// selected cases are the row's cases, each exactly once; and when every offered project is accepted (a valid
    /// selection), the selected cases are every offered case exactly once.
    /// </summary>
    /// <param name="ownership">The decision under test.</param>
    /// <param name="rows">The rows to assert.</param>
    internal static void AssertRows(ITestProjectOwnership ownership, IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows)
        {
            var outcome = ownership.Decide(row.Selection, row.Offered);
            Assert.NotNull(outcome);
            var actualVerdicts = outcome.Projects.ToArray();
            Assert.True(row.Verdicts.SequenceEqual(actualVerdicts), $"Row '{row.Name}': expected verdicts [{Describe(row.Verdicts)}] but got [{Describe(actualVerdicts)}].");
            AssertExactlyOnce($"row '{row.Name}'", outcome.Cases, row.Cases);
            if (actualVerdicts.All(decision => decision.Verdict == TestProjectVerdict.Accepted))
                AssertExactlyOnce($"row '{row.Name}', valid selection", outcome.Cases, CasesOf(row.Offered));
        }
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: every case of <paramref name="projects"/>, in project then inventory order.</summary>
    /// <param name="projects">The projects whose cases are listed.</param>
    /// <returns>The cases, identified by project and case name.</returns>
    internal static SelectedTestCase[] CasesOf(params TestProjectInventory[] projects) =>
        projects.SelectMany(project => project.Cases.Select(item => new SelectedTestCase(project.Name, item.Name))).ToArray();

    /// <summary>Asserts that <paramref name="actual"/> holds each case of <paramref name="expected"/> exactly once and nothing else.</summary>
    /// <param name="context">The row or check named in a failure.</param>
    /// <param name="actual">The selected cases.</param>
    /// <param name="expected">The cases that must be selected.</param>
    internal static void AssertExactlyOnce(string context, IReadOnlyList<SelectedTestCase> actual, IReadOnlyList<SelectedTestCase> expected)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(expected);
        var duplicated = actual.GroupBy(item => item).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        Assert.True(duplicated.Length == 0, $"In {context}: cases selected more than once: [{Describe(duplicated)}].");
        var omitted = expected.Except(actual).ToArray();
        Assert.True(omitted.Length == 0, $"In {context}: intended cases omitted: [{Describe(omitted)}].");
        var unexpected = actual.Except(expected).ToArray();
        Assert.True(unexpected.Length == 0, $"In {context}: cases selected that the selection does not intend: [{Describe(unexpected)}].");
    }

    /// <summary>A contract row: its name, the selection, the offered projects, the verdicts in offer order and the cases to run.</summary>
    /// <param name="Name">The row name shown in a failure.</param>
    /// <param name="Selection">The selection under test.</param>
    /// <param name="Offered">The offered projects, in offer order.</param>
    /// <param name="Verdicts">The required verdict of each offered project, in offer order.</param>
    /// <param name="Cases">The cases the selection must run, each exactly once.</param>
    internal sealed record Row(string Name, TestSelectionKind Selection, TestProjectInventory[] Offered, TestProjectDecision[] Verdicts, SelectedTestCase[] Cases);

    /// <summary>A scripted unit case.</summary>
    /// <param name="name">The case name.</param>
    /// <returns>The case.</returns>
    private static TestCaseEntry UnitCase(string name) => new(name, TestCaseKind.Unit);

    /// <summary>A scripted integration case.</summary>
    /// <param name="name">The case name.</param>
    /// <returns>The case.</returns>
    private static TestCaseEntry IntegrationCase(string name) => new(name, TestCaseKind.Integration);

    /// <summary>A scripted infrastructure case.</summary>
    /// <param name="name">The case name.</param>
    /// <returns>The case.</returns>
    private static TestCaseEntry InfrastructureCase(string name) => new(name, TestCaseKind.Infrastructure);

    /// <summary>A scripted project.</summary>
    /// <param name="name">The project name.</param>
    /// <param name="role">The declared role.</param>
    /// <param name="cases">The case inventory.</param>
    /// <returns>The project.</returns>
    private static TestProjectInventory Project(string name, TestProjectRole role, params TestCaseEntry[] cases) => new(name, role, cases);

    /// <summary>The required verdict for one scripted project.</summary>
    /// <param name="project">The project.</param>
    /// <param name="verdict">The verdict.</param>
    /// <returns>The decision.</returns>
    private static TestProjectDecision Verdict(TestProjectInventory project, TestProjectVerdict verdict) => new(project.Name, verdict);

    /// <summary>Formats verdicts for a failure message.</summary>
    /// <param name="decisions">The verdicts.</param>
    /// <returns>The text.</returns>
    private static string Describe(IEnumerable<TestProjectDecision> decisions) => string.Join(", ", decisions.Select(decision => $"{decision.Project}={decision.Verdict}"));

    /// <summary>Formats cases for a failure message.</summary>
    /// <param name="cases">The cases.</param>
    /// <returns>The text.</returns>
    private static string Describe(IEnumerable<SelectedTestCase> cases) => string.Join(", ", cases.Select(item => $"{item.Project}/{item.Case}"));
}
