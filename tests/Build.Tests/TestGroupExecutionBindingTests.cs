namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-020 AC-1 to AC-5: binds the unchanged <see cref="TestGroupExecutionContractTests"/> rows and assertions,
/// mock-gated in ug-r9-mockgate-q-2, to the production <see cref="TestGroupExecution"/> on both simulated hosts. The
/// lane's own composition, filter and class-filter bindings (IntegrationLaneExecutionBindingTests, IntegrationLaneFilterTests,
/// IntegrationLaneClassFilterTests) run beside these cases to prove the shared platform and class clauses leave the lane
/// unchanged (AC-3). No checkout, process or environment is read.
/// </summary>
public sealed class TestGroupExecutionBindingTests
{
    /// <summary>The real composition satisfies every accepted and refused row on both hosts, with the project list in either order.</summary>
    [Fact]
    public void ProductionComposition_SatisfiesMockGatedContract() =>
        TestGroupExecutionContractTests.AssertComposition((groups, projects, host) => TestGroupExecution.Compose(groups, projects, host)
            .Select(groupRun => new TestGroupExecutionContractTests.Step(
                groupRun.Run.Name, groupRun.Run.Path, groupRun.Run.Filter, groupRun.Run.LogLine, groupRun.Run.TrxName)).ToArray());

    /// <summary>The real run-id policy passes safe ids through and refuses blank and unsafe ones.</summary>
    [Fact]
    public void ProductionRunId_SatisfiesMockGatedContract() =>
        TestGroupExecutionContractTests.AssertRunId(TestGroupExecution.RequireRunId);

    /// <summary>The real class check accepts a TRX with a case of every named class and names each class that ran none.</summary>
    [Fact]
    public void ProductionClassCheck_SatisfiesMockGatedContract() =>
        TestGroupExecutionContractTests.AssertClassCheck((classes, xml) => TestGroupExecution.CheckClasses("project-under-test", classes, xml));
}
