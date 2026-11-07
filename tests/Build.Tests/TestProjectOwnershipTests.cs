namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-7 binding (slice S2): runs the <see cref="TestProjectOwnershipContract"/> cases, unchanged from the
/// BDPv4 mock gate ug-r14f4-s1-mockgate-1, over the production <see cref="TestProjectOwnership"/>; pins the build's role
/// declarations (<see cref="TestProjectRoles"/>) and the checks Test and MigrationIntegrationTests make with them; and
/// records the verdicts the real declarations give the current mixed layout.
/// </summary>
/// <remarks>
/// <para>
/// Recorded expectation, not a failure: on the current layout the unit selection rejects the unit-owned projects
/// that still hold Category=Integration cases, and the integration selection rejects the unit-owned and
/// infrastructure-owned projects the lane still runs. That is expected and correct until slices S3 to S8 move the cases
/// into integration-owned projects. The two CurrentLayout cases therefore assert those verdicts by name and pass; each
/// later slice that moves cases updates <see cref="CurrentLayoutCaseCounts"/>, the declarations and the recorded verdicts
/// in the same change, and when slice S9 completes the separation every recorded verdict is Accepted. A verdict that
/// differs from the record, from a change to the declarations, the inventory or the decision, fails these cases.
/// </para>
/// <para>
/// The current layout's case inventory is recorded, not scanned (plan section 27.45: no run-time scanner): the per-project
/// case counts of the da11faa2 TRX set docs/receipts/hv/unitgreen-r14-trx-da11faa2 (unit cases from the Test gate,
/// infrastructure cases from the Build.Tests gate, integration cases from the lane as distinct cases across both hosts),
/// expanded to ordinal case names. Build-infrastructure test, accountable through TR only; it reads no checkout, process
/// environment or test result, so it carries no Integration trait.
/// </para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added the binding of slice S2 for hostile validation finding R14-F4 (operator 2026-11-06: "Separate projects + AC (Recommended)"; MEMORY-BESTPRACTICE-001 rules 4.1, 4.4 and 4.5).</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S3 updates the record: the 1 McpAgent, 2 REPL core and 3 QBAgent integration cases moved to QBrainAi.McpAgent.IntegrationTests, QBrainAi.Repl.IntegrationTests and QBrainAi.QBAgent.IntegrationTests, so the unit selection now accepts QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests, and the integration selection accepts the two new integration-owned projects in the lane; the declarations and the case counts change with them.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S4 updates the record: the 96 integration cases of the infrastructure-owned Build.Tests moved to the integration-owned Build.IntegrationTests, which takes Build.Tests' place in the lane, so the integration selection accepts it and no lane project is rejected as infrastructure; Build.Tests holds only its 467 infrastructure cases and is offered to neither selection.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S5 updates the record: the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests took its first 13 integration cases from QBrainAi.Support.Mcp.Tests (Storage: BugTriage139EighteenthSqliteTests and the two WorkspaceNativePath facade classes) and runs in the lane after it, so the integration selection accepts it; QBrainAi.Support.Mcp.Tests, which still holds 475 integration cases, stays rejected by both selections until slice S8.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S6 updates the record: 208 more integration cases (the whole pure-integration files of nine folders that no unit file pins) moved from QBrainAi.Support.Mcp.Tests to QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the integration selection accepts 221 cases there; QBrainAi.Support.Mcp.Tests, which still holds 267 integration cases, stays rejected by both selections until slice S8.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the project renames that follow slice S6 (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name.") update the record's names only: QBrainAi.Support.Mcp.Environment.IntegrationTests is now QBrainAi.Support.Mcp.EnvironmentIntegrationTests and Build.IntegrationTests is now NukeBuild.IntegrationTests, with the same roles, case counts, run positions and verdicts.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S7 updates the record: the mixed classes of QBrainAi.Support.Mcp.Tests split into their unit halves, which stay, and their integration halves in QBrainAi.Support.Mcp.EnvironmentIntegrationTests, with the pure-integration classes that shared their files, so 169 more integration cases moved there (221 to 390); QBrainAi.Support.Mcp.Tests, which still holds 98 integration cases (the reverse-reference bindings and the classes left for slice S8), stays rejected by both selections until then.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S8 updates the record: the last 98 integration cases of QBrainAi.Support.Mcp.Tests (the classes its collection bindings named, the platform-native cluster and the S7 leftovers) moved to QBrainAi.Support.Mcp.EnvironmentIntegrationTests, and the two collection bindings that name integration classes moved with them as Category=Integration cases (390 to 490 there); QBrainAi.Support.Mcp.Tests holds 3308 unit cases and no integration case, so the unit selection accepts it, and it left the lane, so the integration selection accepts every lane project. Every recorded verdict is now Accepted.</para>
/// </remarks>
public sealed class TestProjectOwnershipTests
{
    /// <summary>The seven projects Test ran at da11faa2 (the unit gate TRX set), in solution order.</summary>
    private static readonly string[] UnitProjectsTestRanAtDa11faa2 =
    [
        "QBrainAi.Support.Mcp.Tests", "QBrainAi.Client.Tests", "QBrainAi.Cqrs.Tests", "QBrainAi.Launcher.Tests",
        "QBrainAi.McpAgent.Tests", "QBrainAi.Repl.Core.Tests", "QBrainAi.QBAgent.Tests",
    ];

