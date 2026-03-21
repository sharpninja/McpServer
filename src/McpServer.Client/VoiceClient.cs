using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for voice conversation endpoints (<c>/mcpserver/voice</c>), including session
/// lifecycle operations, turn submission, interruption, transcript retrieval, and streaming.
/// </summary>
/// <seealso cref="McpServerClient.Voice"/>
public sealed class VoiceClient : McpClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public VoiceClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal VoiceClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Creates a new voice session.</summary>
    public async Task<VoiceSessionCreateResponse> CreateSessionAsync(VoiceSessionCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<VoiceSessionCreateResponse>("mcpserver/voice/session", request, cancellationToken);
    }

    /// <summary>Finds an active voice session by device ID.</summary>
    public async Task<VoiceSessionStatus> FindSessionByDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/voice/session?deviceId={Uri.EscapeDataString(deviceId)}";
        return await GetAsync<VoiceSessionStatus>(path, cancellationToken);
    }

    /// <summary>Submits a single voice turn for a session.</summary>
    public async Task<VoiceTurnResponse> SubmitTurnAsync(string sessionId, VoiceTurnRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<VoiceTurnResponse>($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/turn", request, cancellationToken);
    }

    /// <summary>Submits a voice turn and streams SSE events.</summary>
    public async IAsyncEnumerable<VoiceTurnStreamEvent> SubmitTurnStreamingAsync(
        string sessionId,
        VoiceTurnRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await SendRawAsync(
            HttpMethod.Post,
            $"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/turn/stream",
            request,
            HttpCompletionOption.ResponseHeadersRead,
            "text/event-stream",
            cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true);
        using var reader = new System.IO.StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(true);
            if (line is null)
                yield break;

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var data = line.Substring(6);
            VoiceTurnStreamEvent? evt;
            try
            {
                evt = JsonSerializer.Deserialize<VoiceTurnStreamEvent>(data, s_jsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (evt is not null)
                yield return evt;
        }
    }

    /// <summary>Interrupts an active turn for the specified session.</summary>
    public async Task<VoiceInterruptResponse> InterruptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await PostAsync<VoiceInterruptResponse>($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/interrupt", null, cancellationToken);
    }

    /// <summary>Sends escape characters to the active interactive voice session.</summary>
    public async Task<VoiceEscapeResponse> EscapeAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await PostAsync<VoiceEscapeResponse>($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/escape", null, cancellationToken);
    }

    /// <summary>Gets session status for the specified voice session.</summary>
    public async Task<VoiceSessionStatus> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<VoiceSessionStatus>($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}", cancellationToken);
    }

    /// <summary>Gets transcript entries for the specified voice session.</summary>
    public async Task<VoiceTranscriptResponse> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<VoiceTranscriptResponse>($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/transcript", cancellationToken);
    }

    /// <summary>Deletes a voice session.</summary>
    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await SendForStatusAsync(HttpMethod.Delete, $"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}", null, cancellationToken);
            return status == HttpStatusCode.NoContent || status == HttpStatusCode.OK;
        }
        catch (McpNotFoundException)
        {
            return false;
        }
    }
}
