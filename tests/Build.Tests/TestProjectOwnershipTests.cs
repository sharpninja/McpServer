namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-7 binding: runs the <see cref="TestProjectOwnershipContract"/> cases, unchanged from the BDPv4 mock
/// gate ug-r14f4-s1-mockgate-1, over the production <see cref="TestProjectOwnership"/>; pins the build's role declarations
/// (<see cref="TestProjectRoles"/>) and the checks Test and MigrationIntegrationTests make with them; and proves that the
/// build's real role declarations against explicitly synthetic case inventories: unit-owned projects accept only unit
/// cases, integration-owned projects accept only integration cases, the infrastructure-owned project is offered to neither
/// selection, and each valid selection preserves every scripted case exactly once.
/// </summary>
/// <remarks>
/// <para>
/// A declaration, inventory row or decision that mixes the kinds fails these cases: a unit-owned project with an
/// integration or infrastructure case, an integration-owned project with a unit or infrastructure case, a declared
/// project the selections leave unexplained, an infrastructure-owned project in either selection, or a selection that
/// omits or repeats a case. The method names say what each case proves; the history of the per-slice record they
/// replace is in the dated entries below.
/// </para>
/// <para>
/// These infrastructure tests do not claim the current layout's per-project or total case counts. Runtime target ownership
/// is bound separately by <see cref="TestProjectCaseDiscoveryTests"/> to cases discovered from each selected project. The
/// scripted inventories here isolate the pure ownership decision; the only workspace quantity retained is AC-8's 29
/// quarantined cases, counted apart as 26 and 3. They read no checkout, process environment or test result.
/// </para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: binds the quarantine contract (mock gate ug-qr-mockgate-1) to the production decision through an adapter that marks the quarantined cases of each offered project, and records the quarantined cases per project apart from the unit and integration cases: the 22 test methods tagged Quarantine=NoMatchingCriterion on 2026-11-07, whose behavior matches no acceptance criterion (uncited-matches.txt), are 29 test cases, 3 in QBrainAi.QBAgent.Tests (QBAgentHeartbeatOverlayTests: 3 methods) and 26 in QBrainAi.Support.Mcp.Tests (McpInstanceResolverTests 3 methods and 3 cases, CliBrainSlotStrategyTests 13 methods and 20 cases, GitHubCliServiceTests 2, SessionLogServiceReplaceDeleteTests 1), because four of the methods are theories (2, 3, 3 and 3 rows). The unit selection still accepts every unit-owned project, runs the 4835 other unit cases exactly once and counts the 29 quarantined cases apart; the integration selection counts none.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added the binding of slice S2 for hostile validation finding R14-F4 (operator 2026-11-06: "Separate projects + AC (Recommended)"; MEMORY-BESTPRACTICE-001 rules 4.1, 4.4 and 4.5).</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S3 updates the record: the 1 McpAgent, 2 REPL core and 3 QBAgent integration cases moved to QBrainAi.McpAgent.IntegrationTests, QBrainAi.Repl.IntegrationTests and QBrainAi.QBAgent.IntegrationTests, so the unit selection now accepts QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests, and the integration selection accepts the two new integration-owned projects in the lane; the declarations and the case counts change with them.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S4 updates the record: the 96 integration cases of the infrastructure-owned Build.Tests moved to the integration-owned Build.IntegrationTests, which takes Build.Tests' place in the lane, so the integration selection accepts it and no lane project is rejected as infrastructure; Build.Tests holds only its 467 infrastructure cases and is offered to neither selection.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S5 updates the record: the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests took its first 13 integration cases from QBrainAi.Support.Mcp.Tests (Storage: BugTriage139EighteenthSqliteTests and the two WorkspaceNativePath facade classes) and runs in the lane after it, so the integration selection accepts it; QBrainAi.Support.Mcp.Tests, which still holds 475 integration cases, stays rejected by both selections until slice S8.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S6 updates the record: 208 more integration cases (the whole pure-integration files of nine folders that no unit file pins) moved from QBrainAi.Support.Mcp.Tests to QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the integration selection accepts 221 cases there; QBrainAi.Support.Mcp.Tests, which still holds 267 integration cases, stays rejected by both selections until slice S8.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the project renames that follow slice S6 (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name.") update the record's names only: QBrainAi.Support.Mcp.Environment.IntegrationTests is now QBrainAi.Support.Mcp.EnvironmentIntegrationTests and Build.IntegrationTests is now NukeBuild.IntegrationTests, with the same roles, case counts, run positions and verdicts.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S7 updates the record: the mixed classes of QBrainAi.Support.Mcp.Tests split into their unit halves, which stay, and their integration halves in QBrainAi.Support.Mcp.EnvironmentIntegrationTests, with the pure-integration classes that shared their files, so 169 more integration cases moved there (221 to 390); QBrainAi.Support.Mcp.Tests, which still holds 98 integration cases (the reverse-reference bindings and the classes left for slice S8), stays rejected by both selections until then.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S8 updates the record: the last 98 integration cases of QBrainAi.Support.Mcp.Tests (the classes its collection bindings named, the platform-native cluster and the S7 leftovers) moved to QBrainAi.Support.Mcp.EnvironmentIntegrationTests, and the two collection bindings that name integration classes moved with them as Category=Integration cases (390 to 490 there); QBrainAi.Support.Mcp.Tests holds 3308 unit cases and no integration case, so the unit selection accepts it, and it left the lane, so the integration selection accepts every lane project. Every recorded verdict is now Accepted.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S9 of hostile validation finding R14-F4 turns the recorded current-layout expectation that slices S3 to S8 updated into the plain separated assertion, now that the separation is complete: the summary and the first two remarks paragraphs say what the class proves; CurrentLayoutCaseCounts is now RecordedCaseInventory with the same rows; CurrentLayout_UnitSelection_RecordsTheMixedUnitProjectsAsRejected becomes UnitSelection_AcceptsEveryUnitOwnedProject_AndRunsEveryUnitCaseOnce and CurrentLayout_IntegrationSelection_RecordsTheLaneProjectsItCannotAccept becomes IntegrationSelection_AcceptsEveryLaneProject_AndRunsEveryIntegrationCaseOnce, each asserting that every offered project is accepted and every intended case of the inventory runs exactly once instead of listing recorded verdicts; DeclaredProjects_HoldOnlyCasesOfTheirOwnKind and InfrastructureProject_IsOfferedToNeitherSelection are added. The contract cases, the mock gate and the declarations are unchanged (the role decisions of slice S9 leave every declaration as it was; TestProjectRoles documents why the other test projects stay undeclared).</para>
/// <para>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7, AC-8; R16-F1: the preceding dated count entries are history only. The binding now uses an explicitly synthetic inventory and makes no current-layout case-count claim; runtime-discovered identities bind the targets in <see cref="TestProjectCaseDiscoveryTests"/>.</para>
/// </remarks>
public sealed class TestProjectOwnershipTests
{
    /// <summary>The declared unit projects offered by Test, in target order.</summary>
    private static readonly string[] UnitSelectionProjects =
    [
        "QBrainAi.Support.Mcp.Tests", "QBrainAi.Client.Tests", "QBrainAi.Cqrs.Tests", "QBrainAi.Launcher.Tests",
        "QBrainAi.McpAgent.Tests", "QBrainAi.Repl.Core.Tests", "QBrainAi.QBAgent.Tests",
    ];

