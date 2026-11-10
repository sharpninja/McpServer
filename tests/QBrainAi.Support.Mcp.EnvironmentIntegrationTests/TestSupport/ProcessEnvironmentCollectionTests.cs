using QBrainAi.Support.Mcp.Tests.Documentation;
using QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Documentation;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CollectionAssertions;
using static QBrainAi.Support.Mcp.Tests.TestSupport.ProcessEnvironmentCollectionTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.TestSupport;

/// <summary>
/// Tests that the test classes which write the process environment run one at a time: each belongs to an xUnit
/// collection with parallelization disabled, so no two of them, and no other test, run while one of them has a
/// variable changed.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-TESTRESOURCE-001 ac-9; TR-MCP-USECASE-018 AC-006 ("the tests that write the process
/// environment run one at a time"; operator 2026-10-21: "Fix both, strengthen ACs" and "Approve, strengthen AC-006/008
/// (Recommended)"; hostile validation round 5 finding R5-F2). Test data: scripted nested types carrying a collection
/// attribute with the required name, another name, or none; the attributes are read through the shared
/// <see cref="CollectionAssertions"/>, so no test is run. Negative controls prove the membership assertion rejects a
/// type outside any collection and a type in another collection; the definition assertion's own controls are in
/// <c>SqlServerLocalDbCollectionTests</c>. The binding checks the real definition and the classes that write the
/// process environment, found by the one-time environment sweep of plan section 19.3 and its extension through
/// production code in plan section 20 (the one-shot strategy tests, the two Codex platform classes and the
/// <see cref="CliTempVariables"/> tests, which write the CLI temp variables; hostile validation finding R6-F1), and the
/// desktop spawner's environment-delta case, which owns its GH_TOKEN baseline (hostile validation round 8b finding
/// R8B-F1). Stated
/// limit: the list is not discovered from source; a new class that writes the process environment must be added to it.
/// <para>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-9; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the binding half of this class, split from QBrainAi.Support.Mcp.Tests (TestSupport/ProcessEnvironmentCollectionTests.cs), where its mock gate cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests together with the classes it binds (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its one case now carries Category=Integration because this project holds only integration cases, and it names the bound classes in this project's namespace (see the entry on that case); the collection name both halves use comes from TestSupport/ProcessEnvironmentCollectionTestsSupport.cs of QBrainAi.Support.Mcp.Tests, and CollectionAssertions, ProcessEnvironmentCollection and RepositoryRootProcessStateCollection are linked into this assembly (DRY).</para>
/// </remarks>
public sealed class ProcessEnvironmentCollectionTests
{
    /// <summary>
    /// Binding: the collection is defined with the required name and parallelization disabled (the same definition
    /// assertion the scripted definitions of <c>SqlServerLocalDbCollectionTests</c> passed and failed); each class the
    /// sweep found writing the process environment passes the same membership assertion; and the repository-root
    /// writer stays in its own serialized collection.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-9; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved from QBrainAi.Support.Mcp.Tests with the classes it binds (slice S8, hostile validation finding R14-F4). It names test classes that now live in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, which a unit project cannot reference, and that project holds only integration cases, so this binding now carries Category=Integration (where it runs, not what it checks); the typeof names that were qualified with the QBrainAi.Support.Mcp.Tests namespace now give this project's namespace, so each names the class of the same name here, whose cases write the process environment, and the two unqualified names resolve to the classes of this project; the definition and membership assertions are unchanged.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void ProcessEnvironmentWriters_AreInASerializedCollection()
    {
        Assert.Equal(RequiredCollection, ProcessEnvironmentCollection.Name);
        AssertSerializedDefinition(typeof(ProcessEnvironmentCollection), RequiredCollection);

        foreach (var type in new[]
                 {
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Options.ProductAliasCompatibilityTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Options.McpInstanceResolverTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.BrainSlotCredentialResolverTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory.MemoryIndexerTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.CliBrainSlotStrategyTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.CodexCliAgentExecutionStrategyTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.DatabaseMaintenance.McpDatabaseEncryptionTransitionCommandTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.ProcessEnvironmentServiceTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory.MemoryBenchHarnessTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.OneShotCliAgentExecutionStrategyTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.CodexCliAgentExecutionStrategyWindowsTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.CodexCliAgentExecutionStrategyLinuxTests),
                     typeof(CliTempVariablesTests),
                     typeof(QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services.DesktopProcessSpawnerTests),
                 })
        {
            AssertInCollection(type, RequiredCollection);
        }

        AssertInCollection(typeof(BugTriage139FourteenthHarnessTests), RepositoryRootProcessStateCollection.Name);
    }
}
