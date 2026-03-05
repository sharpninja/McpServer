using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Tunnel provider info returned by the tunnel endpoints.</summary>
public sealed class TunnelProviderInfo
{
    /// <summary>Provider name (e.g. <c>ngrok</c>, <c>cloudflare</c>, <c>frp</c>).</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "";

    /// <summary>Whether the provider is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Whether the tunnel process is running.</summary>
    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    /// <summary>Public URL assigned by the tunnel provider.</summary>
    [JsonPropertyName("publicUrl")]
    public string? PublicUrl { get; set; }

    /// <summary>Error message, if any.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