    /// <summary>
    /// The two integration-owned projects the lane holds out until PLAN-SESSIONTRIAGEFIX-001 merges (TODO
    /// IMPL-TXNINTEGRATION-001), in declaration order: no selection offers them, so the inventory has no row for them.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: added in slice S9 for the plain separated assertion.</remarks>
    private static readonly string[] HeldOutIntegrationProjects =
    [
        "QBrainAi.TransactionSecurity.IntegrationTests", "QBrainAi.Acid.IntegrationTests",
    ];

    /// <summary>
    /// A synthetic ownership inventory: one ordinary case of each declared project's role, plus AC-8's 29 quarantined
    /// unit cases counted apart in the two projects that own them. It is deliberately not a snapshot of current test counts.
    /// </summary>
    /// <remarks>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7, AC-8; R16-F1: replaces the recorded current-layout counts with an explicitly synthetic decision contract.</remarks>
    private static readonly (string Project, int Unit, int Integration, int Infrastructure, int Quarantined)[] SyntheticCaseInventory =
    [
        ("QBrainAi.Support.Mcp.Tests", 1, 0, 0, 26),
        ("QBrainAi.Support.Mcp.EnvironmentIntegrationTests", 0, 1, 0, 0),
        ("QBrainAi.Client.Tests", 1, 0, 0, 0),
        ("QBrainAi.Cqrs.Tests", 1, 0, 0, 0),
        ("QBrainAi.Launcher.Tests", 1, 0, 0, 0),
        ("QBrainAi.McpAgent.Tests", 1, 0, 0, 0),
        ("QBrainAi.Repl.Core.Tests", 1, 0, 0, 0),
        ("QBrainAi.QBAgent.Tests", 1, 0, 0, 3),
        ("QBrainAi.McpAgent.IntegrationTests", 0, 1, 0, 0),
        ("QBrainAi.QBAgent.IntegrationTests", 0, 1, 0, 0),
        ("NukeBuild.IntegrationTests", 0, 1, 0, 0),
        ("Build.Tests", 0, 0, 1, 0),
        ("QBrainAi.Repl.IntegrationTests", 0, 1, 0, 0),
        ("QBrainAi.Support.Mcp.IntegrationTests", 0, 1, 0, 0),
    ];

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8: the quarantined test cases the unit selection must count apart, per project, in the unit
    /// selection's project order: the 29 cases of the 22 methods tagged Quarantine=NoMatchingCriterion on 2026-11-07.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static readonly (string Project, int Cases)[] QuarantinedPerProject =
    [
        ("QBrainAi.Support.Mcp.Tests", 26),
        ("QBrainAi.QBAgent.Tests", 3),
    ];

