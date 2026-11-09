namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-7 scripted discovery contract: proves ownership inventories come from discovered identities and
/// that role/category drift fails the owning target instead of disappearing behind a test filter.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: added before binding the runtime list-tests source to
/// Test and MigrationIntegrationTests. The scripted source is hermetic and each negative control changes one discovery
/// result while keeping the declared role fixed.
/// </remarks>
public sealed class TestProjectCaseDiscoveryTests
{
    /// <summary>Proves the inventory identities and kinds come from the discovery source, with no recorded-count input.</summary>
    /// <remarks>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: required discovery-source positive control.</remarks>
    [Fact]
    public void OwnershipInventory_ComesFromDiscoveredCases_NotRecordedCounts()
    {
        var source = new ScriptedDiscoverySource(
            all: ["UnitCase", "IntegrationCase", "QuarantinedCase"],
            integration: ["IntegrationCase"],
            quarantined: ["QuarantinedCase"]);

        var inventory = TestProjectCaseDiscovery.Discover("Project.Tests", "Project.Tests.csproj", TestProjectRole.Unit, source);

        Assert.Equal([null, "Category=Integration", "Quarantine=NoMatchingCriterion"], source.Filters);
        Assert.Collection(
            inventory.Cases,
            item => Assert.Equal(new TestCaseEntry("UnitCase", TestCaseKind.Unit), item),
            item => Assert.Equal(new TestCaseEntry("IntegrationCase", TestCaseKind.Integration), item),
            item => Assert.Equal(new TestCaseEntry("QuarantinedCase", TestCaseKind.Unit, Quarantined: true), item));
    }

    /// <summary>Proves Test fails when runtime discovery finds an integration case in a unit-owned project.</summary>
    /// <remarks>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: required unit-target negative control.</remarks>
    [Fact]
    public void UnitOwnedProject_WithDiscoveredIntegrationCase_FailsTestTarget()
    {
        var inventory = TestProjectCaseDiscovery.Discover(
            "Project.Tests",
            "Project.Tests.csproj",
            TestProjectRole.Unit,
            new ScriptedDiscoverySource(["UnitCase", "IntegrationCase"], ["IntegrationCase"], []));
        var outcome = new TestProjectOwnership().Decide(TestSelectionKind.Unit, [inventory]);

        var failure = Assert.Throws<InvalidOperationException>(() => TestProjectOwnershipGate.RequireAccepted("Test", outcome));

        Assert.Contains("Project.Tests (RejectedForeignCase)", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>Proves the integration lane fails when runtime discovery finds a case without Category=Integration.</summary>
    /// <remarks>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: required lane negative control.</remarks>
    [Fact]
    public void IntegrationOwnedProject_WithDiscoveredUnitCase_FailsLane()
    {
        var inventory = TestProjectCaseDiscovery.Discover(
            "Project.IntegrationTests",
            "Project.IntegrationTests.csproj",
            TestProjectRole.Integration,
            new ScriptedDiscoverySource(["IntegrationCase", "MissingTraitCase"], ["IntegrationCase"], []));
        var outcome = new TestProjectOwnership().Decide(TestSelectionKind.Integration, [inventory]);

        var failure = Assert.Throws<InvalidOperationException>(() => TestProjectOwnershipGate.RequireAccepted("MigrationIntegrationTests", outcome));

        Assert.Contains("Project.IntegrationTests (RejectedForeignCase)", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>Proves one newly discovered foreign case invalidates a previously clean recorded count.</summary>
    /// <remarks>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: required stale-count drift negative control.</remarks>
    [Fact]
    public void RecordedCountDrift_IsRejected()
    {
        const int historicalRecordedUnitCount = 1;
        var source = new ScriptedDiscoverySource(
            ["HistoricalUnitCase", "NewIntegrationCase"],
            ["NewIntegrationCase"],
            []);
        var inventory = TestProjectCaseDiscovery.Discover("Project.Tests", "Project.Tests.csproj", TestProjectRole.Unit, source);
        var outcome = new TestProjectOwnership().Decide(TestSelectionKind.Unit, [inventory]);

        Assert.Equal(historicalRecordedUnitCount + 1, inventory.Cases.Count);
        Assert.Throws<InvalidOperationException>(() => TestProjectOwnershipGate.RequireAccepted("Test", outcome));
    }

    /// <summary>Proves the runtime-output parser preserves indented identities and treats an empty filtered list as empty.</summary>
    /// <remarks>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: binds the scripted contract to VSTest output shape.</remarks>
    [Fact]
    public void DotNetListTestDiscovery_ParsesCasesAndEmptyFilterResult()
    {
        var cases = DotNetListTestDiscoverySource.Parse(
            ["build output", "The following Tests are available:", "    Namespace.Class.Case", "    Namespace.Class.Theory(value: 1)"]);
        var empty = DotNetListTestDiscoverySource.Parse(
            ["The following Tests are available:", "No test matches the given testcase filter `Category=Integration` in Project.Tests.dll"]);

        Assert.Equal(["Namespace.Class.Case", "Namespace.Class.Theory(value: 1)"], cases);
        Assert.Empty(empty);
    }

    /// <summary>Hermetic discovery source keyed by the exact filters the production composer requests.</summary>
    /// <remarks>2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: added with the scripted discovery contract.</remarks>
    private sealed class ScriptedDiscoverySource(
        IReadOnlyList<string> all,
        IReadOnlyList<string> integration,
        IReadOnlyList<string> quarantined) : ITestCaseDiscoverySource
    {
        private readonly List<string?> filters = [];

        /// <summary>Gets the requested filters in call order.</summary>
        internal IReadOnlyList<string?> Filters => filters;

        /// <inheritdoc />
        public IReadOnlyList<string> Discover(string projectPath, string? filter)
        {
            filters.Add(filter);
            return filter switch
            {
                null => all,
                "Category=Integration" => integration,
                "Quarantine=NoMatchingCriterion" => quarantined,
                _ => throw new InvalidOperationException($"Unexpected scripted filter '{filter}'."),
            };
        }
    }
}
