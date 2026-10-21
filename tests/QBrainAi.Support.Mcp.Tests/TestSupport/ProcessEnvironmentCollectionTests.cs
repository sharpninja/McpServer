using QBrainAi.Support.Mcp.Tests.Documentation;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CollectionAssertions;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests that the test classes which write the process environment run one at a time: each belongs to an xUnit
/// collection with parallelization disabled, so no two of them, and no other test, run while one of them has a
/// variable changed.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 ("the tests that write the process
/// environment run one at a time"; operator 2026-10-21: "Fix both, strengthen ACs" and "Approve, strengthen AC-006/008
/// (Recommended)"; hostile validation round 5 finding R5-F2). Test data: scripted nested types carrying a collection
/// attribute with the required name, another name, or none; the attributes are read through the shared
/// <see cref="CollectionAssertions"/>, so no test is run. Negative controls prove the membership assertion rejects a
/// type outside any collection and a type in another collection; the definition assertion's own controls are in
/// <c>SqlServerLocalDbCollectionTests</c>. The binding checks the real definition and the classes that write the
/// process environment, found by the one-time environment sweep of plan section 19.3 (stated limit: the list is not
/// discovered from source; a new class that writes the process environment must be added to it).
/// </remarks>
public sealed class ProcessEnvironmentCollectionTests
{
    private const string RequiredCollection = "Process environment";

    /// <summary>Mock gate positive control: a scripted type in the required collection passes the assertion.</summary>
    [Fact]
    public void ScriptedMember_PassesTheMembershipAssertion() =>
        AssertInCollection(typeof(ScriptedMember), RequiredCollection);

    /// <summary>
    /// Mock gate negative controls: a scripted type with no collection, one in another collection, and one in the
    /// repository-root collection are each rejected by the membership assertion for the required collection, inside
    /// a passing harness.
    /// </summary>
    [Fact]
    public void MembershipNegativeControls_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertInCollection(typeof(ScriptedOutsider), RequiredCollection)));
        Assert.NotNull(Record.Exception(() => AssertInCollection(typeof(ScriptedOtherMember), RequiredCollection)));
        Assert.NotNull(Record.Exception(() => AssertInCollection(typeof(ScriptedRepositoryRootMember), RequiredCollection)));
    }

    /// <summary>
    /// Mock gate positive control for the existing serialized collection that already holds the repository-root
    /// writer: its definition passes the definition assertion.
    /// </summary>
    [Fact]
    public void RepositoryRootCollection_IsSerialized() =>
        AssertSerializedDefinition(typeof(RepositoryRootProcessStateCollection), RepositoryRootProcessStateCollection.Name);

    /// <summary>
    /// Binding: the collection is defined with the required name and parallelization disabled (the same definition
    /// assertion the scripted definitions of <c>SqlServerLocalDbCollectionTests</c> passed and failed); each class the
    /// sweep found writing the process environment passes the same membership assertion; and the repository-root
    /// writer stays in its own serialized collection.
    /// </summary>
    [Fact]
    public void ProcessEnvironmentWriters_AreInASerializedCollection()
    {
        Assert.Equal(RequiredCollection, ProcessEnvironmentCollection.Name);
        AssertSerializedDefinition(typeof(ProcessEnvironmentCollection), RequiredCollection);

        foreach (var type in new[]
                 {
                     typeof(QBrainAi.Support.Mcp.Tests.Options.ProductAliasCompatibilityTests),
                     typeof(QBrainAi.Support.Mcp.Tests.Options.McpInstanceResolverTests),
                     typeof(QBrainAi.Support.Mcp.Tests.Services.BrainSlotCredentialResolverTests),
                     typeof(QBrainAi.Support.Mcp.Tests.Memory.MemoryIndexerTests),
                     typeof(QBrainAi.Support.Mcp.Tests.Services.CliBrainSlotStrategyTests),
                     typeof(QBrainAi.Support.Mcp.Tests.Services.CodexCliAgentExecutionStrategyTests),
                     typeof(QBrainAi.Support.Mcp.Tests.DatabaseMaintenance.McpDatabaseEncryptionTransitionCommandTests),
                     typeof(QBrainAi.Support.Mcp.Tests.Services.ProcessEnvironmentServiceTests),
                     typeof(QBrainAi.Support.Mcp.Tests.Memory.MemoryBenchHarnessTests),
                 })
        {
            AssertInCollection(type, RequiredCollection);
        }

        AssertInCollection(typeof(BugTriage139FourteenthHarnessTests), RepositoryRootProcessStateCollection.Name);
    }

    /// <summary>Scripted type in the required collection; it holds no tests.</summary>
    [Collection(RequiredCollection)]
    private sealed class ScriptedMember
    {
    }

    /// <summary>Scripted type in no collection.</summary>
    private sealed class ScriptedOutsider
    {
    }

    /// <summary>Scripted type in another collection; it holds no tests.</summary>
    [Collection("Another collection")]
    private sealed class ScriptedOtherMember
    {
    }

    /// <summary>Scripted type in the repository-root collection; it holds no tests.</summary>
    [Collection(RepositoryRootProcessStateCollection.Name)]
    private sealed class ScriptedRepositoryRootMember
    {
    }
}
