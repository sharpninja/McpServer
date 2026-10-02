using System.Collections.Concurrent;

namespace McpServer.Support.Mcp.Services;

/// <summary>Default in-process PowerShell session store. Commands run through <see cref="IProcessRunner"/>.</summary>
public sealed class InMemoryQuadBrainPowerShellSessions : IQuadBrainPowerShellSessions
{
    private readonly ConcurrentDictionary<string, string> _sessions = new(StringComparer.Ordinal);
    private readonly IProcessRunner _processRunner;

    /// <summary>Creates a session store that executes commands with <paramref name="processRunner"/>.</summary>
    public InMemoryQuadBrainPowerShellSessions(IProcessRunner processRunner)
        => _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));

    /// <inheritdoc />
    public string Create(string workingDirectory)
    {
        var id = Guid.NewGuid().ToString("N");
        _sessions[id] = workingDirectory;
        return id;
    }

    /// <inheritdoc />
    public async Task<ProcessRunResult> ExecuteAsync(string sessionId, string command, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var cwd))
            throw new InvalidOperationException($"PowerShell session '{sessionId}' was not found.");

        return await _processRunner.RunAsync(
            new ProcessRunRequest("pwsh", "-NoProfile -Command " + command, WorkingDirectory: cwd),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool Close(string sessionId)
        => _sessions.TryRemove(sessionId, out _);
}
