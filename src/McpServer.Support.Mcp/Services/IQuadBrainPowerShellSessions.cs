namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-QBEXEC-002: In-process PowerShell session map used by QuadBrain mcp_powershell_* tools.</summary>
public interface IQuadBrainPowerShellSessions
{
    /// <summary>Creates a session rooted at <paramref name="workingDirectory"/> and returns its id.</summary>
    string Create(string workingDirectory);

    /// <summary>Runs a command in a previously created session.</summary>
    Task<ProcessRunResult> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default);

    /// <summary>Closes a session. Returns false when the id was unknown.</summary>
    bool Close(string sessionId);
}
