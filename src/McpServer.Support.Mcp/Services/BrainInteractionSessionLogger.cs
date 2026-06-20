using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBEXEC-003 / TR-MCP-QBEXEC-003: Writes a brain interaction (full prompt + full output text) to the
/// session log so all interaction between the brains is logged in full, correlated by the turn's id. This
/// complements the hashed <c>BrainSlotInvocationEntity</c> audit row (which it does not replace). Logging is
/// best-effort: when no session/turn context is available, or the append fails, the orchestration continues
/// uninterrupted. Secrets are redacted before logging.
/// </summary>
public interface IBrainInteractionSessionLogger
{
    /// <summary>Appends a brain interaction's full prompt and output to the session-log turn.</summary>
    /// <param name="sourceType">Session-log source type (for example <c>QBAgent</c>).</param>
    /// <param name="sessionId">Session identifier; when null/empty the call is a no-op.</param>
    /// <param name="turnId">Turn request id (must match an existing turn); when null/empty the call is a no-op.</param>
    /// <param name="role">The brain role (LeftHemisphere, RightHemisphere, CuriosityEngine, ArbiterOfTruth).</param>
    /// <param name="prompt">The full prompt sent to the brain.</param>
    /// <param name="output">The full output returned by the brain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogInteractionAsync(
        string sourceType,
        string? sessionId,
        string? turnId,
        string role,
        string prompt,
        string? output,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-MCP-QBEXEC-001 (AC-5): Records an MCP-internal tool execution failure to the session-log turn so the
    /// failure is durably captured (not only surfaced to the agent as a note). Best-effort: a no-op when no
    /// session/turn context is available.
    /// </summary>
    /// <param name="sourceType">Session-log source type (for example <c>QBAgent</c>).</param>
    /// <param name="sessionId">Session identifier; when null/empty the call is a no-op.</param>
    /// <param name="turnId">Turn request id; when null/empty the call is a no-op.</param>
    /// <param name="toolName">The MCP-internal tool that failed.</param>
    /// <param name="error">The failure reason, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogInternalToolFailureAsync(
        string sourceType,
        string? sessionId,
        string? turnId,
        string toolName,
        string? error,
        CancellationToken cancellationToken = default);
}

/// <summary>FR-MCP-QBEXEC-003: Default <see cref="IBrainInteractionSessionLogger"/> over <see cref="ISessionLogService"/>.</summary>
public sealed partial class BrainInteractionSessionLogger : IBrainInteractionSessionLogger
{
    private readonly ISessionLogService _sessionLog;
    private readonly ILogger<BrainInteractionSessionLogger> _logger;

    /// <summary>Initializes a new instance of the <see cref="BrainInteractionSessionLogger"/> class.</summary>
    /// <param name="sessionLog">The session-log service the full-text dialog is appended to.</param>
    /// <param name="logger">Logger for best-effort failure diagnostics.</param>
    public BrainInteractionSessionLogger(ISessionLogService sessionLog, ILogger<BrainInteractionSessionLogger> logger)
    {
        _sessionLog = sessionLog ?? throw new ArgumentNullException(nameof(sessionLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task LogInteractionAsync(
        string sourceType,
        string? sessionId,
        string? turnId,
        string role,
        string prompt,
        string? output,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(turnId))
            return; // No session/turn context: nothing to correlate the full-text dialog with.

        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var items = new ProcessingDialogItemDto[]
        {
            new() { Timestamp = timestamp, Role = "user", Category = "observation", Content = $"[{role}] prompt:\n{Redact(prompt)}" },
            new() { Timestamp = timestamp, Role = "model", Category = "reasoning", Content = $"[{role}] output:\n{Redact(output ?? string.Empty)}" },
        };

        try
        {
            await _sessionLog.AppendProcessingDialogAsync(sourceType, sessionId, turnId, items, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Logging must never break orchestration; the hashed BrainSlotInvocationEntity audit row remains the
            // durable index even if full-text dialog capture fails (e.g. the turn was not yet created).
            _logger.LogWarning(ex, "Inter-brain full-text session logging failed for {SourceType}/{SessionId}/{TurnId} role {Role}.",
                sourceType, sessionId, turnId, role);
        }
    }

    /// <inheritdoc />
    public async Task LogInternalToolFailureAsync(
        string sourceType,
        string? sessionId,
        string? turnId,
        string toolName,
        string? error,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(turnId))
            return; // No session/turn context to correlate the failure with.

        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var items = new ProcessingDialogItemDto[]
        {
            new()
            {
                Timestamp = timestamp,
                Role = "model",
                Category = "error",
                Content = $"MCP-internal tool '{toolName}' failed server-side and was not emitted to the agent"
                          + (string.IsNullOrWhiteSpace(error) ? "." : $": {Redact(error)}"),
            },
        };

        try
        {
            await _sessionLog.AppendProcessingDialogAsync(sourceType, sessionId, turnId, items, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Internal-tool failure session logging failed for {SourceType}/{SessionId}/{TurnId} tool {Tool}.",
                sourceType, sessionId, turnId, toolName);
        }
    }

    /// <summary>Redacts common secret shapes (bearer tokens, api keys) before full-text logging.</summary>
    /// <param name="text">The text to redact.</param>
    /// <returns>The redacted text.</returns>
    public static string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = BearerRegex().Replace(text, "Bearer [REDACTED]");
        text = ApiKeyRegex().Replace(text, "${1}[REDACTED]");
        return text;
    }

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"(?i)((?:x-api-key|api[_-]?key|apikey)\s*[:=]\s*)[A-Za-z0-9\-._]{8,}")]
    private static partial Regex ApiKeyRegex();
}
