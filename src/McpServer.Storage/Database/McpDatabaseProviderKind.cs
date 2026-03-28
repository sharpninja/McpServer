namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Identifies the relational database provider selected for the MCP server runtime.
/// </summary>
public enum McpDatabaseProviderKind
{
    /// <summary>Uses SQLite as the backing relational database engine.</summary>
    Sqlite = 0,

    /// <summary>Uses PostgreSQL as the backing relational database engine.</summary>
    PostgreSql = 1,

    /// <summary>Uses SQL Server as the backing relational database engine.</summary>
    SqlServer = 2,
}
