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
