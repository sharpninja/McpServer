using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Service abstraction for MCP voice conversation operations.
/// </summary>
public interface IVoiceConversationService
{
    /// <summary>Dynamic workspace resolver invoked before each request when set.</summary>
    Func<string?>? ResolveWorkspacePath { get; set; }

    /// <summary>Fallback workspace path used when resolver is not set.</summary>
    string? WorkspacePath { get; set; }

    /// <summary>Creates a new voice session.</summary>
    Task<McpVoiceSessionCreateResponse> CreateSessionAsync(
        McpVoiceSessionCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Submits one voice turn and waits for completion.</summary>
    Task<McpVoiceTurnResponse> SubmitTurnAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Submits one voice turn and streams events.</summary>
    IAsyncEnumerable<McpVoiceTurnStreamEvent> SubmitTurnStreamingAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Interrupts an active turn if possible.</summary>
    Task<McpVoiceInterruptResponse> InterruptAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Sends ESC to an active interactive turn when supported.</summary>
    Task<bool> SendEscapeAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets current session status.</summary>
    Task<McpVoiceSessionStatus> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets transcript for a session.</summary>
    Task<McpVoiceTranscriptResponse> GetTranscriptAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds an existing session for a device when available.</summary>
    Task<McpVoiceSessionStatus?> FindExistingSessionAsync(
        string deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a session.</summary>
    Task DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
