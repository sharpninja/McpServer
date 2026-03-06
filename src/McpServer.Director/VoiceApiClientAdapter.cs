using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IVoiceApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class VoiceApiClientAdapter : IVoiceApiClient
{
    private readonly DirectorMcpContext _context;
    private readonly ILogger<VoiceApiClientAdapter> _logger;

    public VoiceApiClientAdapter(
        DirectorMcpContext context,
        ILogger<VoiceApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<VoiceApiClientAdapter>.Instance;
    }

    public async Task<VoiceSessionInfo> CreateSessionAsync(CreateVoiceSessionCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Voice.CreateSessionAsync(
            new VoiceSessionCreateRequest
            {
                Language = command.Language,
                DeviceId = command.DeviceId,
                ClientName = command.ClientName,
                WorkspacePath = command.WorkspacePath,
                AgentName = command.AgentName,
                AgentPath = command.AgentPath,
                AgentModel = command.AgentModel,
                AgentSeed = command.AgentSeed,
                AgentPrompt = command.AgentPrompt,
                AgentParameters = command.AgentParameters,
                OneShotSession = command.OneShotSession
            },
            cancellationToken).ConfigureAwait(true);

        return new VoiceSessionInfo(
            result.SessionId,
            result.Status,
            result.Language,
            result.ModelRequested,
            result.ModelResolved);
    }

    public async Task<VoiceTurnInfo> SubmitTurnAsync(SubmitVoiceTurnCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Voice.SubmitTurnAsync(
            command.SessionId,
            new VoiceTurnRequest
            {
                UserTranscriptText = command.UserTranscriptText,
                Language = command.Language,
                ClientTimestampUtc = command.ClientTimestampUtc
            },
            cancellationToken).ConfigureAwait(true);

        var toolCalls = result.ToolCalls?.Select(MapToolCall).ToList() ?? [];
        return new VoiceTurnInfo(
            result.SessionId,
            result.TurnId,
            result.Status,
            result.AssistantDisplayText,
            result.AssistantSpeakText,
            toolCalls,
            result.Error,
            result.LatencyMs,
            result.ModelRequested,
            result.ModelResolved);
    }

    public async Task<VoiceInterruptInfo> InterruptAsync(InterruptVoiceCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Voice.InterruptAsync(command.SessionId, cancellationToken).ConfigureAwait(true);
        return new VoiceInterruptInfo(result.SessionId, result.Interrupted, result.Status);
    }

    public async Task<VoiceSessionStatusInfo?> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
            var result = await client.Voice.GetStatusAsync(sessionId, cancellationToken).ConfigureAwait(true);
            return new VoiceSessionStatusInfo(
                result.SessionId,
                result.Status,
                result.Language,
                result.CreatedUtc,
                result.LastUpdatedUtc,
                result.IsTurnActive,
                result.LastError,
                result.LastTurnId,
                result.TurnCounter,
                result.TranscriptCount);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<VoiceTranscriptInfo?> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
            var result = await client.Voice.GetTranscriptAsync(sessionId, cancellationToken).ConfigureAwait(true);
            return new VoiceTranscriptInfo(
                result.SessionId,
                result.Items.Select(i => new VoiceTranscriptEntryInfo(i.TimestampUtc, i.TurnId, i.Role, i.Category, i.Text)).ToList());
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        return await client.Voice.DeleteSessionAsync(sessionId, cancellationToken).ConfigureAwait(true);
    }

    private async Task<McpServerClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_context.HasControlConnection)
            return await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(true);
        return await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
    }

    private static VoiceToolCallInfo MapToolCall(VoiceToolCallRecord call)
        => new(
            call.TurnId,
            call.ToolName,
            call.Step,
            call.ArgumentsJson,
            call.Status,
            call.IsMutation,
            call.ResultSummary,
            call.Error);
}
