using Xunit.Sdk;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-7, BDPv4 mock gate: proves the <see cref="TestProjectOwnershipContract"/> assertions accept an
/// independent scripted reference of the ownership decision (the criterion's rule as stated, written here in the test
/// project and never used by production code) over the contract's scripted inventories, and reject deliberately wrong
/// decisions (negative controls), before any production ownership code is written. No checkout, process environment or
/// solution file is read.
/// </summary>
/// <remarks>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added the mock gate of slice S1 for hostile validation finding R14-F4 (operator 2026-11-06: "Separate projects + AC (Recommended)"; MEMORY-BESTPRACTICE-001 rules 4.1, 4.4 and 4.5).</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: adds the quarantine mock gate (ug-qr-mockgate-1, before the production ownership changes): a scripted quarantine reference (the AC-7 reference for the verdicts, then the accepted projects' cases split into cases run and quarantined cases counted apart) satisfies <see cref="TestProjectOwnershipContract.AssertQuarantineContract"/>, and seven wrong references are rejected by it, among them a quarantined case counted as a unit or integration case and a quarantined case dropped from the inventory. The AC-7 cases are unchanged.</para>
/// </remarks>
public sealed class TestProjectOwnershipMockGateTests
{
    /// <summary>TR-MCP-NUKE-019 AC-7 positive control: the scripted reference satisfies every row and refusal of the contract.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void ScriptedOwnership_SatisfiesTheContract()
        => TestProjectOwnershipContract.AssertContract(new ScriptedOwnership(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: under the scripted reference, the unit selection rejects a unit-owned project holding an
    /// integration case (and one holding only integration cases) and accepts the pure unit project beside it, running
    /// only the pure project's cases.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void UnitProject_WithIntegrationCase_IsRejected()
        => TestProjectOwnershipContract.AssertUnitProjectWithIntegrationCaseIsRejected(new ScriptedOwnership(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: under the scripted reference, the integration selection rejects an integration-owned project
    /// holding a unit case (and one holding only unit cases) and accepts the pure integration project beside it.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void IntegrationProject_WithUnitCase_IsRejected()
        => TestProjectOwnershipContract.AssertIntegrationProjectWithUnitCaseIsRejected(new ScriptedOwnership(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: under the scripted reference, neither selection accepts an infrastructure-owned project, even
    /// one whose cases are all of the selection's kind, and no infrastructure case is selected.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void InfrastructureProject_IsAcceptedByNeitherSelection()
        => TestProjectOwnershipContract.AssertInfrastructureProjectIsAcceptedByNeitherSelection(new ScriptedOwnership(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: under the scripted reference, over the separated layout the unit and integration selections
    /// accept their own projects and together run every unit and integration case exactly once, and no infrastructure case.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Fact]
    public void SeparatedProjects_PreserveSelectedCases()
        => TestProjectOwnershipContract.AssertSeparatedProjectsPreserveSelectedCases(new ScriptedOwnership(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7 negative controls, each rejected inside this passing harness by the contract check it targets:
    /// - a decision that accepts a mixed project (checked by both named rejection groups);
    /// - a decision that omits a selected case;
    /// - a decision that duplicates a selected case;
    /// - a decision that lets an infrastructure-owned project into a selection;
    /// - a decision that accepts a project of the other test role whose cases match the selection;
    /// - a decision that runs the cases of rejected projects;
    /// - a decision that accepts a malformed offer (a project twice, or a case twice in one project);
    /// - a decision that drops a case whose name another project also uses.
    /// </summary>
    /// <param name="fault">The injected fault.</param>
    /// <param name="check">The contract check that must reject it.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    [Theory]
    [InlineData(Fault.AcceptsMixedProject, Check.UnitProjectWithIntegrationCase)]
    [InlineData(Fault.AcceptsMixedProject, Check.IntegrationProjectWithUnitCase)]
    [InlineData(Fault.OmitsCase, Check.SeparatedProjects)]
    [InlineData(Fault.DuplicatesCase, Check.SeparatedProjects)]
    [InlineData(Fault.LetsInfrastructureIn, Check.InfrastructureProject)]
    [InlineData(Fault.AcceptsOtherRole, Check.Contract)]
    [InlineData(Fault.KeepsRejectedCases, Check.UnitProjectWithIntegrationCase)]
    [InlineData(Fault.IgnoresDuplicates, Check.Contract)]
    [InlineData(Fault.DedupesByCaseName, Check.SeparatedProjects)]
    public void WrongOwnership_IsRejected(Fault fault, Check check)
    {
        var wrong = new ScriptedOwnership(fault);
        Action assertion = check switch
        {
            Check.UnitProjectWithIntegrationCase => () => TestProjectOwnershipContract.AssertUnitProjectWithIntegrationCaseIsRejected(wrong),
            Check.IntegrationProjectWithUnitCase => () => TestProjectOwnershipContract.AssertIntegrationProjectWithUnitCaseIsRejected(wrong),
            Check.InfrastructureProject => () => TestProjectOwnershipContract.AssertInfrastructureProjectIsAcceptedByNeitherSelection(wrong),
            Check.SeparatedProjects => () => TestProjectOwnershipContract.AssertSeparatedProjectsPreserveSelectedCases(wrong),
            _ => () => TestProjectOwnershipContract.AssertContract(wrong),
        };
        Assert.ThrowsAny<XunitException>(assertion);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8 positive control: the scripted quarantine reference satisfies every quarantine row and every
    /// AC-7 row with nothing quarantined.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Fact]
    public void ScriptedQuarantine_SatisfiesTheQuarantineContract()
        => TestProjectOwnershipContract.AssertQuarantineContract((selection, offered, quarantined) => ScriptedQuarantine(QuarantineFault.None, selection, offered, quarantined));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8 negative controls, each rejected inside this passing harness by the quarantine contract:
    /// - a quarantined case counted as a unit or integration case (run with the selection's cases);
    /// - a quarantined case dropped from the inventory (neither run nor counted);
    /// - a quarantined case both run and counted;
    /// - a quarantine matched by case name across projects;
    /// - a quarantine that hides a case of another kind from the ownership judgment;
    /// - a decision that drops a case that is not quarantined;
    /// - a decision that counts the quarantined cases of a project it rejects.
    /// </summary>
    /// <param name="fault">The injected fault.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Theory]
    [InlineData(QuarantineFault.RunsQuarantinedCase)]
    [InlineData(QuarantineFault.DropsQuarantinedCase)]
    [InlineData(QuarantineFault.RunsAndCountsQuarantinedCase)]
    [InlineData(QuarantineFault.QuarantinesByCaseName)]
    [InlineData(QuarantineFault.HidesForeignCase)]
    [InlineData(QuarantineFault.DropsRunnableCase)]
    [InlineData(QuarantineFault.CountsRejectedProjectQuarantine)]
    public void WrongQuarantine_IsRejected(QuarantineFault fault)
        => Assert.ThrowsAny<XunitException>(() => TestProjectOwnershipContract.AssertQuarantineContract(
            (selection, offered, quarantined) => ScriptedQuarantine(fault, selection, offered, quarantined)));

    /// <summary>TR-MCP-NUKE-019 AC-8: faults injected into the scripted quarantine reference for the negative controls.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    public enum QuarantineFault
    {
        /// <summary>No fault: the criterion's rule.</summary>
        None,

        /// <summary>Runs a quarantined case with the selection's cases instead of counting it apart.</summary>
        RunsQuarantinedCase,

        /// <summary>Neither runs nor counts a quarantined case.</summary>
        DropsQuarantinedCase,

        /// <summary>Runs a quarantined case and also counts it apart.</summary>
        RunsAndCountsQuarantinedCase,

        /// <summary>Treats every case whose name a quarantined case uses as quarantined, in every project.</summary>
        QuarantinesByCaseName,

        /// <summary>Judges a project's kinds without its quarantined cases, so a quarantine hides a misplaced case.</summary>
        HidesForeignCase,

        /// <summary>Drops the last case that is not quarantined.</summary>
        DropsRunnableCase,

        /// <summary>Counts the quarantined cases of a rejected project.</summary>
        CountsRejectedProjectQuarantine,
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8 scripted quarantine reference: the verdicts are the AC-7 reference's (the role, then the kind
    /// of every case, quarantined or not); each accepted project's cases that are not quarantined run, in offer and
    /// inventory order, and its quarantined cases are counted apart; a rejected project's cases are neither; with an
    /// optional injected fault.
    /// </summary>
    /// <param name="fault">The injected fault.</param>
    /// <param name="selection">The selection.</param>
    /// <param name="offered">The offered projects.</param>
    /// <param name="quarantined">The quarantined cases among them.</param>
    /// <returns>The verdicts, the cases run and the quarantined cases counted apart.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static TestProjectOwnershipContract.QuarantineOutcome ScriptedQuarantine(
        QuarantineFault fault, TestSelectionKind selection, IReadOnlyList<TestProjectInventory> offered, IReadOnlyList<SelectedTestCase> quarantined)
    {
        bool IsQuarantined(string project, string name) => fault == QuarantineFault.QuarantinesByCaseName
            ? quarantined.Any(item => item.Case == name)
            : quarantined.Contains(new SelectedTestCase(project, name));

        var judged = fault == QuarantineFault.HidesForeignCase
            ? offered.Select(project => project with { Cases = project.Cases.Where(item => !IsQuarantined(project.Name, item.Name)).ToArray() }).ToArray()
            : offered.ToArray();
        var verdicts = new ScriptedOwnership(Fault.None).Decide(selection, judged).Projects;
        var run = new List<SelectedTestCase>();
        var counted = new List<SelectedTestCase>();
        for (var index = 0; index < offered.Count; index++)
        {
            var project = offered[index];
            var accepted = verdicts[index].Verdict == TestProjectVerdict.Accepted;
            foreach (var item in project.Cases)
            {
                var identity = new SelectedTestCase(project.Name, item.Name);
                var isQuarantined = IsQuarantined(project.Name, item.Name);
                if (!accepted)
                {
                    if (isQuarantined && fault == QuarantineFault.CountsRejectedProjectQuarantine)
                        counted.Add(identity);
                    continue;
                }

                if (!isQuarantined || fault == QuarantineFault.RunsQuarantinedCase || fault == QuarantineFault.RunsAndCountsQuarantinedCase)
                    run.Add(identity);
                if (isQuarantined && fault != QuarantineFault.RunsQuarantinedCase && fault != QuarantineFault.DropsQuarantinedCase)
                    counted.Add(identity);
            }
        }

        if (fault == QuarantineFault.DropsRunnableCase && run.Count > 0)
            run.RemoveAt(run.Count - 1);
        return new TestProjectOwnershipContract.QuarantineOutcome(verdicts, run, counted);
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: faults injected into the scripted reference for the negative controls.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    public enum Fault
    {
        /// <summary>No fault: the criterion's rule.</summary>
        None,

        /// <summary>Skips the case check, so a project of the selection's role holding a case of another kind is accepted.</summary>
        AcceptsMixedProject,

        /// <summary>Drops the last selected case.</summary>
        OmitsCase,

        /// <summary>Selects the first selected case a second time.</summary>
        DuplicatesCase,

        /// <summary>Skips the infrastructure role check, so an infrastructure-owned project is judged by its cases alone.</summary>
        LetsInfrastructureIn,

        /// <summary>Skips the other-role check, so a project of the other test role is judged by its cases alone.</summary>
        AcceptsOtherRole,

        /// <summary>Selects the cases of rejected projects as well.</summary>
        KeepsRejectedCases,

        /// <summary>Does not refuse a project offered twice or a case listed twice.</summary>
        IgnoresDuplicates,

        /// <summary>Keeps only the first selected case of each case name, across projects.</summary>
        DedupesByCaseName,
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: the contract check a negative control targets.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    public enum Check
    {
        /// <summary>The whole contract (<see cref="TestProjectOwnershipContract.AssertContract"/>).</summary>
        Contract,

        /// <summary>The unit-owned project holding an integration case rows.</summary>
        UnitProjectWithIntegrationCase,

        /// <summary>The integration-owned project holding a unit case rows.</summary>
        IntegrationProjectWithUnitCase,

        /// <summary>The infrastructure-owned project rows.</summary>
        InfrastructureProject,

        /// <summary>The separated layout rows and the both-selections preservation check.</summary>
        SeparatedProjects,
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7 scripted reference: the criterion's rule as stated, with an optional injected fault. A
    /// malformed offer is refused first; then each offered project is judged in offer order: infrastructure-owned is
    /// rejected, the other test role is rejected, a case of another kind than the selection's is rejected, anything else
    /// is accepted; the accepted projects' cases are selected in offer and inventory order.
    /// </summary>
    /// <param name="fault">The injected fault.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; lists only, no dictionary, so a fault never throws an incidental ArgumentException.</remarks>
    private sealed class ScriptedOwnership(Fault fault) : ITestProjectOwnership
    {
        /// <inheritdoc />
        public TestOwnershipOutcome Decide(TestSelectionKind selection, IReadOnlyList<TestProjectInventory> offered)
        {
            ArgumentNullException.ThrowIfNull(offered);
            if (fault != Fault.IgnoresDuplicates)
                RefuseDuplicates(offered);

            var ownRole = selection == TestSelectionKind.Unit ? TestProjectRole.Unit : TestProjectRole.Integration;
            var ownKind = selection == TestSelectionKind.Unit ? TestCaseKind.Unit : TestCaseKind.Integration;
            var decisions = new List<TestProjectDecision>();
            var selected = new List<SelectedTestCase>();
            foreach (var project in offered)
            {
                var verdict = Judge(project, ownRole, ownKind);
                decisions.Add(new TestProjectDecision(project.Name, verdict));
                if (verdict == TestProjectVerdict.Accepted || fault == Fault.KeepsRejectedCases)
                    selected.AddRange(project.Cases.Select(item => new SelectedTestCase(project.Name, item.Name)));
            }

            if (fault == Fault.OmitsCase && selected.Count > 0)
                selected.RemoveAt(selected.Count - 1);
            if (fault == Fault.DuplicatesCase && selected.Count > 0)
                selected.Add(selected[0]);
            if (fault == Fault.DedupesByCaseName)
                selected = selected.Where((item, index) => selected.FindIndex(other => other.Case == item.Case) == index).ToList();

            return new TestOwnershipOutcome(decisions, selected);
        }

        /// <summary>Judges one project: role first (infrastructure, then the other test role), then its cases.</summary>
        /// <param name="project">The offered project.</param>
        /// <param name="ownRole">The role the selection accepts.</param>
        /// <param name="ownKind">The case kind the selection accepts.</param>
        /// <returns>The verdict.</returns>
        private TestProjectVerdict Judge(TestProjectInventory project, TestProjectRole ownRole, TestCaseKind ownKind)
        {
            if (project.Role == TestProjectRole.Infrastructure && fault != Fault.LetsInfrastructureIn)
                return TestProjectVerdict.RejectedInfrastructure;
            if (project.Role != TestProjectRole.Infrastructure && project.Role != ownRole && fault != Fault.AcceptsOtherRole)
                return TestProjectVerdict.RejectedOtherRole;
            if (fault != Fault.AcceptsMixedProject && project.Cases.Any(item => item.Kind != ownKind))
                return TestProjectVerdict.RejectedForeignCase;
            return TestProjectVerdict.Accepted;
        }

        /// <summary>Refuses a project offered twice or a case listed twice in one project, naming it.</summary>
        /// <param name="offered">The offered projects.</param>
        private static void RefuseDuplicates(IReadOnlyList<TestProjectInventory> offered)
        {
            for (var index = 0; index < offered.Count; index++)
            {
                var project = offered[index];
                for (var earlier = 0; earlier < index; earlier++)
                {
                    if (offered[earlier].Name == project.Name)
                        throw new ArgumentException($"Project '{project.Name}' is offered more than once.", nameof(offered));
                }

                for (var item = 0; item < project.Cases.Count; item++)
                {
                    for (var earlier = 0; earlier < item; earlier++)
                    {
                        if (project.Cases[earlier].Name == project.Cases[item].Name)
                            throw new ArgumentException($"Project '{project.Name}' lists case '{project.Cases[item].Name}' more than once.", nameof(offered));
                    }
                }
            }
        }
    }
}
