using System;
using System.Text.Json.Serialization;

namespace McpServer.Client;

/// <summary>Configuration options for the MCP Server client.</summary>
public sealed class McpServerClientOptions
{
    /// <summary>Base URL of the MCP Server (e.g. http://localhost:7148).</summary>
    public Uri BaseUrl { get; set; } = new Uri("http://localhost:7148");

    /// <summary>
    /// API key for workspace authentication. Sent via <c>X-Api-Key</c> header on every request
    /// (except <c>/health</c> which the server allows unauthenticated). Required.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>HTTP request timeout (default: 30 seconds).</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
