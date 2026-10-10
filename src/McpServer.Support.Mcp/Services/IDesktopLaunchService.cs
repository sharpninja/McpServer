using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-047: In-process desktop launch used by HTTP, STDIO, and QuadBrain internal tools.</summary>
public interface IDesktopLaunchService
{
    /// <summary>Launches a local desktop process for the specified workspace.</summary>
    Task<DesktopLaunchResult> LaunchAsync(
        string workspacePath,
        DesktopLaunchRequest request,
        CancellationToken cancellationToken = default);
}
