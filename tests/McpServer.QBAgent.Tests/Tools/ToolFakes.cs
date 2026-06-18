using McpServer.McpAgent.PowerShellSessions;
using McpServer.Support.Mcp.Services;

namespace McpServer.QBAgent.Tests.Tools;

/// <summary>
/// Test double for <see cref="IProcessRunner"/> that records the last request and returns a configured result.
/// Used to assert how the git and bash tools build process invocations (TEST-MCP-QBTOOLS-002/003).
/// </summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly ProcessRunResult _result;

    /// <summary>Initializes the fake with the result every invocation returns.</summary>
    /// <param name="result">The canned result.</param>
    public FakeProcessRunner(ProcessRunResult result) => _result = result;

    /// <summary>Gets the last request passed to <see cref="RunAsync(ProcessRunRequest, CancellationToken)"/>.</summary>
    public ProcessRunRequest? LastRequest { get; private set; }

    /// <summary>Gets the number of times the runner was invoked.</summary>
    public int InvocationCount { get; private set; }

    /// <inheritdoc />
    public Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct = default)
        => RunAsync(new ProcessRunRequest(fileName, arguments), ct);

    /// <inheritdoc />
    public Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken ct = default)
    {
        LastRequest = request;
        InvocationCount++;
        return Task.FromResult(_result);
    }
}

/// <summary>
/// Test double for <see cref="IHostedPowerShellSessionManager"/> that returns canned create/command results and
/// records the executed command. Used by run_powershell tests (TEST-MCP-QBTOOLS-007).
/// </summary>
internal sealed class FakePowerShellSessionManager : IHostedPowerShellSessionManager
{
    private readonly PowerShellSessionCommandResult _commandResult;
    private readonly bool _createSucceeds;

    /// <summary>Initializes the fake with the command result every execution returns.</summary>
    /// <param name="commandResult">The canned command result.</param>
    /// <param name="createSucceeds">Whether <see cref="CreateSession"/> reports success.</param>
    public FakePowerShellSessionManager(PowerShellSessionCommandResult commandResult, bool createSucceeds = true)
    {
        _commandResult = commandResult;
        _createSucceeds = createSucceeds;
    }

    /// <summary>Gets the last command executed.</summary>
    public string? LastCommand { get; private set; }

    /// <summary>Gets the number of sessions created.</summary>
    public int CreatedSessions { get; private set; }

    /// <summary>Gets the number of sessions closed.</summary>
    public int ClosedSessions { get; private set; }

    /// <inheritdoc />
    public PowerShellSessionCreateResult CreateSession(string workspacePath, string? workingDirectory = null)
    {
        CreatedSessions++;
        return _createSucceeds
            ? new PowerShellSessionCreateResult { Success = true, SessionId = "ps-test", CurrentLocation = workspacePath }
            : new PowerShellSessionCreateResult { Success = false, ErrorMessage = "session creation failed" };
    }

    /// <inheritdoc />
    public Task<PowerShellSessionCommandResult> ExecuteCommandAsync(string sessionId, string command, CancellationToken cancellationToken = default)
    {
        LastCommand = command;
        return Task.FromResult(_commandResult);
    }

    /// <inheritdoc />
    public Task<PowerShellSessionCommandResult> ExecuteInteractiveCommandAsync(
        string sessionId, string command, Func<CancellationToken, string?> readLine,
        TextWriter outputWriter, TextWriter errorWriter, CancellationToken cancellationToken = default)
        => Task.FromResult(_commandResult);

    /// <inheritdoc />
    public PowerShellSessionCloseResult CloseSession(string sessionId)
    {
        ClosedSessions++;
        return new PowerShellSessionCloseResult { Success = true, SessionId = sessionId };
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
