using Microsoft.Data.SqlClient;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-NUKE-019 AC-2: the server connection of the SQL Server integration test classes. The built-in default is
/// LocalDB with a 120 s connect timeout (matching the classes' 120 s command timeout), so a LocalDB instance that is
/// still starting while several SQL Server test classes open it at once does not fail the Windows integration lane
/// (ug-r3-win-lane-2 failed after SqlClient's 15 s default). <c>MCP_TEST_SQLSERVER_CONNECTION</c> names another
/// server and is used unchanged.
/// </summary>
internal static class SqlServerTestConnection
{
    /// <summary>Environment variable that names another SQL Server; its value is used unchanged.</summary>
    internal const string OverrideVariable = "MCP_TEST_SQLSERVER_CONNECTION";

    /// <summary>Seconds a connection may wait for LocalDB to accept it.</summary>
    internal const int ConnectTimeoutSeconds = 120;

    /// <summary>The built-in LocalDB default: integrated security, a trusted certificate, no database.</summary>
    internal static string LocalDbDefault { get; } = new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB",
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        ConnectTimeout = ConnectTimeoutSeconds,
    }.ConnectionString;

    /// <summary>The server connection for this process: the override when it is set, otherwise the LocalDB default.</summary>
    internal static string Server => Resolve(Environment.GetEnvironmentVariable(OverrideVariable));

    /// <summary>Returns <paramref name="overrideValue"/> when it is set, otherwise <see cref="LocalDbDefault"/>.</summary>
    /// <param name="overrideValue">The value of <see cref="OverrideVariable"/>, or null when it is unset.</param>
    /// <returns>The server connection string.</returns>
    internal static string Resolve(string? overrideValue) => overrideValue ?? LocalDbDefault;
}
