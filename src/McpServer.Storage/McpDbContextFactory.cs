using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// Design-time factory for <see cref="McpDbContext"/>.
/// Used by EF Core tooling (dotnet-ef) to create a context instance at design time.
/// </summary>
public sealed class McpDbContextFactory : IDesignTimeDbContextFactory<McpDbContext>
{
    /// <inheritdoc />
    public McpDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<McpDbContext>();
        optionsBuilder.UseSqlite("Data Source=mcp_design_time.db");
        return new McpDbContext(optionsBuilder.Options);
    }
}