    /// <summary>
    /// The current layout's case counts per project: unit, integration and infrastructure cases, the da11faa2 counts
    /// with the cases the completed slices moved. The unit counts add up to 4864 cases per host: the Test gate's 4866 at
    /// da11faa2 less the two collection bindings that slice S8 moved to the integration-owned project as integration cases;
    /// Build.Tests' 467 infrastructure cases and NukeBuild.IntegrationTests' 96 integration cases made the da11faa2 Linux
    /// infrastructure gate's 563, when both were in Build.Tests; QBrainAi.Support.Mcp.IntegrationTests counts the 6 cases
    /// of the two classes the lane runs (its held-out classes are Integration-tagged as well).
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S3 moved 1 case from QBrainAi.McpAgent.Tests to QBrainAi.McpAgent.IntegrationTests, 2 from QBrainAi.Repl.Core.Tests to QBrainAi.Repl.IntegrationTests (179 to 181) and 3 from QBrainAi.QBAgent.Tests to QBrainAi.QBAgent.IntegrationTests; no unit count changed.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S4 moved the 96 integration cases of Build.Tests to Build.IntegrationTests; Build.Tests keeps its 467 infrastructure cases.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S5 moved 13 integration cases (distinct across both hosts: 7 on Linux, 7 on Windows) from QBrainAi.Support.Mcp.Tests (488 to 475) to QBrainAi.Support.Mcp.Environment.IntegrationTests (case-identity reconciliation reconciliation-s5.txt); no unit count changed.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S6 moved 208 integration cases (distinct across both hosts) of 48 whole pure-integration files from QBrainAi.Support.Mcp.Tests (475 to 267) to QBrainAi.Support.Mcp.EnvironmentIntegrationTests (13 to 221; case-identity reconciliation reconciliation-s6.txt); no unit count changed.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: QBrainAi.Support.Mcp.Environment.IntegrationTests and Build.IntegrationTests renamed to QBrainAi.Support.Mcp.EnvironmentIntegrationTests and NukeBuild.IntegrationTests, their root namespaces (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); no count changed.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S7 moved 169 integration cases (distinct across both hosts) of the split mixed classes and the pure classes in their files from QBrainAi.Support.Mcp.Tests (267 to 98) to QBrainAi.Support.Mcp.EnvironmentIntegrationTests (221 to 390; case-identity reconciliation reconciliation-s7.txt); the unit halves keep every unit case, so no unit count changed.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S8 moved the last 98 integration cases (distinct across both hosts) from QBrainAi.Support.Mcp.Tests (98 to 0) to QBrainAi.Support.Mcp.EnvironmentIntegrationTests, and the two collection-membership bindings that name classes there (ProcessEnvironmentCollectionTests.ProcessEnvironmentWriters_AreInASerializedCollection and SqlServerLocalDbCollectionTests.LocalDbClasses_AreInTheSqlServerLocalDbCollection) moved with them and now carry Category=Integration, because an integration-owned project holds only integration cases (3310 to 3308 unit cases; 390 to 490 integration cases; case-identity reconciliation reconciliation-s8-v2.txt).</para>
    /// </remarks>
    private static readonly (string Project, int Unit, int Integration, int Infrastructure)[] CurrentLayoutCaseCounts =
    [
        ("QBrainAi.Support.Mcp.Tests", 3308, 0, 0),
        ("QBrainAi.Support.Mcp.EnvironmentIntegrationTests", 0, 490, 0),
        ("QBrainAi.Client.Tests", 311, 0, 0),
        ("QBrainAi.Cqrs.Tests", 33, 0, 0),
        ("QBrainAi.Launcher.Tests", 20, 0, 0),
        ("QBrainAi.McpAgent.Tests", 64, 0, 0),
        ("QBrainAi.Repl.Core.Tests", 1035, 0, 0),
        ("QBrainAi.QBAgent.Tests", 93, 0, 0),
        ("QBrainAi.McpAgent.IntegrationTests", 0, 1, 0),
        ("QBrainAi.QBAgent.IntegrationTests", 0, 3, 0),
        ("NukeBuild.IntegrationTests", 0, 96, 0),
        ("Build.Tests", 0, 0, 467),
        ("QBrainAi.Repl.IntegrationTests", 0, 181, 0),
        ("QBrainAi.Support.Mcp.IntegrationTests", 0, 6, 0),
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
    /// TR-MCP-NUKE-019 AC-7: Test's check passes for the seven projects its name rules selected at da11faa2, so Test runs
    /// the same projects and cases, and it refuses, naming each, a selected project that is undeclared or declared with
    /// another role, while not naming a project that is declared unit-owned.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void TestSelection_IsDeclaredUnitOwned_AndRefusesAnyOtherProject()
    {
        TestProjectRoles.RequireRole(TestProjectRole.Unit, UnitProjectsTestRanAtDa11faa2, "Test");

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
    /// TR-MCP-NUKE-019 AC-7, recorded expectation for the current layout (see the class remarks): offered Test's seven
    /// projects with their real declared roles and the current inventory, the unit selection accepts all seven,
    /// QBrainAi.Support.Mcp.Tests, QBrainAi.Client.Tests, QBrainAi.Cqrs.Tests, QBrainAi.Launcher.Tests,
    /// QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests, and runs their 4864 cases exactly
    /// once, since slice S8 moved the last Category=Integration cases out of QBrainAi.Support.Mcp.Tests.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S3 moved the integration cases of QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests, so the record accepts them (364 to 1556 accepted cases).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S5 moved 13 of the integration cases of QBrainAi.Support.Mcp.Tests; with 475 still there it stays rejected, so the verdicts and the 1556 accepted cases are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S6 moved 208 more; with 267 still there QBrainAi.Support.Mcp.Tests stays rejected, so the verdicts and the 1556 accepted cases are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S7 moved 169 more; with 98 still there QBrainAi.Support.Mcp.Tests stays rejected, so the verdicts and the 1556 accepted cases are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S8 moved the last 98 and the two collection bindings that name integration classes (now integration cases there), so QBrainAi.Support.Mcp.Tests holds 3308 unit cases and no integration case and is accepted (1556 to 4864 accepted cases); the unit total is 4864, the da11faa2 Test gate's 4866 less those two bindings. The method name is kept for the case's identity; the summary describes the new record.</para>
    /// </remarks>
    [Fact]
    public void CurrentLayout_UnitSelection_RecordsTheMixedUnitProjectsAsRejected()
    {
        Assert.Equal(4864, UnitProjectsTestRanAtDa11faa2.Sum(project => CurrentLayoutCaseCounts.Single(row => row.Project == project).Unit));
        var offered = UnitProjectsTestRanAtDa11faa2.Select(CurrentLayoutInventory).ToArray();

        var outcome = ownership.Decide(TestSelectionKind.Unit, offered);

        TestProjectDecision[] recorded =
        [
            new("QBrainAi.Support.Mcp.Tests", TestProjectVerdict.Accepted),
            new("QBrainAi.Client.Tests", TestProjectVerdict.Accepted),
            new("QBrainAi.Cqrs.Tests", TestProjectVerdict.Accepted),
            new("QBrainAi.Launcher.Tests", TestProjectVerdict.Accepted),
            new("QBrainAi.McpAgent.Tests", TestProjectVerdict.Accepted),
            new("QBrainAi.Repl.Core.Tests", TestProjectVerdict.Accepted),
            new("QBrainAi.QBAgent.Tests", TestProjectVerdict.Accepted),
        ];
        Assert.Equal(recorded, outcome.Projects);
        AssertAcceptedCasesRunOnce("current layout, unit selection", offered, outcome, 4864);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7, recorded expectation for the current layout (see the class remarks): offered the lane's six
    /// projects with their real declared roles and the current inventory, the integration selection accepts every one,
    /// QBrainAi.Support.Mcp.EnvironmentIntegrationTests, QBrainAi.McpAgent.IntegrationTests,
    /// QBrainAi.QBAgent.IntegrationTests, NukeBuild.IntegrationTests, QBrainAi.Repl.IntegrationTests and
    /// QBrainAi.Support.Mcp.IntegrationTests, and runs their 777 cases exactly once; the unit-owned
    /// QBrainAi.Support.Mcp.Tests is no longer a lane project (slice S8).
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
    /// </remarks>
    [Fact]
    public void CurrentLayout_IntegrationSelection_RecordsTheLaneProjectsItCannotAccept()
    {
        var lane = new IntegrationLaneProjects().Select(IntegrationLaneProjectsContract.Solution);
        var offered = lane.Select(CurrentLayoutInventory).ToArray();

        var outcome = ownership.Decide(TestSelectionKind.Integration, offered);

        TestProjectDecision[] recorded =
        [
            new("QBrainAi.Support.Mcp.EnvironmentIntegrationTests", TestProjectVerdict.Accepted),
            new("QBrainAi.McpAgent.IntegrationTests", TestProjectVerdict.Accepted),
            new("QBrainAi.QBAgent.IntegrationTests", TestProjectVerdict.Accepted),
            new("NukeBuild.IntegrationTests", TestProjectVerdict.Accepted),
            new("QBrainAi.Repl.IntegrationTests", TestProjectVerdict.Accepted),
            new("QBrainAi.Support.Mcp.IntegrationTests", TestProjectVerdict.Accepted),
        ];
        Assert.Equal(recorded, outcome.Projects);
        AssertAcceptedCasesRunOnce("current layout, integration selection", offered, outcome, 777);
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: the recorded current-layout inventory of one project, with its real declared role.</summary>
    /// <param name="project">The project name.</param>
    /// <returns>The project with its declared role and its cases, named by kind and ordinal.</returns>
    private static TestProjectInventory CurrentLayoutInventory(string project)
    {
        var (_, unit, integration, infrastructure) = CurrentLayoutCaseCounts.Single(row => row.Project == project);
        TestCaseEntry[] cases =
        [
            .. Cases(TestCaseKind.Unit, unit),
            .. Cases(TestCaseKind.Integration, integration),
            .. Cases(TestCaseKind.Infrastructure, infrastructure),
        ];
        return new TestProjectInventory(project, TestProjectRoles.RoleOf(project), cases);
    }

    /// <summary>Ordinal cases of one kind.</summary>
    /// <param name="kind">The case kind.</param>
    /// <param name="count">How many cases.</param>
    /// <returns>The cases.</returns>
    private static IEnumerable<TestCaseEntry> Cases(TestCaseKind kind, int count)
        => Enumerable.Range(1, count).Select(ordinal => new TestCaseEntry($"{kind} case {ordinal}", kind));

    /// <summary>Asserts the outcome runs every case of the accepted offered projects exactly once, and how many that is.</summary>
    /// <param name="context">The check named in a failure.</param>
    /// <param name="offered">The offered projects.</param>
    /// <param name="outcome">The selection's outcome.</param>
    /// <param name="count">The number of cases the accepted projects hold.</param>
    private static void AssertAcceptedCasesRunOnce(string context, TestProjectInventory[] offered, TestOwnershipOutcome outcome, int count)
    {
        var accepted = offered.Where(project => outcome.Projects.Any(decision => decision.Project == project.Name && decision.Verdict == TestProjectVerdict.Accepted)).ToArray();
        TestProjectOwnershipContract.AssertExactlyOnce(context, outcome.Cases, TestProjectOwnershipContract.CasesOf(accepted));
        Assert.Equal(count, outcome.Cases.Count);
    }
}
