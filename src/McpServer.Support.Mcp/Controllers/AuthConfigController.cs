using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Exposes public OIDC configuration so CLI clients (Director) can auto-discover
/// Keycloak settings without prior knowledge of the auth infrastructure.
/// This endpoint is intentionally unauthenticated — it only returns public metadata.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthConfigController : ControllerBase
{
    /// <summary>
    /// Returns the public OIDC configuration for CLI clients.
    /// No secrets are exposed — only the authority URL, public client ID, and endpoint URLs.
    /// </summary>
    /// <param name="options">Bound from <c>Mcp:Auth</c> configuration section.</param>
    /// <returns>Public auth configuration or a disabled indicator.</returns>
    [HttpGet("config")]
    [ProducesResponseType(typeof(AuthConfigResponse), 200)]
    public IActionResult GetConfig([FromServices] IOptions<OidcAuthOptions> options)
    {
        var auth = options.Value;

        if (!auth.Enabled)
        {
            return Ok(new AuthConfigResponse
            {
                Enabled = false,
                Authority = "",
                ClientId = "",
                Scopes = "",
                DeviceAuthorizationEndpoint = "",
                TokenEndpoint = ""
            });
        }

        var authority = auth.Authority.TrimEnd('/');

        return Ok(new AuthConfigResponse
        {
            Enabled = true,
            Authority = authority,
            ClientId = auth.DirectorClientId,
            Scopes = "openid profile email",
            DeviceAuthorizationEndpoint = $"{authority}/protocol/openid-connect/auth/device",
            TokenEndpoint = $"{authority}/protocol/openid-connect/token"
        });
    }
}

/// <summary>
/// Public OIDC configuration response for CLI clients.
/// Contains only public metadata — no secrets.
/// </summary>
public sealed class AuthConfigResponse
{
    /// <summary>Whether OIDC authentication is enabled on this server.</summary>
    public bool Enabled { get; set; }

    /// <summary>Keycloak realm authority URL.</summary>
    public string Authority { get; set; } = "";

    /// <summary>Public client ID for the Director CLI (Device Authorization Flow).</summary>
    public string ClientId { get; set; } = "";

    /// <summary>OAuth scopes to request.</summary>
    public string Scopes { get; set; } = "";

    /// <summary>OAuth 2.0 Device Authorization endpoint.</summary>
    public string DeviceAuthorizationEndpoint { get; set; } = "";

    /// <summary>OAuth 2.0 Token endpoint.</summary>
    public string TokenEndpoint { get; set; } = "";
}
