namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Captures a server instance startup timestamp for unauthenticated diagnostics
/// and marker-file stale detection.
/// </summary>
public sealed class ServerRuntimeInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerRuntimeInfo"/> class.
    /// </summary>
    /// <param name="startedAtUtc">Server startup time in UTC (or convertible to UTC).</param>
    public ServerRuntimeInfo(DateTimeOffset startedAtUtc)
    {
        StartedAtUtc = startedAtUtc.ToUniversalTime();
    }

    /// <summary>The server startup timestamp in UTC.</summary>
    public DateTimeOffset StartedAtUtc { get; }
}
