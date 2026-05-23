using Microsoft.AspNetCore.Authentication;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// OIDC provider strategy when no identity provider is configured.
/// Authentication is not registered; all auth policies pass unconditionally.
/// </summary>
internal sealed class DisabledOidcProviderStrategy : IOidcProviderStrategy
{
    public bool IsEnabled => false;
    public string Authority => "";
    public string GetPublicAuthority(string requestBaseUrl) => "";
    public string Audience => "";
    public bool RequireHttpsMetadata => false;
    public string ClientId => "";
    public string Scopes => "";

    public string GetDeviceAuthorizationEndpoint(string requestBaseUrl) => "";
    public string GetTokenEndpoint(string requestBaseUrl) => "";
    public string GetPairingLoginUrl(string baseUrl) => $"{baseUrl}/pair";

    public void ConfigureAuthentication(AuthenticationBuilder builder)
    {
        // No-op: no JWT scheme is registered.
    }
}
