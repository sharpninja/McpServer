using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using Microsoft.Extensions.Logging;

namespace McpServer.McpAgent.PowerShellSessions;

internal sealed class HostedPowerShellSessionManager : IHostedPowerShellSessionManager
{
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, HostedPowerShellSession> _sessions =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _disposed;

    public HostedPowerShellSessionManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public PowerShellSessionCreateResult CreateSession(string workspacePath, string? workingDirectory = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var resolvedWorkingDirectory = ResolveWorkingDirectory(workspacePath, workingDirectory);
        if (resolvedWorkingDirectory is null)
        {
            return new PowerShellSessionCreateResult
            {
                Success = false,
                ErrorMessage = "A valid working directory is required to create a PowerShell session."
            };
        }

        var sessionId = $"ps-{Guid.NewGuid():N}";

        try
        {
            var session = new HostedPowerShellSession(sessionId, workspacePath, resolvedWorkingDirectory, _logger);
            if (!_sessions.TryAdd(sessionId, session))
            {
                session.Dispose();
                return new PowerShellSessionCreateResult
                {
                    Success = false,
                    ErrorMessage = $"PowerShell session '{sessionId}' already exists."
                };
            }

            return new PowerShellSessionCreateResult
            {
                Success = true,
                SessionId = sessionId,
                CurrentLocation = session.CurrentLocation,
                CreatedAtUtc = session.CreatedAtUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create hosted PowerShell session in {WorkingDirectory}", resolvedWorkingDirectory);
            return new PowerShellSessionCreateResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<PowerShellSessionCommandResult> ExecuteCommandAsync(
        string sessionId,
        string command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            sessionId,
            command,
            static (session, script, token) => session.ExecuteAsync(script, token),
            cancellationToken);

    public Task<PowerShellSessionCommandResult> ExecuteInteractiveCommandAsync(
        string sessionId,
        string command,
        Func<CancellationToken, string?> readLine,
        TextWriter outputWriter,
        TextWriter errorWriter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(readLine);
        ArgumentNullException.ThrowIfNull(outputWriter);
        ArgumentNullException.ThrowIfNull(errorWriter);

        return ExecuteAsync(
            sessionId,
            command,
            (session, script, token) => session.ExecuteInteractiveAsync(
                script,
                readLine,
                outputWriter,
                errorWriter,
                token),
            cancellationToken);
    }

    public PowerShellSessionCloseResult CloseSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new PowerShellSessionCloseResult
            {
                Success = false,
                ErrorMessage = "sessionId is required."
            };
        }

        if (!_sessions.TryRemove(sessionId, out var session))
        {
            return new PowerShellSessionCloseResult
            {
                Success = false,
                SessionId = sessionId,
                ErrorMessage = $"PowerShell session '{sessionId}' was not found."
            };
        }

        session.Dispose();
        return new PowerShellSessionCloseResult
        {
            Success = true,
            SessionId = sessionId
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var sessionId in _sessions.Keys.ToArray())
        {
            if (_sessions.TryRemove(sessionId, out var session))
                session.Dispose();
        }
    }

    private Task<PowerShellSessionCommandResult> ExecuteAsync(
        string sessionId,
        string command,
        Func<HostedPowerShellSession, string, CancellationToken, Task<PowerShellSessionCommandResult>> executor,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult(new PowerShellSessionCommandResult
            {
                Success = false,
                HadErrors = true,
                ErrorOutput = "sessionId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return Task.FromResult(new PowerShellSessionCommandResult
            {
                Success = false,
                SessionId = sessionId,
                HadErrors = true,
                ErrorOutput = "command is required."
            });
        }

        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(new PowerShellSessionCommandResult
            {
                Success = false,
                SessionId = sessionId,
                HadErrors = true,
                ErrorOutput = $"PowerShell session '{sessionId}' was not found."
            });
        }

        return executor(session, command, cancellationToken);
    }

