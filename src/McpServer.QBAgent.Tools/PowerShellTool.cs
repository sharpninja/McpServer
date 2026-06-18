using McpServer.McpAgent.PowerShellSessions;

namespace McpServer.QBAgent.Tools;

/// <summary>
/// FR-MCP-QBTOOLS-002 / TR-MCP-QBTOOLS-002: Agent-side <c>run_powershell</c> tool. Reuses the in-process
/// <see cref="IHostedPowerShellSessionManager"/> runspace. A single session is created lazily and reused across
/// calls so working-directory and variable state persist within a QBAgent run; invocations are serialized.
/// </summary>
public sealed class PowerShellTool : IDisposable
{
    private readonly IHostedPowerShellSessionManager _sessions;
    private readonly string _workspacePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _sessionId;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="PowerShellTool"/> class.</summary>
    /// <param name="sessions">The hosted PowerShell session manager.</param>
    /// <param name="workspacePath">The workspace path used to root the session.</param>
    public PowerShellTool(IHostedPowerShellSessionManager sessions, string workspacePath)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
    }

    /// <summary>Runs a PowerShell command in the agent's reused hosted session.</summary>
    /// <param name="command">The PowerShell script text to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command result including captured output and error streams.</returns>
    public async Task<PowerShellSessionCommandResult> RunAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new PowerShellSessionCommandResult { Success = false, HadErrors = true, ErrorOutput = "A PowerShell command is required." };

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sessionId is null)
            {
                var created = _sessions.CreateSession(_workspacePath);
                if (!created.Success || string.IsNullOrWhiteSpace(created.SessionId))
                {
                    return new PowerShellSessionCommandResult
                    {
                        Success = false,
                        HadErrors = true,
                        ErrorOutput = created.ErrorMessage ?? "Failed to create a PowerShell session.",
                    };
                }

                _sessionId = created.SessionId;
            }

            return await _sessions.ExecuteCommandAsync(_sessionId, command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Closes the reused PowerShell session, if any, and releases the invocation gate.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_sessionId is not null)
        {
            try
            {
                _sessions.CloseSession(_sessionId);
            }
            catch (ObjectDisposedException)
            {
                // The session manager was already disposed with the hosting agent; nothing to release.
            }

            _sessionId = null;
        }

        _gate.Dispose();
    }
}

