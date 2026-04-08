using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// OIDC provider strategy backed by the embedded Duende IdentityServer.
/// The authority is the MCP server itself.
/// </summary>
internal sealed class EmbeddedIdentityServerStrategy : IOidcProviderStrategy
{
    private readonly IdentityServerOptions _ids;

    public EmbeddedIdentityServerStrategy(IdentityServerOptions ids, int listenPort)
    {
        _ids = ids;
        Authority = !string.IsNullOrWhiteSpace(ids.IssuerUri)
            ? ids.IssuerUri
            : $"http://localhost:{listenPort}";
    }

    public bool IsEnabled => true;
    public string Authority { get; }
    public string GetPublicAuthority(string requestBaseUrl) => requestBaseUrl;
    public string Audience => _ids.ApiResourceName;
    public bool RequireHttpsMetadata => false;
    public string ClientId => "mcp-director";
    public string Scopes => $"openid profile email roles {_ids.ApiScopeName}";

    public string GetDeviceAuthorizationEndpoint(string requestBaseUrl)
        => $"{requestBaseUrl}/connect/deviceauthorization";

    public string GetTokenEndpoint(string requestBaseUrl)
        => $"{requestBaseUrl}/connect/token";

    public string GetPairingLoginUrl(string baseUrl)
        => $"{baseUrl}/pair";

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
