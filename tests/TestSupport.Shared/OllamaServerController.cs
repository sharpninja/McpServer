namespace McpServer.TestSupport.Ollama;

/// <summary>
/// TR-MCP-QBOLLAMA-002: Handle to a launched Ollama server process, abstracted so controller behavior
/// can be exercised without starting a real process.
/// </summary>
public interface IOllamaProcessHandle : IDisposable
{
    /// <summary>Gets a value indicating whether the launched process has exited.</summary>
    bool HasExited { get; }

    /// <summary>Terminates the launched process.</summary>
    void Kill();
}

/// <summary>
/// TR-MCP-QBOLLAMA-002: Outcome of an <see cref="OllamaServerController.EnsureRunningAsync"/> call.
/// </summary>
/// <param name="WasAlreadyRunning">True when a server answered the probe before any launch attempt.</param>
/// <param name="StartedByController">True when this controller launched the server and therefore owns it.</param>
/// <param name="ExecutablePath">Executable used to launch the server, or null when none was launched.</param>
public sealed record OllamaStartupResult(bool WasAlreadyRunning, bool StartedByController, string? ExecutablePath);

/// <summary>
/// TR-MCP-QBOLLAMA-002: Probes for a running Ollama server, launches one when absent, and stops only a
/// server it launched itself. Every external dependency (probe, executable resolution, process launch,
/// delay, and clock) is injected so the policy is unit-testable without a real Ollama installation.
/// Implements FR-MCP-QBOLLAMA-002.
/// </summary>
public sealed class OllamaServerController
{
    private readonly Func<CancellationToken, Task<bool>> _probeAsync;
    private readonly Func<string?> _resolveExecutable;
    private readonly Func<string, IOllamaProcessHandle> _startProcess;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _startupTimeout;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly Func<DateTimeOffset> _utcNow;

    private IOllamaProcessHandle? _ownedProcess;

    /// <summary>Initializes a controller over injected probe, resolution, launch, delay, and clock behavior.</summary>
    /// <param name="probeAsync">Returns true when the Ollama endpoint answers.</param>
    /// <param name="resolveExecutable">Returns the ollama executable path, or null when none is discoverable.</param>
    /// <param name="startProcess">Launches the server for the resolved executable path.</param>
    /// <param name="pollInterval">Delay between readiness probes after a launch.</param>
    /// <param name="startupTimeout">Maximum time to wait for a launched server to answer.</param>
    /// <param name="delayAsync">Delay implementation, injected so tests need not consume wall-clock time.</param>
    /// <param name="utcNow">Clock used to evaluate the startup deadline.</param>
    public OllamaServerController(
        Func<CancellationToken, Task<bool>> probeAsync,
        Func<string?> resolveExecutable,
        Func<string, IOllamaProcessHandle> startProcess,
        TimeSpan pollInterval,
        TimeSpan startupTimeout,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _probeAsync = probeAsync ?? throw new ArgumentNullException(nameof(probeAsync));
        _resolveExecutable = resolveExecutable ?? throw new ArgumentNullException(nameof(resolveExecutable));
        _startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
        _pollInterval = pollInterval;
        _startupTimeout = startupTimeout;
        _delayAsync = delayAsync ?? Task.Delay;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>Gets the outcome of the last <see cref="EnsureRunningAsync"/> call, or null before the first call.</summary>
    public OllamaStartupResult? LastResult { get; private set; }

    /// <summary>
    /// Adopts a running Ollama server when one answers the probe, otherwise launches one and waits for it
    /// to answer. A launched process that never answers is terminated before the failure surfaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result describing whether a server was adopted or launched.</returns>
    /// <exception cref="InvalidOperationException">
    /// No ollama executable is discoverable, or a launched server did not answer within the startup timeout.
    /// </exception>
    public async Task<OllamaStartupResult> EnsureRunningAsync(CancellationToken cancellationToken = default)
    {
        if (await _probeAsync(cancellationToken).ConfigureAwait(false))
        {
            LastResult = new OllamaStartupResult(WasAlreadyRunning: true, StartedByController: false, ExecutablePath: null);
            return LastResult;
        }

        var executablePath = _resolveExecutable();
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException(
                "No Ollama server is running on localhost:11434 and no ollama executable could be discovered. " +
                "Run the 'InstallOllama' Nuke target to provision the portable binaries and the required model, " +
                "or start Ollama manually before running TEST-MCP-QBOLLAMA-001.");
        }

        _ownedProcess = _startProcess(executablePath);
        var deadline = _utcNow().Add(_startupTimeout);

        while (_utcNow() < deadline)
        {
            await _delayAsync(_pollInterval, cancellationToken).ConfigureAwait(false);
            if (await _probeAsync(cancellationToken).ConfigureAwait(false))
            {
                LastResult = new OllamaStartupResult(WasAlreadyRunning: false, StartedByController: true, ExecutablePath: executablePath);
                return LastResult;
            }
        }

        // Never leave a process this controller started running after a failed startup.
        await StopAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException(
            $"Ollama server launched from '{executablePath}' did not answer on localhost:11434 within {_startupTimeout}.");
    }

    /// <summary>
    /// Stops the server only when this controller launched it. Adopted servers are left running.
    /// Repeated calls are safe and terminate the owned process at most once.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task once any owned process has been terminated and released.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var owned = _ownedProcess;
        _ownedProcess = null;
        if (owned is null)
            return Task.CompletedTask;

        if (!owned.HasExited)
            owned.Kill();
        owned.Dispose();
        return Task.CompletedTask;
    }
}