    /// <summary>The production decision under test.</summary>
    private readonly TestProjectOwnership ownership = new();

    /// <summary>TR-MCP-NUKE-019 AC-7: the production decision satisfies every row and refusal of the mock-gated contract.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void ProductionOwnership_SatisfiesTheContract()
        => TestProjectOwnershipContract.AssertContract(ownership);

    /// <summary>TR-MCP-NUKE-019 AC-7: the production unit selection rejects a unit-owned project holding an integration case (contract rows).</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void UnitProject_WithIntegrationCase_IsRejected()
        => TestProjectOwnershipContract.AssertUnitProjectWithIntegrationCaseIsRejected(ownership);

    /// <summary>TR-MCP-NUKE-019 AC-7: the production integration selection rejects an integration-owned project holding a unit case (contract rows).</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void IntegrationProject_WithUnitCase_IsRejected()
        => TestProjectOwnershipContract.AssertIntegrationProjectWithUnitCaseIsRejected(ownership);

    /// <summary>TR-MCP-NUKE-019 AC-7: neither production selection accepts an infrastructure-owned project (contract rows).</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void InfrastructureProject_IsAcceptedByNeitherSelection()
        => TestProjectOwnershipContract.AssertInfrastructureProjectIsAcceptedByNeitherSelection(ownership);

