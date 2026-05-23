using Microsoft.AspNetCore.Authentication;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Strategy for configuring OIDC-based authentication.
/// Implementations encapsulate the details of a specific identity provider
/// (embedded IdentityServer, external Keycloak, or none) so that Program.cs
/// and AuthConfigController never need provider-specific branching.
/// </summary>
public interface IOidcProviderStrategy
{
    /// <summary>Whether authentication is enabled at all.</summary>
    bool IsEnabled { get; }

    /// <summary>The OIDC authority URL used for JWT validation and discovery.</summary>
    string Authority { get; }

    /// <summary>
    /// Returns the authority URL to advertise to external clients via <c>/auth/config</c>.
    /// For embedded providers this is the request-facing base URL (so tunnels/proxies work);
    /// for external providers this is the configured authority.
    /// </summary>
    string GetPublicAuthority(string requestBaseUrl);

    /// <summary>The expected JWT audience claim.</summary>
    string Audience { get; }

    /// <summary>Whether HTTPS is required for metadata retrieval.</summary>
    bool RequireHttpsMetadata { get; }

    /// <summary>Public client ID returned by <c>/auth/config</c> for CLI clients.</summary>
    string ClientId { get; }

    /// <summary>OAuth scopes returned by <c>/auth/config</c>.</summary>
    string Scopes { get; }

    /// <summary>
    /// Builds the device authorization endpoint URL for the given request base URL.
    /// </summary>
    string GetDeviceAuthorizationEndpoint(string requestBaseUrl);

    /// <summary>
    /// Builds the token endpoint URL for the given request base URL.
    /// </summary>
    string GetTokenEndpoint(string requestBaseUrl);

    /// <summary>
    /// Resolves the login URL for the QR pairing page.
    /// </summary>
    /// <param name="baseUrl">The server's publicly reachable base URL (tunnel or local).</param>
    string GetPairingLoginUrl(string baseUrl);

    /// <summary>
    /// Configures authentication services on the <see cref="AuthenticationBuilder"/>.
    /// Called during startup to register the appropriate JWT bearer or other schemes.
    /// </summary>
    void ConfigureAuthentication(AuthenticationBuilder builder);
}
