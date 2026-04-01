namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Configuration options for the embedded IdentityServer instance.
/// Bound from <c>Mcp:IdentityServer</c> configuration section.
/// </summary>
public sealed class IdentityServerOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Mcp:IdentityServer";

    /// <summary>Whether the embedded IdentityServer is enabled. Default: false.</summary>
    public bool Enabled { get; set; }

    /// <summary>The issuer URI for tokens. Defaults to the server's base URL.</summary>
    public string IssuerUri { get; set; } = "";

    /// <summary>SQLite database file for identity data. Relative to DataFolder.</summary>
    public string DatabaseFile { get; set; } = "identity.db";

    /// <summary>Whether to seed default clients and resources on startup.</summary>
    public bool SeedDefaults { get; set; } = true;

    /// <summary>Default admin username seeded on first run.</summary>
    public string DefaultAdminUser { get; set; } = "admin";

    /// <summary>Default admin password seeded on first run. Change after initial setup.</summary>
    public string DefaultAdminPassword { get; set; } = "McpAdmin1!";

    /// <summary>API scope name for the MCP Server API.</summary>
    public string ApiScopeName { get; set; } = "mcp-api";

    /// <summary>API resource name.</summary>
    public string ApiResourceName { get; set; } = "mcp-server-api";
}
