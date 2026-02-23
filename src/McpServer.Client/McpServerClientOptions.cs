using System;
using System.Text.Json.Serialization;

namespace McpServer.Client;

/// <summary>Configuration options for the MCP Server client.</summary>
public sealed class McpServerClientOptions
{
    /// <summary>Base URL of the MCP Server (e.g. http://localhost:7148).</summary>
    public Uri BaseUrl { get; set; } = new Uri("http://localhost:7148");

    /// <summary>Optional API key for protected endpoints. Sent via X-Api-Key header.</summary>
    public string? ApiKey { get; set; }

    /// <summary>HTTP request timeout (default: 30 seconds).</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
