using System.Reflection;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// Tests that the test classes which open connections to the shared LocalDB instance belong to one xUnit collection,
/// so they run one after another instead of creating and migrating databases on the same instance at once.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-NUKE-019 AC-2 (every integration case runs and passes in its host's lane at
/// 0 failed). Found by the Windows interactive lane ug-r3-win-lane-4, where a migration command of
/// SqlServerAuditPayloadMigrationTests waited past its 120 s command timeout while several SQL Server test classes
/// migrated fresh databases on one LocalDB instance; fix chosen by the operator on 2026-10-20 ("Serialize SQL Server
/// tests (Recommended)"). Test data: scripted nested types carrying a collection attribute with the required name,
/// another name, or none; the attribute is read through <see cref="CustomAttributeData"/>, so no test is run and no
/// server is contacted. Negative controls prove the membership assertion rejects a type outside any collection and a
/// type in another collection. The binding then checks the four classes that open LocalDB connections; a new class
/// that opens LocalDB must be added to the list (stated limit: the list is not discovered from source).
/// </remarks>
public sealed class SqlServerLocalDbCollectionTests
{
    private const string RequiredCollection = "SQL Server LocalDB";

    /// <summary>
    /// Asserts that <paramref name="type"/> carries exactly one xUnit collection attribute and that it names
    /// <paramref name="collection"/>.
    /// </summary>
    private static void AssertInCollection(Type type, string collection)
    {
        var names = type.GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType == typeof(CollectionAttribute))
            .Select(attribute => attribute.ConstructorArguments.Count == 1 ? attribute.ConstructorArguments[0].Value as string : null)
            .ToList();
        var name = Assert.Single(names);
        Assert.Equal(collection, name);
    }

    /// <summary>Mock gate positive control: a scripted type in the required collection passes the assertion.</summary>
    [Fact]
    public void ScriptedMember_PassesTheMembershipAssertion() =>
        AssertInCollection(typeof(ScriptedMember), RequiredCollection);

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
    /// Binding: the collection is defined with the required name, and each of the four classes that open LocalDB
    /// connections passes the same membership assertion.
    /// </summary>
    [Fact]
    public void LocalDbClasses_AreInTheSqlServerLocalDbCollection()
    {
        Assert.Equal(RequiredCollection, SqlServerLocalDbCollection.Name);
        Assert.Contains(
            typeof(SqlServerLocalDbCollection).GetCustomAttributesData(),
            attribute => attribute.AttributeType == typeof(CollectionDefinitionAttribute)
                && attribute.ConstructorArguments.Count == 1
                && Equals(attribute.ConstructorArguments[0].Value, RequiredCollection));

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
}
