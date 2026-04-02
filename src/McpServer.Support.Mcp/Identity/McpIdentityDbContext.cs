using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Identity DbContext for IdentityServer user management.
/// Uses a separate SQLite database from the main MCP data store.
/// </summary>
public sealed class McpIdentityDbContext : IdentityDbContext<McpUser>
{
    /// <summary>Initializes a new instance with the specified options.</summary>
    public McpIdentityDbContext(DbContextOptions<McpIdentityDbContext> options)
        : base(options) { }
}
