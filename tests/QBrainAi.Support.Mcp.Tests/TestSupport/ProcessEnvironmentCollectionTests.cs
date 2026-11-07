using QBrainAi.Support.Mcp.Tests.Documentation;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CollectionAssertions;
using static QBrainAi.Support.Mcp.Tests.TestSupport.ProcessEnvironmentCollectionTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

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
/// <para>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-9; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the mock gate half of this class. Its binding case (ProcessEnvironmentWriters_AreInASerializedCollection) names test classes that moved to the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"), which this unit project cannot reference, so it moved to the class of the same name there and carries Category=Integration; the collection name both halves use (RequiredCollection) moved once into TestSupport/ProcessEnvironmentCollectionTestsSupport.cs beside this file, which the integration project links (DRY); the three mock gate cases and the scripted types are unchanged.</para>
/// </remarks>
public sealed class ProcessEnvironmentCollectionTests
{
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
