using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Public OIDC configuration metadata returned by <c>/auth/config</c>.</summary>
public sealed class AuthConfigResponse
{
    /// <summary>Whether OIDC authentication is enabled on the server.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>OIDC authority URL.</summary>
    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    /// <summary>Public Director client ID.</summary>
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    /// <summary>OAuth scopes string.</summary>
    [JsonPropertyName("scopes")]
    public string? Scopes { get; set; }

    /// <summary>Device authorization endpoint URL.</summary>
    [JsonPropertyName("deviceAuthorizationEndpoint")]
    public string? DeviceAuthorizationEndpoint { get; set; }

    /// <summary>Token endpoint URL.</summary>
    [JsonPropertyName("tokenEndpoint")]
    public string? TokenEndpoint { get; set; }
}

/// <summary>OAuth device authorization request sent to <c>/auth/device</c>.</summary>
public sealed class AuthDeviceAuthorizationRequest
{
    /// <summary>Public OAuth client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Requested OAuth scopes.</summary>
    public string? Scope { get; set; }

    /// <summary>Additional provider-specific form fields.</summary>
    public IDictionary<string, string?> AdditionalParameters { get; set; } = new Dictionary<string, string?>();
}

/// <summary>OAuth device authorization response returned from <c>/auth/device</c>.</summary>
public sealed class AuthDeviceAuthorizationResponse
{
    /// <summary>Opaque device code used to poll the token endpoint.</summary>
    [JsonPropertyName("device_code")]
    public string? DeviceCode { get; set; }

    /// <summary>User-facing verification code.</summary>
    [JsonPropertyName("user_code")]
    public string? UserCode { get; set; }

    /// <summary>Verification URI where the user enters the code.</summary>
    [JsonPropertyName("verification_uri")]
    public string? VerificationUri { get; set; }

    /// <summary>Verification URI with the code embedded when supplied by the provider.</summary>
    [JsonPropertyName("verification_uri_complete")]
    public string? VerificationUriComplete { get; set; }

    /// <summary>Device-code lifetime in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    /// <summary>Recommended polling interval in seconds.</summary>
    [JsonPropertyName("interval")]
    public int? Interval { get; set; }

    /// <summary>OAuth error code when the provider rejects the request.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>OAuth error description when the provider rejects the request.</summary>
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    /// <summary>Provider-specific response fields not modeled by the client.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>OAuth token request sent to <c>/auth/token</c>.</summary>
public sealed class AuthTokenRequest
{
    /// <summary>OAuth grant type.</summary>
    public string GrantType { get; set; } = string.Empty;

    /// <summary>Public OAuth client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Device code for device-flow polling.</summary>
    public string? DeviceCode { get; set; }

    /// <summary>Authorization code for authorization-code exchanges.</summary>
    public string? Code { get; set; }

    /// <summary>Redirect URI for authorization-code exchanges.</summary>
    public string? RedirectUri { get; set; }

    /// <summary>PKCE verifier for authorization-code exchanges.</summary>
    public string? CodeVerifier { get; set; }

    /// <summary>Refresh token for refresh-token exchanges.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Requested OAuth scopes.</summary>
    public string? Scope { get; set; }

    /// <summary>Additional provider-specific form fields.</summary>
    public IDictionary<string, string?> AdditionalParameters { get; set; } = new Dictionary<string, string?>();
}

/// <summary>OAuth token response returned from <c>/auth/token</c>.</summary>
public sealed class AuthTokenResponse
{
    /// <summary>Access token issued by the provider.</summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>Token type, usually <c>Bearer</c>.</summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    /// <summary>Access-token lifetime in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    /// <summary>Refresh-token lifetime in seconds when provided.</summary>
    [JsonPropertyName("refresh_expires_in")]
    public int? RefreshExpiresIn { get; set; }

    /// <summary>Refresh token issued by the provider.</summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>Granted OAuth scopes.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>OpenID Connect ID token when provided.</summary>
    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    /// <summary>OAuth error code when the provider rejects the request.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>OAuth error description when the provider rejects the request.</summary>
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    /// <summary>Provider-specific response fields not modeled by the client.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
