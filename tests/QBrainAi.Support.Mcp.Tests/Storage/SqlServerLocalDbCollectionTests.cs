using System.Reflection;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CollectionAssertions;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// Tests that the test classes which open connections to the shared LocalDB instance belong to one xUnit collection,
/// so they run one after another instead of creating and migrating databases on the same instance at once.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-TESTRESOURCE-001 ac-7 (serves TR-MCP-NUKE-019 AC-2). Found by the Windows interactive lane ug-r3-win-lane-4, where a migration command of
/// SqlServerAuditPayloadMigrationTests waited past its 120 s command timeout while several SQL Server test classes
/// migrated fresh databases on one LocalDB instance; fix chosen by the operator on 2026-10-20 ("Serialize SQL Server
/// tests (Recommended)"), whose option text says the classes "join one xUnit collection with parallelization
/// disabled, so they run one at a time against LocalDB". Test data: scripted nested types carrying a collection
/// attribute with the required name, another name, or none, and scripted collection definitions with the flag set,
/// absent or false (each under its own name, so no definition duplicates the real one, and no class is a member); the
/// attributes are read through <see cref="CustomAttributeData"/>, so no test is run and no server is contacted.
/// Negative controls prove the membership assertion rejects a type outside any collection and a type in another
/// collection, and the definition assertion rejects a definition without the flag, one with the flag false, a wrong
/// name and a missing definition. The binding then checks the real definition and the four classes that open LocalDB
/// connections; a new class that opens LocalDB must be added to the list (stated limit: the list is not discovered
/// from source).
/// </remarks>
public sealed class SqlServerLocalDbCollectionTests
{
    private const string RequiredCollection = "SQL Server LocalDB";

    private const string ScriptedSerializedName = "Scripted serialized collection";

    private const string ScriptedParallelName = "Scripted parallel collection";

    private const string ScriptedExplicitParallelName = "Scripted explicit parallel collection";

    /// <summary>Mock gate positive control: a scripted type in the required collection passes the assertion.</summary>
    [Fact]
    public void ScriptedMember_PassesTheMembershipAssertion() =>
        AssertInCollection(typeof(ScriptedMember), RequiredCollection);

    /// <summary>
    /// Mock gate positive control: a scripted definition with parallelization disabled passes the definition
    /// assertion under its own name.
    /// </summary>
    [Fact]
    public void ScriptedSerializedDefinition_PassesTheDefinitionAssertion() =>
        AssertSerializedDefinition(typeof(ScriptedSerializedDefinition), ScriptedSerializedName);

    /// <summary>
    /// Mock gate negative controls: a definition without the flag (the shape of 0210244d), a definition that sets the
    /// flag to false, a serialized definition asserted under another name, and a type with no definition are each
    /// rejected by the definition assertion, inside a passing harness.
    /// </summary>
    [Fact]
    public void DefinitionNegativeControls_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertSerializedDefinition(typeof(ScriptedParallelDefinition), ScriptedParallelName)));
        Assert.NotNull(Record.Exception(() => AssertSerializedDefinition(typeof(ScriptedExplicitParallelDefinition), ScriptedExplicitParallelName)));
        Assert.NotNull(Record.Exception(() => AssertSerializedDefinition(typeof(ScriptedSerializedDefinition), RequiredCollection)));
        Assert.NotNull(Record.Exception(() => AssertSerializedDefinition(typeof(ScriptedOutsider), RequiredCollection)));
    }

    /// <summary>
    /// Mock gate negative controls: a scripted type with no collection and one in another collection are each
    /// rejected by the membership assertion, inside a passing harness.
    /// </summary>
    [Fact]
    public void MembershipNegativeControls_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertInCollection(typeof(ScriptedOutsider), RequiredCollection)));
        Assert.NotNull(Record.Exception(() => AssertInCollection(typeof(ScriptedOtherMember), RequiredCollection)));
    }

    /// <summary>
    /// Binding: the collection is defined with the required name and parallelization disabled (the same definition
    /// assertion the scripted definitions passed and failed), and each of the four classes that open LocalDB
    /// connections passes the same membership assertion.
    /// </summary>
    [Fact]
    public void LocalDbClasses_AreInTheSqlServerLocalDbCollection()
    {
        Assert.Equal(RequiredCollection, SqlServerLocalDbCollection.Name);
        AssertSerializedDefinition(typeof(SqlServerLocalDbCollection), RequiredCollection);

        foreach (var type in new[]
                 {
                     typeof(SqlServerAuditPayloadMigrationTests),
                     typeof(SqlServerDecompose4nfBackfillMigrationTests),
                     typeof(SqlServerRenameQuadBrainRolesMigrationTests),
                     typeof(SessionLogAgentSessionHeaderMigrationTests),
                 })
        {
            AssertInCollection(type, RequiredCollection);
        }
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

    /// <summary>
    /// Scripted definition with parallelization disabled; no class is a member. Public because xUnit requires a
    /// collection definition class to be public (xUnit1027).
    /// </summary>
    [CollectionDefinition(ScriptedSerializedName, DisableParallelization = true)]
    public sealed class ScriptedSerializedDefinition
    {
    }

    /// <summary>Scripted definition without the flag, the shape of 0210244d; no class is a member.</summary>
    [CollectionDefinition(ScriptedParallelName)]
    public sealed class ScriptedParallelDefinition
    {
    }

    /// <summary>Scripted definition that sets the flag to false; no class is a member.</summary>
    [CollectionDefinition(ScriptedExplicitParallelName, DisableParallelization = false)]
    public sealed class ScriptedExplicitParallelDefinition
    {
    }
}
