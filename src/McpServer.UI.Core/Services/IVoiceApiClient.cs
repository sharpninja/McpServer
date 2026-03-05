using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over voice conversation endpoints used by UI.Core CQRS handlers.
/// </summary>
public interface IVoiceApiClient
{
    /// <summary>Creates a voice session.</summary>
    Task<VoiceSessionInfo> CreateSessionAsync(CreateVoiceSessionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Submits a voice turn.</summary>
    Task<VoiceTurnInfo> SubmitTurnAsync(SubmitVoiceTurnCommand command, CancellationToken cancellationToken = default);

    /// <summary>Interrupts the current voice turn.</summary>
    Task<VoiceInterruptInfo> InterruptAsync(InterruptVoiceCommand command, CancellationToken cancellationToken = default);

    /// <summary>Gets voice session status by session ID.</summary>
    Task<VoiceSessionStatusInfo?> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Gets voice transcript by session ID.</summary>
    Task<VoiceTranscriptInfo?> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a voice session by session ID.</summary>
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
