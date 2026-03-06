using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Typed SSE client for incoming MCP change events from <c>/mcpserver/events</c>.
/// Supports request-time auth/workspace resolution through resolver delegates.
/// </summary>
public sealed class McpAgentEventStreamService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private const string NgrokSkipBrowserWarningHeader = "ngrok-skip-browser-warning";

    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly string? _bearerToken;

    /// <summary>
    /// Creates a new MCP event stream client with pre-resolved auth.
    /// Prefer setting the resolver properties so values are read at request time.
    /// </summary>
    public McpAgentEventStreamService(string baseUrl, string? apiKey = null, string? bearerToken = null)
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
    /// Streams change events from <c>/mcpserver/events</c> as they arrive.
    /// </summary>
    public async IAsyncEnumerable<McpIncomingChangeEvent> StreamEventsAsync(
        string? category = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = await CreateAuthorizedClientAsync(Timeout.InfiniteTimeSpan);
        HttpResponseMessage? response = null;

        try
        {
            var requestUri = string.IsNullOrWhiteSpace(category)
                ? "mcpserver/events"
                : $"mcpserver/events?category={Uri.EscapeDataString(category)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var payloadBuilder = new StringBuilder();
            string? currentEventName = null;

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                    break;

                if (line.Length == 0)
                {
                    if (payloadBuilder.Length > 0)
                        yield return DeserializeEvent(payloadBuilder, currentEventName);

                    payloadBuilder.Clear();
                    currentEventName = null;
                    continue;
                }

                if (line.StartsWith(":", StringComparison.Ordinal))
                    continue;

                if (line.StartsWith("event:", StringComparison.Ordinal))
                {
                    currentEventName = line.Length > 6 ? line[6..].Trim() : null;
                    continue;
                }

                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    var value = line.Length > 5 ? line[5..] : string.Empty;
                    if (value.StartsWith(" ", StringComparison.Ordinal))
                        value = value[1..];

                    payloadBuilder.AppendLine(value);
                }
            }

            if (payloadBuilder.Length > 0)
                yield return DeserializeEvent(payloadBuilder, currentEventName);
        }
        finally
        {
            response?.Dispose();
            client.Dispose();
        }
    }

    private static McpIncomingChangeEvent DeserializeEvent(StringBuilder payloadBuilder, string? currentEventName)
    {
        var json = payloadBuilder.ToString().TrimEnd('\r', '\n');
        var changeEvent = JsonSerializer.Deserialize<McpIncomingChangeEvent>(json, JsonOptions)
            ?? throw new InvalidOperationException("MCP event stream emitted an empty event payload.");

        if (!string.IsNullOrWhiteSpace(currentEventName) && string.IsNullOrWhiteSpace(changeEvent.SseEvent))
            changeEvent = changeEvent with { SseEvent = currentEventName };

        return changeEvent;
    }

    private Task<HttpClient> CreateAuthorizedClientAsync(TimeSpan timeout)
    {
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
            : (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        var message = response.StatusCode switch
        {
            HttpStatusCode.NotFound => "MCP event stream endpoint was not found.",
            HttpStatusCode.Unauthorized => "Unauthorized to call MCP event stream endpoint.",
            HttpStatusCode.BadRequest => string.IsNullOrWhiteSpace(body) ? "MCP event stream request was invalid." : body,
            HttpStatusCode.ServiceUnavailable => string.IsNullOrWhiteSpace(body) ? "MCP event stream service is unavailable." : body,
            _ => string.IsNullOrWhiteSpace(body)
                ? $"MCP event stream request failed ({(int)response.StatusCode} {response.ReasonPhrase})."
                : $"{body} ({(int)response.StatusCode})"
        };

        throw new InvalidOperationException(message);
    }
}

