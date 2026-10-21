using Microsoft.Data.SqlClient;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// Tests the server connection that the SQL Server integration test classes use by default: LocalDB with integrated
/// security, a trusted server certificate, no database, and a 120 s connect timeout, so a LocalDB instance that is
/// still starting when several SQL Server test classes open it at once does not fail the Windows integration lane.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-NUKE-019 AC-2 (every integration case runs and passes in its host's lane at
/// 0 failed). Found by the Windows interactive lane ug-r3-win-lane-2, where
/// SqlServerAuditPayloadMigrationTests failed after the 15 s default connect timeout; fix chosen by the operator on
/// 2026-10-20 ("Fix the flake in PR 87 (Recommended)"). Test data: scripted connection strings parsed with
/// <see cref="SqlConnectionStringBuilder"/>; no server is contacted and no environment variable is read. Negative
/// controls feed the default assertion the literal the three classes used before (15 s default), a short timeout,
/// another server, a string without the trusted certificate, and one that names a database, and prove it rejects each.
/// </remarks>
public sealed class SqlServerTestConnectionTests
{
    private const string RequiredDefault =
        "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=120";

    /// <summary>
    /// Asserts that <paramref name="connectionString"/> is the required default: LocalDB, integrated security, a
    /// trusted server certificate, no database, and a 120 s connect timeout.
    /// </summary>
    private static void AssertLocalDbDefault(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        Assert.Equal(@"(localdb)\MSSQLLocalDB", builder.DataSource);
        Assert.True(builder.IntegratedSecurity);
        Assert.True(builder.TrustServerCertificate);
        Assert.Equal(string.Empty, builder.InitialCatalog);
        Assert.Equal(120, builder.ConnectTimeout);
    }

    /// <summary>Mock gate positive control: the required default passes the assertion.</summary>
    [Fact]
    public void RequiredDefault_PassesTheDefaultAssertion() => AssertLocalDbDefault(RequiredDefault);

    /// <summary>
    /// Mock gate negative controls: the literal the three classes used before (SqlClient's 15 s default connect
    /// timeout), an 8 s timeout, another server, no trusted certificate, and a named database are each rejected.
    /// </summary>
    [Fact]
    public void DefaultNegativeControls_AreRejectedInsidePassingHarness()
    {
        foreach (var wrong in new[]
                 {
                     "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True",
                     "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=8",
                     "Server=localhost;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=120",
                     "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;Connect Timeout=120",
                     "Server=(localdb)\\MSSQLLocalDB;Database=mcp;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=120",
                 })
        {
            Assert.NotNull(Record.Exception(() => AssertLocalDbDefault(wrong)));
        }
    }

    /// <summary>
    /// Binding: <see cref="SqlServerTestConnection.LocalDbDefault"/>, and the resolver with no override, pass the
    /// same default assertion.
    /// </summary>
    [Fact]
    public void LocalDbDefault_PassesTheDefaultAssertion()
    {
        AssertLocalDbDefault(SqlServerTestConnection.LocalDbDefault);
        AssertLocalDbDefault(SqlServerTestConnection.Resolve(overrideValue: null));
    }

    /// <summary>Binding: an override is returned unchanged, with no timeout or flag added.</summary>
    [Fact]
    public void Resolve_Override_IsReturnedUnchanged()
    {
        const string external = "Server=sql.lab;Database=master;User Id=tester;Encrypt=False";
        Assert.Equal(external, SqlServerTestConnection.Resolve(external));
    }
}
