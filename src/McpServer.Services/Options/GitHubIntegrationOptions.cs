namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-MCP-GH-001: GitHub integration configuration bound from <c>Mcp:GitHub</c>.
/// </summary>
public sealed class GitHubIntegrationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:GitHub";

    /// <summary>
    /// Relative or absolute path to the workspace token store file.
    /// Relative paths are resolved against the active instance data directory.
    /// </summary>
    public string TokenStorePath { get; set; } = "mcp-data/github-token-store.json";

    /// <summary>
    /// When <see langword="true"/>, the server will attempt to use the stored
    /// per-workspace GitHub token before relying on ambient gh CLI authentication.
    /// </summary>
    public bool PreferStoredToken { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, ambient gh CLI auth is allowed as fallback
    /// when no stored token is available for the current workspace.
    /// </summary>
    public bool AllowCliFallback { get; set; } = true;

    /// <summary>OAuth app configuration used to bootstrap browser/device login flows.</summary>
    public GitHubOAuthOptions OAuth { get; set; } = new();
}

/// <summary>
/// TR-MCP-GH-001: GitHub OAuth app settings used to construct authorization URLs.
/// </summary>
public sealed class GitHubOAuthOptions
{
    /// <summary>GitHub OAuth app client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Registered redirect URI for the OAuth app.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>OAuth authorize endpoint. Default: GitHub authorize endpoint.</summary>
    public string AuthorizeEndpoint { get; set; } = "https://github.com/login/oauth/authorize";

    /// <summary>
    /// Space-separated OAuth scopes requested by the app.
    /// Default scopes support repository and workflow operations.
    /// </summary>
    public string Scopes { get; set; } = "repo workflow read:org";
}
