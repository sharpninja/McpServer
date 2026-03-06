using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Bridges <see cref="McpVoiceConversationService"/> to UI.Core's <see cref="IVoiceApiClient"/>
/// so that <c>VoiceViewModel</c> and its CQRS handlers can operate against the app-owned service.
/// </summary>
internal sealed class UiCoreVoiceApiClientAdapter : IVoiceApiClient
{
    private readonly McpVoiceConversationService _service;
    private readonly ILogger<UiCoreVoiceApiClientAdapter> _logger;

    public UiCoreVoiceApiClientAdapter(
        McpVoiceConversationService service,
        ILogger<UiCoreVoiceApiClientAdapter>? logger = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? NullLogger<UiCoreVoiceApiClientAdapter>.Instance;
    }

    public async Task<VoiceSessionInfo> CreateSessionAsync(CreateVoiceSessionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = UiCoreMessageMapper.ToVoiceSessionCreateRequest(command);
        var result = await _service.CreateSessionAsync(request, cancellationToken);
        return UiCoreMessageMapper.ToVoiceSessionInfo(result);
    }

    public async Task<VoiceTurnInfo> SubmitTurnAsync(SubmitVoiceTurnCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = UiCoreMessageMapper.ToVoiceTurnRequest(command);
        var result = await _service.SubmitTurnAsync(command.SessionId, request, cancellationToken);
        return UiCoreMessageMapper.ToVoiceTurnInfo(result);
    }

    public async Task<VoiceInterruptInfo> InterruptAsync(InterruptVoiceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await _service.InterruptAsync(command.SessionId, cancellationToken);
        return UiCoreMessageMapper.ToVoiceInterruptInfo(result);
    }

    public async Task<VoiceSessionStatusInfo?> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetStatusAsync(sessionId, cancellationToken);
            return UiCoreMessageMapper.ToVoiceSessionStatusInfo(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Voice GetStatus failed for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<VoiceTranscriptInfo?> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetTranscriptAsync(sessionId, cancellationToken);
            return UiCoreMessageMapper.ToVoiceTranscriptInfo(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Voice GetTranscript failed for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _service.DeleteSessionAsync(sessionId, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Voice DeleteSession failed for session {SessionId}", sessionId);
            return false;
        }
    }
}

