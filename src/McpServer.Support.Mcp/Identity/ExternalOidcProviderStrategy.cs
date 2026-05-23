using McpServer.Support.Mcp.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// OIDC provider strategy for an external authority (e.g. Keycloak).
/// Token and device endpoints are proxied through the MCP server.
/// </summary>
internal sealed class ExternalOidcProviderStrategy : IOidcProviderStrategy
{
    private readonly OidcAuthOptions _auth;

    public ExternalOidcProviderStrategy(OidcAuthOptions auth)
    {
        _auth = auth;
    }

    public bool IsEnabled => true;
    public string Authority => _auth.Authority;
    public string GetPublicAuthority(string requestBaseUrl) => _auth.Authority.TrimEnd('/');
    public string Audience => _auth.Audience;
    public bool RequireHttpsMetadata => _auth.RequireHttpsMetadata;
    public string ClientId => _auth.DirectorClientId;
    public string Scopes => "openid profile email";

    public string GetDeviceAuthorizationEndpoint(string requestBaseUrl)
        => $"{requestBaseUrl}/auth/device";

    public string GetTokenEndpoint(string requestBaseUrl)
        => $"{requestBaseUrl}/auth/token";

    public string GetPairingLoginUrl(string baseUrl)
    {
        var authority = _auth.Authority.TrimEnd('/');
        if (Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
            return $"{baseUrl}/auth/ui{authorityUri.AbsolutePath.TrimEnd('/')}/device";
        return $"{baseUrl}/pair";
    }

    public void ConfigureAuthentication(AuthenticationBuilder builder)
    {
        builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.MapInboundClaims = false;
            options.Authority = Authority;
            options.Audience = Audience;
            options.RequireHttpsMetadata = RequireHttpsMetadata;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "preferred_username",
                RoleClaimType = "realm_roles",
                ValidateAudience = !string.IsNullOrWhiteSpace(Audience),
            };
        });
    }
}
