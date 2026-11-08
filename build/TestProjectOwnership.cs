/// <summary>
/// TR-MCP-NUKE-019 AC-7: production <see cref="ITestProjectOwnership"/>. Given each offered project's declared role
/// (<see cref="TestProjectRoles"/>) and a supplied case inventory, the unit selection accepts only unit-owned projects
/// whose cases are all unit cases, the integration selection accepts only integration-owned projects whose cases are all
/// integration cases, and neither accepts an infrastructure-owned project; the selection runs every case of every
/// accepted project exactly once. It reads no checkout, solution, process or test result: the inventory is supplied.
/// </summary>
/// <remarks>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added in slice S2 after mock gate ug-r14f4-s1-mockgate-1, for hostile validation finding R14-F4 (operator 2026-11-06: "Separate projects + AC (Recommended)"; MEMORY-BESTPRACTICE-001 rules 4.1 and 4.4); bound by TestProjectOwnershipTests to the contract cases the mock gate ran.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: an accepted project's quarantined cases are counted apart and not selected; the judgment is unchanged (after mock gate ug-qr-mockgate-1; bound by TestProjectOwnershipTests to its quarantine contract).</para>
/// </remarks>
sealed class TestProjectOwnership : ITestProjectOwnership
{
    /// <inheritdoc />
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: the accepted projects' cases split into the selected cases, which hold no quarantined case, and the quarantined cases counted apart.</para>
    /// </remarks>
    public TestOwnershipOutcome Decide(TestSelectionKind selection, IReadOnlyList<TestProjectInventory> offered)
    {
        ArgumentNullException.ThrowIfNull(offered);
        var (ownRole, ownKind) = selection switch
        {
            TestSelectionKind.Unit => (TestProjectRole.Unit, TestCaseKind.Unit),
            TestSelectionKind.Integration => (TestProjectRole.Integration, TestCaseKind.Integration),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, "Only the unit and integration selections judge test-project ownership."),
        };

        RefuseMalformed(selection, offered);
        var decisions = offered.Select(project => new TestProjectDecision(project.Name, Judge(project, ownRole, ownKind))).ToArray();
        var accepted = offered
            .Where((_, index) => decisions[index].Verdict == TestProjectVerdict.Accepted)
            .SelectMany(project => project.Cases.Select(item => (Case: new SelectedTestCase(project.Name, item.Name), item.Quarantined)))
            .ToArray();
        return new TestOwnershipOutcome(decisions, accepted.Where(item => !item.Quarantined).Select(item => item.Case).ToArray())
        {
            Quarantined = accepted.Where(item => item.Quarantined).Select(item => item.Case).ToArray(),
        };
    }

    /// <summary>TR-MCP-NUKE-019 AC-7: the role is judged first (infrastructure, then the other test role), then the cases.</summary>
    /// <param name="project">The offered project.</param>
    /// <param name="ownRole">The role the selection accepts.</param>
    /// <param name="ownKind">The case kind the selection accepts.</param>
    /// <returns>The verdict.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    static TestProjectVerdict Judge(TestProjectInventory project, TestProjectRole ownRole, TestCaseKind ownKind) => project.Role switch
    {
        TestProjectRole.Infrastructure => TestProjectVerdict.RejectedInfrastructure,
        var role when role != ownRole => TestProjectVerdict.RejectedOtherRole,
        _ => project.Cases.All(item => item.Kind == ownKind) ? TestProjectVerdict.Accepted : TestProjectVerdict.RejectedForeignCase,
    };

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: refuses an offer that would make "exactly once" ambiguous: a project offered twice, or a
    /// project that lists a case name twice; the message names the project and the case.
    /// </summary>
    /// <param name="selection">The selection, named in the message.</param>
    /// <param name="offered">The offered projects.</param>
    /// <exception cref="ArgumentException">A project or a case name repeats.</exception>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    static void RefuseMalformed(TestSelectionKind selection, IReadOnlyList<TestProjectInventory> offered)
    {
        var projects = new HashSet<string>(StringComparer.Ordinal);
        foreach (var project in offered)
        {
            ArgumentNullException.ThrowIfNull(project, nameof(offered));
            if (!projects.Add(project.Name))
                throw new ArgumentException($"Test project '{project.Name}' is offered to the {selection} selection more than once.", nameof(offered));

            var cases = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in project.Cases)
            {
                if (!cases.Add(item.Name))
                    throw new ArgumentException($"Test project '{project.Name}' lists case '{item.Name}' more than once.", nameof(offered));
            }
        }
    }
}
