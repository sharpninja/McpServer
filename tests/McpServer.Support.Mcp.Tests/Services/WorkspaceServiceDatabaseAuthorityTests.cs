using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-138: Red contract tests proving DB-FK-001 requires
/// <see cref="WorkspaceService"/> to become database-authoritative and to treat
/// appsettings workspace entries as post-commit projection data.
/// </summary>
public sealed class WorkspaceServiceDatabaseAuthorityTests
{
    /// <summary>
    /// TEST-MCP-138: WorkspaceService must depend on the EF database context so
    /// list, get, create, update, and delete operations can read/write canonical
    /// workspace rows before appsettings projection happens.
    /// </summary>
    [Fact]
    public void DBFK_WorkspaceService_RequiresDatabaseContextDependency()
    {
        var hasDatabaseDependency = typeof(WorkspaceService)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => p.ParameterType == typeof(McpDbContext)
                || p.ParameterType.Name.Contains("DbContextFactory", StringComparison.Ordinal)
                || p.ParameterType.Name.Contains("WorkspaceRepository", StringComparison.Ordinal));

        Assert.True(
            hasDatabaseDependency,
            "WorkspaceService must receive a database-backed workspace dependency for DB-first workspace registry behavior.");
    }

    /// <summary>
    /// TEST-MCP-138: WorkspaceService must delegate appsettings writes to a
    /// projection writer so the projection can run after a successful database
    /// commit and can omit secret-bearing configuration values.
    /// </summary>
    [Fact]
    public void DBFK_WorkspaceService_UsesProjectionWriterDependency()
    {
        var hasProjectionWriter = typeof(WorkspaceService)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => p.ParameterType.Name.Contains("WorkspaceProjectionWriter", StringComparison.Ordinal));

        Assert.True(
            hasProjectionWriter,
            "WorkspaceService must use an appsettings projection writer instead of writing Mcp:Workspaces as its source of truth.");
    }
}
