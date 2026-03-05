namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Configuration options for Keycloak OIDC / JWT Bearer authentication.
/// Bound from <c>Mcp:Auth</c> configuration section.
/// </summary>
public sealed class OidcAuthOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Auth";

    /// <summary>Keycloak realm authority URL (e.g. <c>http://localhost:8080/realms/mcpserver</c>).</summary>
    public string Authority { get; set; } = "";

    /// <summary>Expected audience claim in JWT tokens (e.g. <c>mcp-server-api</c>).</summary>
    public string Audience { get; set; } = "mcp-server-api";

    /// <summary>Client secret for the API client (used for token introspection if needed).</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>Whether to require HTTPS for metadata retrieval. Set to <c>false</c> for local development.</summary>
    public bool RequireHttpsMetadata { get; set; } = false;

    /// <summary>Public client ID for the Director CLI (Device Authorization Flow). Default: <c>mcp-director</c>.</summary>
    public string DirectorClientId { get; set; } = "mcp-director";

    /// <summary>Whether OIDC auth is enabled. If <c>false</c>, JWT endpoints fall back to API key only.</summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(Authority);
}
