using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Default no-op voice service used when host does not provide implementation.
/// </summary>
public sealed class NoOpVoiceConversationService : IVoiceConversationService
{
    /// <inheritdoc />
    public Func<string?>? ResolveWorkspacePath { get; set; }

    /// <inheritdoc />
    public string? WorkspacePath { get; set; }

    /// <inheritdoc />
    public Task<McpVoiceSessionCreateResponse> CreateSessionAsync(
        McpVoiceSessionCreateRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new McpVoiceSessionCreateResponse
        {
            SessionId = string.Empty,
            Status = "noop",
            Language = request.Language ?? "en-US"
        });

    /// <inheritdoc />
    public Task<McpVoiceTurnResponse> SubmitTurnAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new McpVoiceTurnResponse
        {
            SessionId = sessionId,
            TurnId = string.Empty,
            Status = "noop",
            AssistantDisplayText = string.Empty,
            AssistantSpeakText = string.Empty,
            ToolCalls = Array.Empty<McpVoiceToolCallRecord>(),
            Error = "Not configured",
            LatencyMs = 0
        });

    /// <inheritdoc />
    public async IAsyncEnumerable<McpVoiceTurnStreamEvent> SubmitTurnStreamingAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new McpVoiceTurnStreamEvent
        {
            Type = "error",
            Message = "Not configured",
            TurnId = string.Empty,
            Status = "noop"
        };
    }

    /// <inheritdoc />
    public Task<McpVoiceInterruptResponse> InterruptAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new McpVoiceInterruptResponse
        {
            SessionId = sessionId,
            Interrupted = false,
            Status = "noop"
        });

    /// <inheritdoc />
    public Task<bool> SendEscapeAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    /// <inheritdoc />
    public Task<McpVoiceSessionStatus> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new McpVoiceSessionStatus
        {
            SessionId = sessionId,
            Status = "noop",
            Language = "en-US",
            CreatedUtc = string.Empty,
            LastUpdatedUtc = string.Empty,
            IsTurnActive = false,
            LastError = "Not configured",
            LastTurnId = null,
            TurnCounter = 0,
            TranscriptCount = 0
        });

    /// <inheritdoc />
    public Task<McpVoiceTranscriptResponse> GetTranscriptAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new McpVoiceTranscriptResponse
        {
            SessionId = sessionId,
            Items = Array.Empty<McpVoiceTranscriptEntry>()
        });

    /// <inheritdoc />
    public Task<McpVoiceSessionStatus?> FindExistingSessionAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<McpVoiceSessionStatus?>(null);

    /// <inheritdoc />
    public Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
