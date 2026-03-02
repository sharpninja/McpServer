namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Captures server instance startup timestamp and listen port for unauthenticated diagnostics
/// and marker-file generation.
/// </summary>
public sealed class ServerRuntimeInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerRuntimeInfo"/> class.
    /// </summary>
    /// <param name="startedAtUtc">Server startup time in UTC (or convertible to UTC).</param>
    /// <param name="listenPort">The actual port the server is listening on.</param>
    public ServerRuntimeInfo(DateTimeOffset startedAtUtc, int listenPort)
    {
        StartedAtUtc = startedAtUtc.ToUniversalTime();
        ListenPort = listenPort;
    }

    /// <summary>The server startup timestamp in UTC.</summary>
    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>The actual TCP port the server is listening on.</summary>
    public int ListenPort { get; }
}
