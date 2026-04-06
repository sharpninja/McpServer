using Duende.IdentityServer.Models;
using DuendeClient = Duende.IdentityServer.Models.Client;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Static IdentityServer resource and client definitions for the MCP Server.
/// </summary>
internal static class IdentityServerConfig
{
    public static IEnumerable<IdentityResource> GetIdentityResources() =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email(),
        new IdentityResource("roles", "User roles", ["role", "realm_roles"]),
    ];

    public static IEnumerable<ApiScope> GetApiScopes(IdentityServerOptions options) =>
    [
        new ApiScope(options.ApiScopeName, "MCP Server API")
        {
            UserClaims = ["role", "realm_roles", "preferred_username"],
        },
    ];

    public static IEnumerable<ApiResource> GetApiResources(IdentityServerOptions options) =>
    [
        new ApiResource(options.ApiResourceName, "MCP Server API")
        {
            Scopes = { options.ApiScopeName },
            UserClaims = ["role", "realm_roles", "preferred_username"],
        },
    ];

    public static IEnumerable<DuendeClient> GetClients(IdentityServerOptions options) =>
    [
        // Machine-to-machine client for agents and services
        new DuendeClient
        {
            ClientId = "mcp-agent",
            ClientName = "MCP Agent Client",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("mcp-agent-secret".Sha256()) },
            AllowedScopes = { options.ApiScopeName },
        },

        // Interactive client for CLI tools (Device Authorization + Password flows)
        new DuendeClient
        {
            ClientId = "mcp-director",
            ClientName = "MCP Director CLI",
            AllowedGrantTypes =
            {
                "urn:ietf:params:oauth:grant-type:device_code",
                GrantType.ResourceOwnerPassword,
            },
            RequireClientSecret = false,
            AllowedScopes = { "openid", "profile", "email", "roles", options.ApiScopeName },
            AllowOfflineAccess = true,
            AccessTokenLifetime = 3600,
            RefreshTokenUsage = TokenUsage.ReUse,
            RefreshTokenExpiration = TokenExpiration.Sliding,
            SlidingRefreshTokenLifetime = 86400,
        },

        // Web/SPA client for pairing UI and browser-based access
        new DuendeClient
        {
            ClientId = "mcp-web",
            ClientName = "MCP Web Client",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,
            RedirectUris =
            {
                "http://localhost:7147/auth/callback", "https://localhost:7147/auth/callback",
                "https://localhost:39983/signin-oidc", "http://localhost:39984/signin-oidc",
            },
            PostLogoutRedirectUris =
            {
                "http://localhost:7147/", "https://localhost:7147/",
                "https://localhost:39983/", "http://localhost:39984/",
            },
            AllowedCorsOrigins =
            {
                "http://localhost:7147", "https://localhost:7147",
                "https://localhost:39983", "http://localhost:39984",
            },
            AllowedScopes = { "openid", "profile", "email", "roles", options.ApiScopeName },
            AllowOfflineAccess = true,
        },
    ];
}
