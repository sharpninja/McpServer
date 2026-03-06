using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Typed client for MCP voice conversation REST endpoints.
/// When <see cref="ResolveBaseUrl"/>, <see cref="ResolveBearerToken"/>, etc. are set,
/// auth and workspace are resolved at request time (pull from source of truth).
/// Otherwise falls back to cached construction-time values.
/// </summary>
public sealed class McpVoiceConversationService : IVoiceConversationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ILogger s_logger = AppLogService.Instance.CreateLogger("McpVoiceConversationService");
    private const string NgrokSkipBrowserWarningHeader = "ngrok-skip-browser-warning";

    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly string? _bearerToken;

    /// <summary>
    /// Creates a new MCP voice conversation client with pre-resolved auth.
    /// Prefer setting the <c>Resolve*</c> Func properties so values are read at request time.
    /// </summary>
    public McpVoiceConversationService(string baseUrl, string? apiKey = null, string? bearerToken = null)
    {
        _baseUrl = McpServerRestClientFactory.NormalizeBaseUrl(baseUrl);
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        _bearerToken = string.IsNullOrWhiteSpace(bearerToken) ? null : bearerToken.Trim();
    }

    /// <summary>When set, overrides the cached base URL at request time.</summary>
    public Func<string>? ResolveBaseUrl { get; set; }

    /// <summary>When set, overrides the cached API key at request time.</summary>
    public Func<string?>? ResolveApiKey { get; set; }

    /// <summary>When set, overrides the cached bearer token at request time.</summary>
    public Func<string?>? ResolveBearerToken { get; set; }

    /// <summary>When set, overrides the cached workspace path at request time.</summary>
    public Func<string?>? ResolveWorkspacePath { get; set; }

    /// <summary>
    /// Gets or sets the workspace root path sent via <c>X-Workspace-Path</c> header.
    /// Superseded by <see cref="ResolveWorkspacePath"/> when set.
    /// </summary>
    public string? WorkspacePath { get; set; }

    /// <summary>
    /// Creates a voice session on the MCP server.
    /// </summary>
    public async Task<McpVoiceSessionCreateResponse> CreateSessionAsync(
        McpVoiceSessionCreateRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(true);
        using var response = await client.PostAsJsonAsync("mcpserver/voice/session", request ?? new McpVoiceSessionCreateRequest(), JsonOptions, cancellationToken).ConfigureAwait(true);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
        return (await response.Content.ReadFromJsonAsync<McpVoiceSessionCreateResponse>(JsonOptions, cancellationToken).ConfigureAwait(true))
            ?? throw new InvalidOperationException("MCP voice create session returned an empty response.");
    }

    /// <summary>
    /// Submits a transcribed turn to the MCP voice session.
    /// </summary>
    public async Task<McpVoiceTurnResponse> SubmitTurnAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(true);
        using var response = await client.PostAsJsonAsync(
            $"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/turn",
            request,
            JsonOptions,
            cancellationToken).ConfigureAwait(true);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
        return (await response.Content.ReadFromJsonAsync<McpVoiceTurnResponse>(JsonOptions, cancellationToken).ConfigureAwait(true))
            ?? throw new InvalidOperationException("MCP voice submit turn returned an empty response.");
    }

    /// <summary>
    /// Submits a turn via the SSE streaming endpoint. Yields events as they arrive.
    /// </summary>
    public async IAsyncEnumerable<McpVoiceTurnStreamEvent> SubmitTurnStreamingAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        var yieldedAnyEvents = false;
        IAsyncEnumerable<McpVoiceTurnStreamEvent>? fallbackStream = null;
        await using var enumerator = StreamTurnCoreAsync(sessionId, request, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (Exception ex) when (ShouldFallbackToNonStreamingTurn(ex, cancellationToken, yieldedAnyEvents))
            {
                s_logger.LogWarning(
                    ex,
                    "Streaming voice transport failed before the first SSE event for session {SessionId}; falling back to non-streaming turn.",
                    sessionId);

                fallbackStream = SubmitTurnWithFallbackAsync(sessionId, request, cancellationToken);
                break;
            }

            if (!hasNext)
                yield break;

            yieldedAnyEvents = true;
            yield return enumerator.Current;
        }

        if (fallbackStream is null)
            yield break;

        await foreach (var evt in fallbackStream)
            yield return evt;
    }

    /// <summary>
    /// Interrupts an active turn for a voice session.
    /// </summary>
    public async Task<McpVoiceInterruptResponse> InterruptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(true);
        using var response = await client.PostAsync($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/interrupt", content: null, cancellationToken).ConfigureAwait(true);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
        return (await response.Content.ReadFromJsonAsync<McpVoiceInterruptResponse>(JsonOptions, cancellationToken).ConfigureAwait(true))
            ?? throw new InvalidOperationException("MCP voice interrupt returned an empty response.");
    }

    /// <summary>
    /// Sends three ESC characters to the active Copilot interactive session stdin,
    /// cancelling the current generation without ending the session.
    /// Returns <c>false</c> if the session was not found or has no active interactive session.
    /// </summary>
    public async Task<bool> SendEscapeAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(true);
        using var response = await client.PostAsync($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/escape", content: null, cancellationToken).ConfigureAwait(true);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
        return true;
    }

    /// <summary>
    /// Gets a voice session status snapshot.
    /// </summary>
    public async Task<McpVoiceSessionStatus> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(true);
        using var response = await client.GetAsync($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}", cancellationToken).ConfigureAwait(true);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
        return (await response.Content.ReadFromJsonAsync<McpVoiceSessionStatus>(JsonOptions, cancellationToken).ConfigureAwait(true))
            ?? throw new InvalidOperationException("MCP voice status returned an empty response.");
    }

    /// <summary>
    /// Gets the transcript entries for a voice session.
    /// </summary>
    public async Task<McpVoiceTranscriptResponse> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(true);
        using var response = await client.GetAsync($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/transcript", cancellationToken).ConfigureAwait(true);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
        return (await response.Content.ReadFromJsonAsync<McpVoiceTranscriptResponse>(JsonOptions, cancellationToken).ConfigureAwait(true))
            ?? throw new InvalidOperationException("MCP voice transcript returned an empty response.");
    }

    /// <summary>
    /// Finds an active voice session for the specified device.
    /// Returns <c>null</c> if no active session exists.
    /// </summary>
    public async Task<McpVoiceSessionStatus?> FindExistingSessionAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(true);
        using var response = await client.GetAsync($"mcpserver/voice/session?deviceId={Uri.EscapeDataString(deviceId)}", cancellationToken).ConfigureAwait(true);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
        return await response.Content.ReadFromJsonAsync<McpVoiceSessionStatus>(JsonOptions, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Deletes a voice session and any associated in-memory state.
    /// </summary>
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        using var client = await CreateAuthorizedClientAsync(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(true);
        using var response = await client.DeleteAsync($"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}", cancellationToken).ConfigureAwait(true);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(true);
    }

    private Task<HttpClient> CreateAuthorizedClientAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        // Resolve all values at request time — Func resolvers take priority over cached construction-time values
        var baseUrl = ResolveBaseUrl?.Invoke() ?? _baseUrl;
        var apiKey = ResolveApiKey?.Invoke() ?? _apiKey;
        var bearerToken = ResolveBearerToken?.Invoke() ?? _bearerToken;
        var workspacePath = ResolveWorkspacePath?.Invoke() ?? WorkspacePath;

        var client = new HttpClient
        {
            BaseAddress = new Uri(McpServerRestClientFactory.NormalizeBaseUrl(baseUrl).TrimEnd('/') + "/", UriKind.Absolute),
            Timeout = timeout
        };

        // Bearer takes precedence (mutual exclusivity with API key)
        if (!string.IsNullOrWhiteSpace(bearerToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        else if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation(NgrokSkipBrowserWarningHeader, "true");

        if (!string.IsNullOrWhiteSpace(workspacePath))
            client.DefaultRequestHeaders.Add("X-Workspace-Path", workspacePath);

        return Task.FromResult(client);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = response.Content is null
            ? string.Empty
            : (await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true)).Trim();

        // Try to extract {"error":"..."} from JSON response body
        var detail = ExtractErrorDetail(body);

        var message = response.StatusCode switch
        {
            HttpStatusCode.NotFound => string.IsNullOrWhiteSpace(detail) ? "MCP voice endpoint or session was not found." : detail,
            HttpStatusCode.Unauthorized => "Unauthorized to call MCP voice endpoint (API key missing/invalid).",
            HttpStatusCode.BadRequest => string.IsNullOrWhiteSpace(detail) ? "MCP voice request was invalid." : detail,
            HttpStatusCode.ServiceUnavailable => string.IsNullOrWhiteSpace(detail) ? "MCP voice service is unavailable." : detail,
            _ => string.IsNullOrWhiteSpace(detail)
                ? $"MCP voice request failed ({(int)response.StatusCode} {response.ReasonPhrase})."
                : $"{detail} ({(int)response.StatusCode})"
        };

        throw new InvalidOperationException(message);
    }

    private static string? ExtractErrorDetail(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == System.Text.Json.JsonValueKind.String)
                return errorProp.GetString();
        }
        catch { /* not JSON or missing field — use raw body */ }
        return body;
    }

    private async IAsyncEnumerable<McpVoiceTurnStreamEvent> StreamTurnCoreAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = await CreateAuthorizedClientAsync(TimeSpan.FromMinutes(5), cancellationToken);
        HttpResponseMessage? response = null;
        try
        {
            var jsonBody = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/turn/stream")
            { Content = content };

            // SSE requires text/event-stream Accept header for proper proxy behavior.
            httpRequest.Headers.Accept.Clear();
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            response = await client.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                    yield break;

                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                    continue;

                var json = line.AsSpan(6);
                if (json.IsEmpty)
                    continue;

                McpVoiceTurnStreamEvent? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<McpVoiceTurnStreamEvent>(json, JsonOptions);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (evt is null)
                    continue;

                if (evt.Text is not null)
                    evt = evt with { Text = SanitizeStreamText(evt.Text) };

                yield return evt;

                if (evt.Type is "done" or "error")
                    yield break;
            }
        }
        finally
        {
            response?.Dispose();
            client.Dispose();
        }
    }

    private async IAsyncEnumerable<McpVoiceTurnStreamEvent> SubmitTurnWithFallbackAsync(
        string sessionId,
        McpVoiceTurnRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await SubmitTurnAsync(sessionId, request, cancellationToken);
        var displayText = SanitizeStreamText(response.AssistantDisplayText ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(displayText))
        {
            yield return new McpVoiceTurnStreamEvent
            {
                Type = "chunk",
                TurnId = response.TurnId,
                Text = displayText
            };
        }

        if (!string.IsNullOrWhiteSpace(response.Error) &&
            !string.Equals(response.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            yield return new McpVoiceTurnStreamEvent
            {
                Type = "error",
                TurnId = response.TurnId,
                Status = response.Status,
                Message = response.Error,
                ToolCalls = response.ToolCalls,
                LatencyMs = response.LatencyMs
            };
            yield break;
        }

        yield return new McpVoiceTurnStreamEvent
        {
            Type = "done",
            TurnId = response.TurnId,
            Status = string.IsNullOrWhiteSpace(response.Status) ? "completed" : response.Status,
            ToolCalls = response.ToolCalls,
            LatencyMs = response.LatencyMs
        };
    }

    private static bool ShouldFallbackToNonStreamingTurn(Exception ex, CancellationToken cancellationToken, bool yieldedAnyEvents)
        => !cancellationToken.IsCancellationRequested &&
           !yieldedAnyEvents &&
           IsStreamingTransportFailure(ex);

    private static bool IsStreamingTransportFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException)
                return false;

            var message = current.Message;
            if (string.IsNullOrWhiteSpace(message))
                continue;

            if (message.Contains("socket closed", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("connection reset", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("unexpected end of stream", StringComparison.OrdinalIgnoreCase))
            {
                return current is HttpRequestException or WebException or IOException || ReferenceEquals(current, ex);
            }
        }

        return false;
    }

    /// <summary>
    /// Sanitizes streamed text: replaces ESC chars with "ESC" text and
    /// unicode characters above U+00FF with HTML entity escaping.
    /// </summary>
    private static string SanitizeStreamText(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch == '\x1B')
                sb.Append("ESC");
            else if (ch > '\x00FF')
                sb.Append($"&#x{(int)ch:X4};");
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }
}


