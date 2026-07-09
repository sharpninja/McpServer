using System.Text;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Describes one durable session-log persistence implementation.
/// </summary>
public interface ISessionLogPersistenceStrategy
{
    /// <summary>
    /// Gets the stable strategy name exposed in REPL persistence results.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Persists the supplied session-log snapshot.
    /// </summary>
    /// <param name="sessionLog">Session-log snapshot to persist.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>Durable persistence details.</returns>
    Task<SessionLogPersistenceResult> PersistAsync(
        UnifiedSessionLogDto sessionLog,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reports where a session-log snapshot was durably persisted.
/// </summary>
/// <param name="Persisted">Whether durable persistence succeeded.</param>
/// <param name="Degraded">Whether persistence used a degraded fallback.</param>
/// <param name="Strategy">Stable name of the strategy that persisted the snapshot.</param>
/// <param name="FailsafePath">Absolute recovery artifact path when degraded.</param>
/// <param name="Message">Optional operator-facing persistence message.</param>
public sealed record SessionLogPersistenceResult(
    bool Persisted,
    bool Degraded,
    string Strategy,
    string? FailsafePath,
    string? Message);

/// <summary>
/// Persists session-log snapshots through the MCP Session Log client.
/// </summary>
public sealed class McpSessionLogPersistenceStrategy : ISessionLogPersistenceStrategy
{
    private readonly ISessionLogClientAdapter _client;

    /// <summary>
    /// Initializes the primary MCP persistence strategy.
    /// </summary>
    /// <param name="client">Typed Session Log client adapter.</param>
    public McpSessionLogPersistenceStrategy(ISessionLogClientAdapter client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public string Name => "mcp-service";

    /// <inheritdoc />
    public async Task<SessionLogPersistenceResult> PersistAsync(
        UnifiedSessionLogDto sessionLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        await _client.SubmitAsync(sessionLog, cancellationToken).ConfigureAwait(false);
        return new SessionLogPersistenceResult(
            Persisted: true,
            Degraded: false,
            Strategy: Name,
            FailsafePath: null,
            Message: null);
    }
}

/// <summary>
/// Coordinates primary and failsafe persistence without coupling either strategy to the other.
/// </summary>
public sealed class FailoverSessionLogPersistenceStrategy : ISessionLogPersistenceStrategy
{
    private readonly ISessionLogPersistenceStrategy _primary;
    private readonly ISessionLogPersistenceStrategy _failsafe;

    /// <summary>
    /// Initializes a failover coordinator with independent persistence strategies.
    /// </summary>
    /// <param name="primary">Primary MCP persistence strategy.</param>
    /// <param name="failsafe">Filesystem failsafe persistence strategy.</param>
    public FailoverSessionLogPersistenceStrategy(
        ISessionLogPersistenceStrategy primary,
        ISessionLogPersistenceStrategy failsafe)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _failsafe = failsafe ?? throw new ArgumentNullException(nameof(failsafe));
    }

    /// <inheritdoc />
    public string Name => "mcp-service-with-failsafe";

    /// <inheritdoc />
    public async Task<SessionLogPersistenceResult> PersistAsync(
        UnifiedSessionLogDto sessionLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        try
        {
            return await _primary.PersistAsync(sessionLog, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception primaryException)
        {
            try
            {
                var result = await _failsafe.PersistAsync(sessionLog, cancellationToken).ConfigureAwait(false);
                if (!result.Persisted || !result.Degraded || string.IsNullOrWhiteSpace(result.FailsafePath))
                {
                    throw new InvalidOperationException(
                        $"Failsafe strategy '{_failsafe.Name}' did not return a durable degraded persistence result.");
                }

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception failsafeException)
            {
                throw new SessionLogPersistenceException(primaryException, failsafeException);
            }
        }
    }
}

/// <summary>
/// Indicates that both primary MCP and filesystem failsafe persistence failed.
/// </summary>
public sealed class SessionLogPersistenceException : Exception
{
    /// <summary>
    /// Initializes a dual persistence failure.
    /// </summary>
    /// <param name="primaryException">Primary MCP persistence failure.</param>
    /// <param name="failsafeException">Filesystem failsafe persistence failure.</param>
    public SessionLogPersistenceException(Exception primaryException, Exception failsafeException)
        : base(
            "Session log persistence failed through both the primary MCP service and the filesystem failsafe.",
            new AggregateException(primaryException, failsafeException))
    {
        PrimaryException = primaryException ?? throw new ArgumentNullException(nameof(primaryException));
        FailsafeException = failsafeException ?? throw new ArgumentNullException(nameof(failsafeException));
    }

    /// <summary>
    /// Gets the primary MCP persistence failure.
    /// </summary>
    public Exception PrimaryException { get; }

    /// <summary>
    /// Gets the filesystem failsafe persistence failure.
    /// </summary>
    public Exception FailsafeException { get; }
}

/// <summary>
/// Persists replayable session-log recovery envelopes to a workspace-and-agent-scoped filesystem queue.
/// </summary>
public sealed class FilesystemSessionLogPersistenceStrategy : ISessionLogPersistenceStrategy
{
    private readonly string _workspacePath;
    private readonly IYamlSerializer _serializer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the filesystem failsafe strategy.
    /// </summary>
    /// <param name="workspacePath">Active workspace root.</param>
    /// <param name="serializer">Canonical REPL YAML serializer.</param>
    /// <param name="timeProvider">Time provider used for artifact identifiers.</param>
    public FilesystemSessionLogPersistenceStrategy(
        string workspacePath,
        IYamlSerializer serializer,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        _workspacePath = Path.GetFullPath(workspacePath);
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public string Name => "filesystem-failsafe";

    /// <inheritdoc />
    public async Task<SessionLogPersistenceResult> PersistAsync(
        UnifiedSessionLogDto sessionLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionLog.SourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionLog.SessionId);
        cancellationToken.ThrowIfCancellationRequested();

        var pendingDirectory = GetPendingDirectory(sessionLog.SourceType);
        Directory.CreateDirectory(pendingDirectory);

        var now = _timeProvider.GetUtcNow();
        var token = Guid.NewGuid().ToString("N");
        var requestId = $"req-{now:yyyyMMddTHHmmssZ}-failsafe-{token}";
        var safeSessionId = SanitizePathSegment(sessionLog.SessionId);
        var finalPath = Path.GetFullPath(Path.Combine(
            pendingDirectory,
            $"sessionlog-{safeSessionId}-{now:yyyyMMddTHHmmssZ}-{token}.yaml"));
        var temporaryPath = finalPath + ".tmp";
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = requestId,
                Method = SessionLogCommandShapes.ImportRecoveryMethod,
                Params = new Dictionary<string, object?>
                {
                    ["sessionLog"] = sessionLog
                }
            }
        };
        var yaml = _serializer.Serialize(envelope);

        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                yaml,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, finalPath);

            return new SessionLogPersistenceResult(
                Persisted: true,
                Degraded: true,
                Strategy: Name,
                FailsafePath: finalPath,
                Message: $"MCP Session Log persistence is degraded. Turn saved to failsafe path '{finalPath}'.");
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private string GetPendingDirectory(string sourceType)
    {
        var workspaceBytes = Encoding.UTF8.GetBytes(_workspacePath);
        var workspaceKey = Convert.ToBase64String(workspaceBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return Path.Combine(
            _workspacePath,
            ".mcpServer",
            "failsafe",
            SanitizePathSegment(sourceType),
            "workspaces",
            workspaceKey,
            "pending");
    }

    private static string SanitizePathSegment(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : '_');
        }

        var result = builder.ToString().Trim('.');
        return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
    }
}
