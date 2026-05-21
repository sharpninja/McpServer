namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103: Header names used by hub-and-spoke federation routing,
/// replay, acknowledgement, stale-read, and conflict workflows.
/// </summary>
public static class FederationHeaders
{
    /// <summary>Header identifying the local proxy currently forwarding the request.</summary>
    public const string ProxyId = "X-Mcp-Proxy-Id";

    /// <summary>Header identifying the hub-wide workspace identity for a routed request.</summary>
    public const string GlobalWorkspaceId = "X-Mcp-Global-Workspace-Id";

    /// <summary>Header identifying the current federation operation.</summary>
    public const string OperationId = "X-Mcp-Operation-Id";

    /// <summary>Header linking an echoed or derived operation to its source operation.</summary>
    public const string SourceOperationId = "X-Mcp-Source-Operation-Id";

    /// <summary>Header identifying whether the response was accepted into a local queue.</summary>
    public const string Queued = "X-Mcp-Queued";

    /// <summary>Header identifying whether the response is based on a stale local read.</summary>
    public const string StaleRead = "X-Mcp-Stale-Read";
}
