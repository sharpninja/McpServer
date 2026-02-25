namespace McpServer.Director.Auth;

/// <summary>
/// Configuration options for Director CLI Keycloak OIDC authentication.
/// The Director uses the OAuth 2.0 Device Authorization Grant flow.
/// </summary>
internal sealed class DirectorAuthOptions
{
    /// <summary>Keycloak realm authority URL (e.g. <c>http://localhost:8080/realms/mcpserver</c>).</summary>
    public string Authority { get; set; } = "";

    /// <summary>Keycloak public client ID for the Director CLI.</summary>
    public string ClientId { get; set; } = "mcp-director";

    /// <summary>OAuth scopes to request.</summary>
    public string Scopes { get; set; } = "openid profile email";

    /// <summary>Polling interval in seconds for device authorization flow.</summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>Timeout in seconds for device authorization flow.</summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>Whether auth is configured (authority is set).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Authority);

    /// <summary>Device authorization endpoint. Populated from server or derived from authority.</summary>
    public string DeviceAuthorizationEndpoint { get; set; } = "";

    /// <summary>Token endpoint. Populated from server or derived from authority.</summary>
    public string TokenEndpoint { get; set; } = "";

    /// <summary>
    /// Returns the device authorization endpoint, falling back to a derived URL if not explicitly set.
    /// </summary>
    public string GetDeviceAuthorizationEndpoint() =>
        !string.IsNullOrWhiteSpace(DeviceAuthorizationEndpoint)
            ? DeviceAuthorizationEndpoint
            : $"{Authority.TrimEnd('/')}/protocol/openid-connect/auth/device";

    /// <summary>
    /// Returns the token endpoint, falling back to a derived URL if not explicitly set.
    /// </summary>
    public string GetTokenEndpoint() =>
        !string.IsNullOrWhiteSpace(TokenEndpoint)
            ? TokenEndpoint
            : $"{Authority.TrimEnd('/')}/protocol/openid-connect/token";

    /// <summary>
    /// Populates this options instance from an <see cref="AuthConfigResponse"/> received from the MCP server.
    /// Only overwrites values that are not already set.
    /// </summary>
    public void PopulateFrom(AuthConfigResponse config)
    {
        if (!string.IsNullOrWhiteSpace(config.Authority) && string.IsNullOrWhiteSpace(Authority))
            Authority = config.Authority;
        if (!string.IsNullOrWhiteSpace(config.ClientId) && ClientId == "mcp-director")
            ClientId = config.ClientId;
        if (!string.IsNullOrWhiteSpace(config.Scopes) && Scopes == "openid profile email")
            Scopes = config.Scopes;
        if (!string.IsNullOrWhiteSpace(config.DeviceAuthorizationEndpoint))
            DeviceAuthorizationEndpoint = config.DeviceAuthorizationEndpoint;
        if (!string.IsNullOrWhiteSpace(config.TokenEndpoint))
            TokenEndpoint = config.TokenEndpoint;
    }
}