    private static string? ResolveWorkingDirectory(string workspacePath, string? workingDirectory)
    {
        var candidate = string.IsNullOrWhiteSpace(workingDirectory)
            ? workspacePath
            : workingDirectory.Trim();

        if (string.IsNullOrWhiteSpace(candidate))
            candidate = Environment.CurrentDirectory;

        try
        {
            var fullPath = Path.GetFullPath(candidate);
            return Directory.Exists(fullPath) ? fullPath : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed class HostedPowerShellSession : IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly HostedPowerShellSessionHost _host = new();
        private readonly ILogger _logger;
        private readonly Runspace _runspace;
        private bool _disposed;

        public HostedPowerShellSession(
            string sessionId,
            string workspacePath,
            string workingDirectory,
            ILogger logger)
        {
            SessionId = sessionId;
            WorkspacePath = workspacePath;
            _logger = logger;
            CreatedAtUtc = DateTimeOffset.UtcNow;
            LastActivityUtc = CreatedAtUtc;

            _runspace = RunspaceFactory.CreateRunspace(_host, InitialSessionState.CreateDefault2());
            _runspace.ThreadOptions = PSThreadOptions.ReuseThread;
            _runspace.ApartmentState = System.Threading.ApartmentState.STA;
            _runspace.Open();
            _runspace.SessionStateProxy.SetVariable("WorkspacePath", workspacePath);
            _runspace.SessionStateProxy.SetVariable("HostedPowerShellSessionId", sessionId);
            _runspace.SessionStateProxy.Path.SetLocation(workingDirectory);

            using var bootstrap = System.Management.Automation.PowerShell.Create();
            bootstrap.Runspace = _runspace;
            bootstrap
                .AddScript("$ProgressPreference = 'SilentlyContinue'; $InformationPreference = 'Continue';")
                .Invoke();
        }

        public DateTimeOffset CreatedAtUtc { get; }

        public string? CurrentLocation => TryGetCurrentLocation();

        public DateTimeOffset LastActivityUtc { get; private set; }

        public string SessionId { get; }

        public string WorkspacePath { get; }

        public Task<PowerShellSessionCommandResult> ExecuteAsync(
            string command,
            CancellationToken cancellationToken) =>
            ExecuteCoreAsync(
                command,
                new HostedPowerShellCommandExecutionContext(
                    cancellationToken,
                    captureHostOutput: true),
                cancellationToken);

        public Task<PowerShellSessionCommandResult> ExecuteInteractiveAsync(
            string command,
            Func<CancellationToken, string?> readLine,
            TextWriter outputWriter,
            TextWriter errorWriter,
            CancellationToken cancellationToken) =>
            ExecuteCoreAsync(
                command,
                new HostedPowerShellCommandExecutionContext(
                    cancellationToken,
                    readLine,
                    outputWriter,
                    errorWriter,
                    captureHostOutput: false),
                cancellationToken);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _gate.Dispose();
            _runspace.Dispose();
        }

        private async Task<PowerShellSessionCommandResult> ExecuteCoreAsync(
            string command,
            HostedPowerShellCommandExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                using var powerShell = System.Management.Automation.PowerShell.Create();
                powerShell.Runspace = _runspace;
                powerShell
                    .AddScript(command)
                    .AddCommand("Out-String")
                    .AddParameter("Width", 4096);

                _host.CurrentExecutionContext = executionContext;
                using var registration = cancellationToken.Register(
                    static state => ((System.Management.Automation.PowerShell)state!).Stop(),
                    powerShell);

                Collection<PSObject> output;
                string? terminatingError = null;
                try
                {
                    output = await Task.Run(
                            powerShell.Invoke,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (PipelineStoppedException ex) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Canceled hosted PowerShell session command for {SessionId}", SessionId);
                    return CreateCanceledResult();
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Canceled hosted PowerShell session command for {SessionId}", SessionId);
                    return CreateCanceledResult();
                }
                catch (RuntimeException ex)
                {
                    _logger.LogWarning(ex, "PowerShell runtime error in hosted session {SessionId}", SessionId);
                    output = [];
                    terminatingError = ex.Message;
                }

                LastActivityUtc = DateTimeOffset.UtcNow;

                var errorOutput = JoinNonEmpty(
                    terminatingError,
                    executionContext.CapturedErrorText,
                    FormatErrors(powerShell.Streams.Error));
                var informationOutput = JoinNonEmpty(
                    executionContext.CapturedOutputText,
                    FormatInformation(powerShell.Streams.Information));

                return new PowerShellSessionCommandResult
                {
                    Success = string.IsNullOrWhiteSpace(errorOutput) && !powerShell.HadErrors,
                    SessionId = SessionId,
                    Output = NormalizeText(FormatPipelineOutput(output)),
                    ErrorOutput = NormalizeText(errorOutput),
                    WarningOutput = NormalizeText(string.Join(Environment.NewLine, powerShell.Streams.Warning.Select(static warning => warning.Message))),
                    InformationOutput = NormalizeText(informationOutput),
                    VerboseOutput = NormalizeText(string.Join(Environment.NewLine, powerShell.Streams.Verbose.Select(static verbose => verbose.Message))),
                    DebugOutput = NormalizeText(string.Join(Environment.NewLine, powerShell.Streams.Debug.Select(static debug => debug.Message))),
                    HadErrors = powerShell.HadErrors || !string.IsNullOrWhiteSpace(errorOutput),
                    CurrentLocation = CurrentLocation
                };
            }
            finally
            {
                _host.CurrentExecutionContext = null;
                _gate.Release();
            }

            PowerShellSessionCommandResult CreateCanceledResult() =>
                new()
                {
                    Success = false,
                    SessionId = SessionId,
                    HadErrors = true,
                    ErrorOutput = "PowerShell command execution was canceled.",
                    CurrentLocation = CurrentLocation
                };
        }

        private string? TryGetCurrentLocation()
        {
            try
            {
                return _runspace.RunspaceStateInfo.State == RunspaceState.Opened
                    ? _runspace.SessionStateProxy.Path.CurrentLocation?.Path
                    : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve current location for hosted PowerShell session {SessionId}", SessionId);
                return null;
            }
        }

        private static string FormatPipelineOutput(IEnumerable<PSObject> output)
        {
            var builder = new StringBuilder();
            foreach (var item in output)
                builder.Append(item?.BaseObject?.ToString());

            return builder.ToString();
        }

        private static string FormatErrors(IEnumerable<ErrorRecord> errors) =>
            string.Join(
                Environment.NewLine,
                errors.Select(static error => error.ToString()));

        private static string FormatInformation(IEnumerable<InformationRecord> informationRecords) =>
            string.Join(
                Environment.NewLine,
                informationRecords
                    .Select(static record => record.MessageData switch
                    {
                        HostInformationMessage hostMessage => hostMessage.Message,
                        null => string.Empty,
                        _ => record.MessageData.ToString() ?? string.Empty
                    })
                    .Where(static text => !string.IsNullOrWhiteSpace(text)));

        private static string? JoinNonEmpty(params string?[] values)
        {
            var filtered = values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value!.Trim())
                .ToArray();

            return filtered.Length == 0
                ? null
                : string.Join(Environment.NewLine, filtered);
        }

        private static string NormalizeText(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.ReplaceLineEndings(Environment.NewLine).TrimEnd();
    }
}
