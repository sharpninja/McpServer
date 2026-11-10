using System.Reflection;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CollectionAssertions;
using static QBrainAi.Support.Mcp.Tests.Storage.SqlServerLocalDbCollectionTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

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
/// <para>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-7; TR-MCP-NUKE-019 AC-2; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the binding half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/SqlServerLocalDbCollectionTests.cs), where its mock gate cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests together with the classes it binds (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its one case now carries Category=Integration because this project holds only integration cases (see the entry on that case); the collection name both halves use comes from Storage/SqlServerLocalDbCollectionTestsSupport.cs of QBrainAi.Support.Mcp.Tests, and CollectionAssertions and SqlServerLocalDbCollection are linked into this assembly (DRY).</para>
/// </remarks>
public sealed class SqlServerLocalDbCollectionTests
{
    /// <summary>
    /// Binding: the collection is defined with the required name and parallelization disabled (the same definition
    /// assertion the scripted definitions passed and failed), and each of the four classes that open LocalDB
    /// connections passes the same membership assertion.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-7; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved from QBrainAi.Support.Mcp.Tests with the classes it binds (slice S8, hostile validation finding R14-F4). It names the four SQL Server test classes, which now live in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (SessionLogAgentSessionHeaderMigrationTests by its integration half, which holds its SQL Server case), and that project holds only integration cases, so this binding now carries Category=Integration (where it runs, not what it checks); its text and assertions are otherwise unchanged.</remarks>
    [Trait("Category", "Integration")]
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
}