    /// <summary>TR-MCP-NUKE-019 AC-7: over the separated layout the production selections run every unit and integration case exactly once (contract rows).</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void SeparatedProjects_PreserveSelectedCases()
        => TestProjectOwnershipContract.AssertSeparatedProjectsPreserveSelectedCases(ownership);

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8: the production decision satisfies every quarantine row and every AC-7 row with nothing
    /// quarantined (contract rows of mock gate ug-qr-mockgate-1): quarantined cases keep their kind for the judgment, are
    /// counted apart per project, are never run and never dropped, and every other case runs exactly once. Data: the
    /// contract's scripted inventories, with each row's quarantined cases marked through <see cref="DecideWithQuarantine"/>.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Fact]
    public void ProductionOwnership_SatisfiesTheQuarantineContract()
        => TestProjectOwnershipContract.AssertQuarantineContract(DecideWithQuarantine);

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: the build's declarations are exactly these roles: the seven unit projects, the eight
    /// integration projects (two of them held out of the lane) and Build.Tests as infrastructure, and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: records QBrainAi.McpAgent.IntegrationTests and QBrainAi.QBAgent.IntegrationTests as integration-owned (slice S3).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: records Build.IntegrationTests as integration-owned (slice S4).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: records QBrainAi.Support.Mcp.Environment.IntegrationTests as integration-owned (slice S5).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: records the renamed NukeBuild.IntegrationTests and QBrainAi.Support.Mcp.EnvironmentIntegrationTests in place of Build.IntegrationTests and QBrainAi.Support.Mcp.Environment.IntegrationTests (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name.").</para>
    /// </remarks>
    [Fact]
    public void Declarations_AreTheRecordedRoles()
    {
        (string Project, TestProjectRole Role)[] recorded =
        [
            ("QBrainAi.Support.Mcp.Tests", TestProjectRole.Unit),
            ("QBrainAi.Client.Tests", TestProjectRole.Unit),
            ("QBrainAi.Cqrs.Tests", TestProjectRole.Unit),
            ("QBrainAi.Launcher.Tests", TestProjectRole.Unit),
            ("QBrainAi.McpAgent.Tests", TestProjectRole.Unit),
            ("QBrainAi.Repl.Core.Tests", TestProjectRole.Unit),
            ("QBrainAi.QBAgent.Tests", TestProjectRole.Unit),
            ("QBrainAi.Support.Mcp.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.Repl.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.TransactionSecurity.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.Acid.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.McpAgent.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.QBAgent.IntegrationTests", TestProjectRole.Integration),
            ("NukeBuild.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.Support.Mcp.EnvironmentIntegrationTests", TestProjectRole.Integration),
            ("Build.Tests", TestProjectRole.Infrastructure),
        ];
        Assert.Equal(recorded, TestProjectRoles.Declared);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: Test's declared project list contains only unit-owned projects, and its role check refuses,
    /// naming each, a project that is undeclared or declared with another role.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void TestSelection_IsDeclaredUnitOwned_AndRefusesAnyOtherProject()
    {
        TestProjectRoles.RequireRole(TestProjectRole.Unit, UnitSelectionProjects, "Test");

        var refusal = Assert.Throws<InvalidOperationException>(() => TestProjectRoles.RequireRole(
            TestProjectRole.Unit,
            ["QBrainAi.Client.Tests", "QBrainAi.Zeta.Tests", "Build.Tests", "QBrainAi.Repl.IntegrationTests"],
            "Test"));
        Assert.Contains("QBrainAi.Zeta.Tests (declared no role)", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("Build.Tests (declared Infrastructure)", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("QBrainAi.Repl.IntegrationTests (declared Integration)", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("QBrainAi.Client.Tests", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: every project of the lane's selection on the contract's solution row has a declared role (so
    /// the lane's role lookup cannot fail at this layout), recorded here in run order, and an undeclared project is
    /// refused by name.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the recorded lane is the six projects of slice S3, the two new integration-owned projects in place of the three unit projects.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the recorded lane is the six projects of slice S4, the integration-owned Build.IntegrationTests in place of the infrastructure-owned Build.Tests.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the recorded lane is the seven projects of slice S5, the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests after QBrainAi.Support.Mcp.Tests.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the recorded lane names the renamed QBrainAi.Support.Mcp.EnvironmentIntegrationTests and NukeBuild.IntegrationTests in the same positions and roles (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name.").</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the recorded lane is the six integration-owned projects of slice S8; the unit-owned QBrainAi.Support.Mcp.Tests left the lane.</para>
    /// </remarks>
    [Fact]
    public void LaneProjects_HaveTheirRecordedRoles_AndAnUndeclaredProjectIsRefused()
    {
        (string Project, TestProjectRole Role)[] recorded =
        [
            ("QBrainAi.Support.Mcp.EnvironmentIntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.McpAgent.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.QBAgent.IntegrationTests", TestProjectRole.Integration),
            ("NukeBuild.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.Repl.IntegrationTests", TestProjectRole.Integration),
            ("QBrainAi.Support.Mcp.IntegrationTests", TestProjectRole.Integration),
        ];
        var lane = new IntegrationLaneProjects().Select(IntegrationLaneProjectsContract.Solution);
        Assert.Equal(recorded, lane.Select(project => (project, TestProjectRoles.RoleOf(project))).ToArray());

        var refusal = Assert.Throws<InvalidOperationException>(() => TestProjectRoles.RoleOf("QBrainAi.Zeta.IntegrationTests"));
        Assert.Contains("QBrainAi.Zeta.IntegrationTests", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: every project in the synthetic inventory is declared and holds only cases of its own role.
    /// The declared projects without a synthetic row are exactly the two integration-owned projects the lane holds out.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added in slice S9 of hostile validation finding R14-F4 as part of the plain separated assertion.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: reads the Quarantined column; a project's quarantined cases are of its own kind, so they count with its own cases here, and a project still needs at least one case that is not quarantined.</para>
    /// <para>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R16-F1: the inventory is now explicitly synthetic and asserts no current project count.</para>
    /// </remarks>
    [Fact]
    public void DeclaredProjects_HoldOnlyCasesOfTheirOwnKind()
    {
        foreach (var (project, unit, integration, infrastructure, quarantined) in SyntheticCaseInventory)
        {
            var role = TestProjectRoles.RoleOf(project);
            var (own, foreign) = role switch
            {
                TestProjectRole.Unit => (unit, integration + infrastructure),
                TestProjectRole.Integration => (integration, unit + infrastructure),
                _ => (infrastructure, unit + integration),
            };
            Assert.True(own > 0 && foreign == 0, $"{project} (declared {role}) holds {unit} unit, {integration} integration, {infrastructure} infrastructure and {quarantined} quarantined cases.");
        }

        var withoutInventory = TestProjectRoles.Declared
            .Select(entry => entry.Project)
            .Except(SyntheticCaseInventory.Select(row => row.Project), StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(HeldOutIntegrationProjects, withoutInventory);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: the unit selection is offered every unit-owned project the build declares, exactly the projects
    /// Test runs (QBrainAi.Support.Mcp.Tests, QBrainAi.Client.Tests, QBrainAi.Cqrs.Tests, QBrainAi.Launcher.Tests,
    /// QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests), accepts every one with its real
    /// declared role and synthetic inventory, runs every scripted unit case exactly once, and counts AC-8's 29 quarantined
    /// cases apart as 26 and 3. Runtime discovery, not this test, establishes the current layout.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S3 moved the integration cases of QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests, so the record accepts them (364 to 1556 accepted cases).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S5 moved 13 of the integration cases of QBrainAi.Support.Mcp.Tests; with 475 still there it stays rejected, so the verdicts and the 1556 accepted cases are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S6 moved 208 more; with 267 still there QBrainAi.Support.Mcp.Tests stays rejected, so the verdicts and the 1556 accepted cases are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S7 moved 169 more; with 98 still there QBrainAi.Support.Mcp.Tests stays rejected, so the verdicts and the 1556 accepted cases are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S8 moved the last 98 and the two collection bindings that name integration classes (now integration cases there), so QBrainAi.Support.Mcp.Tests holds 3308 unit cases and no integration case and is accepted (1556 to 4864 accepted cases); the unit total is 4864, the da11faa2 Test gate's 4866 less those two bindings. The method name is kept for the case's identity; the summary describes the new record.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S9 renames the case from CurrentLayout_UnitSelection_RecordsTheMixedUnitProjectsAsRejected, which no longer described it, and makes it the plain assertion: the offered projects are every declared unit-owned project, every verdict is Accepted, and the selected cases are every unit case of the inventory exactly once, in place of a recorded verdict list.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: the 29 quarantined cases are counted apart, per project (QBrainAi.Support.Mcp.Tests 26, QBrainAi.QBAgent.Tests 3), each exactly once, and the exactly-once count of the cases run drops from 4864 to 4835. The method name is kept for the case's identity.</para>
    /// <para>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7, AC-8; R16-F1: the earlier totals are historical. This test now uses one scripted unit case per selected project plus the preserved 26-and-3 quarantine contract.</para>
    /// </remarks>
    [Fact]
    public void UnitSelection_AcceptsEveryUnitOwnedProject_AndRunsEveryUnitCaseOnce()
    {
        var unitOwned = TestProjectRoles.Declared.Where(entry => entry.Role == TestProjectRole.Unit).Select(entry => entry.Project).ToArray();
        Assert.Equal(unitOwned, UnitSelectionProjects);
        var offered = UnitSelectionProjects.Select(SyntheticInventory).ToArray();

        var outcome = ownership.Decide(TestSelectionKind.Unit, offered);

        Assert.Equal(UnitSelectionProjects, outcome.Projects.Select(decision => decision.Project).ToArray());
        Assert.All(outcome.Projects, decision => Assert.Equal(TestProjectVerdict.Accepted, decision.Verdict));
        TestProjectOwnershipContract.AssertExactlyOnce("unit selection", outcome.Cases, IntendedCases(TestCaseKind.Unit));
        TestProjectOwnershipContract.AssertExactlyOnce("unit selection, quarantined cases counted apart", outcome.Quarantined, IntendedQuarantinedCases());
        Assert.Equal(QuarantinedPerProject, CountByProject(outcome.Quarantined));
        Assert.Equal(29, outcome.Quarantined.Count);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8: the build's test-project ownership counts the quarantined cases per project apart from the
    /// unit and integration cases: the unit selection counts QBrainAi.Support.Mcp.Tests 26 and QBrainAi.QBAgent.Tests 3
    /// (29 cases of the 22 tagged methods) and runs none of them, the integration selection counts none, and together the
    /// two selections run every non-quarantined scripted case exactly once and account for every quarantined case.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</para>
    /// <para>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7, AC-8; R16-F1: replaces current-layout totals with a synthetic decision trace while retaining the 29-case quarantine assertion.</para>
    /// </remarks>
    [Fact]
    public void QuarantinedCases_AreCountedPerProject_ApartFromTheUnitAndIntegrationCases()
    {
        var lane = new IntegrationLaneProjects().Select(IntegrationLaneProjectsContract.Solution);
        var unit = ownership.Decide(TestSelectionKind.Unit, UnitSelectionProjects.Select(SyntheticInventory).ToArray());
        var integration = ownership.Decide(TestSelectionKind.Integration, lane.Select(SyntheticInventory).ToArray());

        Assert.Equal(QuarantinedPerProject, CountByProject(unit.Quarantined));
        Assert.Empty(integration.Quarantined);
        SelectedTestCase[] run = [.. unit.Cases, .. integration.Cases];
        TestProjectOwnershipContract.AssertExactlyOnce("both selections, cases run", run, [.. IntendedCases(TestCaseKind.Unit), .. IntendedCases(TestCaseKind.Integration)]);
        TestProjectOwnershipContract.AssertExactlyOnce("both selections, quarantined cases", [.. unit.Quarantined, .. integration.Quarantined], IntendedQuarantinedCases());
        Assert.DoesNotContain(run, item => unit.Quarantined.Contains(item));
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2, AC-7: the integration selection is offered the lane's projects, which are every
    /// integration-owned project the build declares except the two the lane holds out
    /// (QBrainAi.Support.Mcp.EnvironmentIntegrationTests, QBrainAi.McpAgent.IntegrationTests,
    /// QBrainAi.QBAgent.IntegrationTests, NukeBuild.IntegrationTests, QBrainAi.Repl.IntegrationTests and
    /// QBrainAi.Support.Mcp.IntegrationTests), accepts every one with its real declared role and synthetic inventory, and
    /// runs every scripted integration case exactly once. Runtime discovery establishes the current case layout.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S3's lane: the two new integration-owned projects are accepted in place of the three rejected unit projects (185 to 191 accepted cases).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S4's lane: Build.IntegrationTests is accepted in place of the rejected infrastructure-owned Build.Tests (191 to 287 accepted cases).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S5's lane: QBrainAi.Support.Mcp.Environment.IntegrationTests is accepted with its first 13 cases (287 to 300 accepted cases); QBrainAi.Support.Mcp.Tests stays rejected for its role.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S6's lane: QBrainAi.Support.Mcp.EnvironmentIntegrationTests is accepted with 221 cases (300 to 508 accepted cases); QBrainAi.Support.Mcp.Tests stays rejected for its role.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the recorded verdicts name the renamed QBrainAi.Support.Mcp.EnvironmentIntegrationTests and NukeBuild.IntegrationTests (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the verdicts and the 508 accepted cases are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S7's lane: QBrainAi.Support.Mcp.EnvironmentIntegrationTests is accepted with 390 cases (508 to 677 accepted cases); QBrainAi.Support.Mcp.Tests stays rejected for its role. The summary's "Slices S7 and S8 update this record" now reads "Slice S8", since S7 has updated it.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S8's lane: QBrainAi.Support.Mcp.Tests left the lane, and QBrainAi.Support.Mcp.EnvironmentIntegrationTests is accepted with 490 cases (the last 98 moved cases and the two moved collection bindings; 677 to 777 accepted cases), so the integration selection accepts every lane project. The method name is kept for the case's identity; the summary describes the new record.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S9 renames the case from CurrentLayout_IntegrationSelection_RecordsTheLaneProjectsItCannotAccept, which no longer described it, and makes it the plain assertion: the lane's projects are every declared integration-owned project except the two held out, every verdict is Accepted, and the selected cases are every integration case of the inventory exactly once, in place of a recorded verdict list.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: no lane project holds a quarantined case, so the integration selection counts none and still runs the 777 integration cases exactly once.</para>
    /// <para>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R16-F1: the earlier totals are historical; the current binding is explicitly synthetic and asserts no layout count.</para>
    /// </remarks>
    [Fact]
    public void IntegrationSelection_AcceptsEveryLaneProject_AndRunsEveryIntegrationCaseOnce()
    {
        var lane = new IntegrationLaneProjects().Select(IntegrationLaneProjectsContract.Solution);
        var integrationOwned = TestProjectRoles.Declared
            .Where(entry => entry.Role == TestProjectRole.Integration && !HeldOutIntegrationProjects.Contains(entry.Project, StringComparer.Ordinal))
            .Select(entry => entry.Project)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(integrationOwned, lane.Order(StringComparer.Ordinal).ToArray());
        var offered = lane.Select(SyntheticInventory).ToArray();

        var outcome = ownership.Decide(TestSelectionKind.Integration, offered);

        Assert.Equal(lane, outcome.Projects.Select(decision => decision.Project).ToArray());
        Assert.All(outcome.Projects, decision => Assert.Equal(TestProjectVerdict.Accepted, decision.Verdict));
        TestProjectOwnershipContract.AssertExactlyOnce("integration selection", outcome.Cases, IntendedCases(TestCaseKind.Integration));
        Assert.Empty(outcome.Quarantined);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2, AC-7: the infrastructure-owned project the build declares (Build.Tests, the build's own tests)
    /// is offered to neither selection: it is not among the projects Test runs nor among the lane's projects, and no case
    /// of it is selected; together the two selections preserve every offered synthetic unit and integration case, while
    /// the synthetic infrastructure case is offered to neither.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: added in slice S9 of hostile validation finding R14-F4 as part of the plain separated assertion.</para>
    /// <para>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R16-F1: replaces the recorded infrastructure count with one explicitly synthetic case.</para>
    /// </remarks>
    [Fact]
    public void InfrastructureProject_IsOfferedToNeitherSelection()
    {
        string[] expectedInfrastructure = ["Build.Tests"];
        var infrastructure = TestProjectRoles.Declared.Where(entry => entry.Role == TestProjectRole.Infrastructure).Select(entry => entry.Project).ToArray();
        Assert.Equal(expectedInfrastructure, infrastructure);
        var lane = new IntegrationLaneProjects().Select(IntegrationLaneProjectsContract.Solution);
        Assert.DoesNotContain("Build.Tests", UnitSelectionProjects);
        Assert.DoesNotContain("Build.Tests", lane);

        var unit = ownership.Decide(TestSelectionKind.Unit, UnitSelectionProjects.Select(SyntheticInventory).ToArray());
        var integration = ownership.Decide(TestSelectionKind.Integration, lane.Select(SyntheticInventory).ToArray());

        SelectedTestCase[] selected = [.. unit.Cases, .. integration.Cases];
        Assert.DoesNotContain(selected, item => item.Project == "Build.Tests");
        TestProjectOwnershipContract.AssertExactlyOnce("both selections", selected, [.. IntendedCases(TestCaseKind.Unit), .. IntendedCases(TestCaseKind.Integration)]);
        Assert.Single(SyntheticInventory("Build.Tests").Cases, item => item.Kind == TestCaseKind.Infrastructure);
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: every non-quarantined case of one kind in the synthetic inventory.</summary>
    /// <param name="kind">The case kind.</param>
    /// <returns>The cases, in inventory order.</returns>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added in slice S9 for the plain separated assertion.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: leaves out the quarantined cases, which no selection runs.</para>
    /// </remarks>
    private static SelectedTestCase[] IntendedCases(TestCaseKind kind)
        => SyntheticCaseInventory
            .Select(row => SyntheticInventory(row.Project))
            .SelectMany(project => project.Cases.Where(item => item.Kind == kind && !item.Quarantined).Select(item => new SelectedTestCase(project.Name, item.Name)))
            .ToArray();

    /// <summary>TR-MCP-NUKE-019 AC-8: every quarantined case of the synthetic inventory, identified by project and case name.</summary>
    /// <returns>The cases, in inventory order.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static SelectedTestCase[] IntendedQuarantinedCases()
        => SyntheticCaseInventory
            .Select(row => SyntheticInventory(row.Project))
            .SelectMany(project => project.Cases.Where(item => item.Quarantined).Select(item => new SelectedTestCase(project.Name, item.Name)))
            .ToArray();

    /// <summary>TR-MCP-NUKE-019 AC-8: the number of cases per project, in first-seen project order.</summary>
    /// <param name="cases">The cases.</param>
    /// <returns>Each project with its case count.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static (string Project, int Cases)[] CountByProject(IEnumerable<SelectedTestCase> cases)
        => cases.GroupBy(item => item.Project, StringComparer.Ordinal).Select(group => (group.Key, group.Count())).ToArray();

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8 binding adapter: marks each offered case the quarantine row names as quarantined, asks the
    /// production decision, and returns its verdicts, selected cases and quarantined cases in the contract's shape.
    /// </summary>
    /// <param name="selection">The selection.</param>
    /// <param name="offered">The offered projects.</param>
    /// <param name="quarantined">The quarantined cases among them.</param>
    /// <returns>The production outcome in the contract's shape.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private TestProjectOwnershipContract.QuarantineOutcome DecideWithQuarantine(
        TestSelectionKind selection, IReadOnlyList<TestProjectInventory> offered, IReadOnlyList<SelectedTestCase> quarantined)
    {
        var marked = offered
            .Select(project => project with
            {
                Cases = project.Cases.Select(item => item with { Quarantined = quarantined.Contains(new SelectedTestCase(project.Name, item.Name)) }).ToArray(),
            })
            .ToArray();
        var outcome = ownership.Decide(selection, marked);
        return new TestProjectOwnershipContract.QuarantineOutcome(outcome.Projects, outcome.Cases, outcome.Quarantined);
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: the explicitly synthetic inventory of one project, with its real declared role.</summary>
    /// <param name="project">The project name.</param>
    /// <returns>The project with its declared role and its cases, named by kind and ordinal.</returns>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: renamed from CurrentLayoutInventory in slice S9; it reads RecordedCaseInventory (formerly CurrentLayoutCaseCounts); the body is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: adds the project's quarantined cases, of its own kind and marked quarantined.</para>
    /// <para>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7, AC-8; R16-F1: renamed to SyntheticInventory and now reads only the explicitly synthetic rows; the 2026-11-07 recorded-layout description is history.</para>
    /// </remarks>
    private static TestProjectInventory SyntheticInventory(string project)
    {
        var (_, unit, integration, infrastructure, quarantined) = SyntheticCaseInventory.Single(row => row.Project == project);
        var role = TestProjectRoles.RoleOf(project);
        TestCaseEntry[] cases =
        [
            .. Cases(TestCaseKind.Unit, unit),
            .. Cases(TestCaseKind.Integration, integration),
            .. Cases(TestCaseKind.Infrastructure, infrastructure),
            .. QuarantinedCases(KindOf(role), quarantined),
        ];
        return new TestProjectInventory(project, role, cases);
    }

    /// <summary>Ordinal cases of one kind.</summary>
    /// <param name="kind">The case kind.</param>
    /// <param name="count">How many cases.</param>
    /// <returns>The cases.</returns>
    private static IEnumerable<TestCaseEntry> Cases(TestCaseKind kind, int count)
        => Enumerable.Range(1, count).Select(ordinal => new TestCaseEntry($"{kind} case {ordinal}", kind));

    /// <summary>TR-MCP-NUKE-019 AC-8: ordinal quarantined cases of one kind.</summary>
    /// <param name="kind">The case kind.</param>
    /// <param name="count">How many cases.</param>
    /// <returns>The cases, marked quarantined.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static IEnumerable<TestCaseEntry> QuarantinedCases(TestCaseKind kind, int count)
        => Enumerable.Range(1, count).Select(ordinal => new TestCaseEntry($"Quarantined {kind} case {ordinal}", kind, Quarantined: true));

    /// <summary>TR-MCP-NUKE-019 AC-8: the case kind of a declared role, the kind a project's quarantined cases keep.</summary>
    /// <param name="role">The declared role.</param>
    /// <returns>The kind.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static TestCaseKind KindOf(TestProjectRole role) => role switch
    {
        TestProjectRole.Unit => TestCaseKind.Unit,
        TestProjectRole.Integration => TestCaseKind.Integration,
        _ => TestCaseKind.Infrastructure,
    };
}
