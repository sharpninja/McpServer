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

namespace McpServer.Client;

/// <summary>
/// Abstract base class for all MCP Server sub-clients (e.g. <see cref="TodoClient"/>,
/// <see cref="WorkspaceClient"/>). Provides shared HTTP plumbing, automatic
/// <c>X-Api-Key</c> header injection, and dynamic port-based URL construction.
///
/// <para><strong>Runtime authentication:</strong> Every outbound request reads the current
/// value of <see cref="ApiKey"/> and <see cref="BearerToken"/>. At least one must be set at
/// call time, otherwise an <see cref="InvalidOperationException"/> is thrown — this avoids
/// silent 401 failures.</para>
///
/// <para><strong>Dynamic port:</strong> The <see cref="Port"/> property is read at call time
/// to construct the request URL, so callers can retarget a client to a different workspace
/// host without creating a new instance.</para>
/// </summary>
public abstract class McpClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly string _scheme;
    private readonly string _host;

    /// <summary>
    /// Initializes a new instance of the sub-client, extracting scheme, host, and port
    /// from <paramref name="options"/>.<see cref="McpServerClientOptions.BaseUrl"/> and
    /// seeding <see cref="ApiKey"/> from <paramref name="options"/>.<see cref="McpServerClientOptions.ApiKey"/>.
    /// </summary>
    /// <param name="http">
    /// The <see cref="HttpClient"/> used for all outbound HTTP requests.
    /// Callers typically share a single instance (or use <c>IHttpClientFactory</c>).
    /// </param>
    /// <param name="options">
    /// Configuration snapshot. <see cref="McpServerClientOptions.BaseUrl"/> supplies scheme,
    /// host, and initial port. <see cref="McpServerClientOptions.ApiKey"/> is an optional
    /// seed value — the key can also be set later via the <see cref="ApiKey"/> property.
    /// <see cref="McpServerClientOptions.BearerToken"/> seeds <see cref="BearerToken"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="http"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    protected McpClientBase(HttpClient http, McpServerClientOptions options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        if (options is null) throw new ArgumentNullException(nameof(options));

        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
        Port = options.BaseUrl.Port;
        ApiKey = options.ApiKey ?? string.Empty;
        BearerToken = options.BearerToken ?? string.Empty;
        WorkspacePath = options.WorkspacePath ?? string.Empty;
    }

    /// <summary>
    /// API key for workspace authentication, sent as the <c>X-Api-Key</c> header on every
    /// request. The value is read at call time so it can be rotated without recreating the
    /// client. Must be non-empty before any endpoint is called; otherwise an
    /// <see cref="InvalidOperationException"/> is thrown.
    ///
    /// <para>Obtain the key from the <c>AGENTS-README-FIRST.yaml</c> marker file that the
    /// MCP Server writes to each workspace root on startup.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// client.ApiKey = File.ReadAllText("AGENTS-README-FIRST.yaml")
    ///     .Split("apiKey:")[1].Trim();
    /// </code>
    /// </example>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional JWT bearer token sent as the <c>Authorization: Bearer</c> header on every
    /// request. The value is read at call time so it can be refreshed without recreating the
    /// client. When set, requests may be authorized by the server without an API key.
    /// Setting this to a non-empty value automatically enables <see cref="RequireBearerToken"/>.
    /// </summary>
    public string BearerToken
    {
        get => _bearerToken;
        set
        {
            _bearerToken = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_bearerToken))
                RequireBearerToken = true;
        }
    }
    private string _bearerToken = string.Empty;

    /// <summary>
    /// When <see langword="true"/>, every outbound request <b>must</b> carry a Bearer token.
    /// An <see cref="InvalidOperationException"/> is thrown if <see cref="BearerToken"/> is
    /// empty at call time — the client will never silently fall back to an API key.
    /// This flag is set automatically the first time <see cref="BearerToken"/> is assigned
    /// a non-empty value.
    /// </summary>
    public bool RequireBearerToken { get; set; }

    /// <summary>
    /// Optional workspace path sent as the <c>X-Workspace-Path</c> header on every request.
    /// Used for multi-tenant workspace routing. The value is read at call time so it can be
    /// changed without recreating the client.
    /// </summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>
    /// TCP port used to construct the base URL for API calls (e.g. <c>http://localhost:{Port}/</c>).
    /// Initialized from <see cref="McpServerClientOptions.BaseUrl"/> and can be changed at
    /// any time — the new value takes effect on the very next HTTP call. This allows a single
    /// client instance to be retargeted to a different workspace host.
    /// </summary>
    /// <example>
    /// <code>
    /// client.Port = 7149; // switch to the "other-project" workspace
    /// var items = await client.Todo.QueryAsync();
    /// </code>
    /// </example>
    public int Port { get; set; }

    /// <summary>Sends a GET request and deserializes the JSON response body to <typeparamref name="T"/>.</summary>
    /// <inheritdoc cref="SendAsync{T}(HttpMethod, string, object?, CancellationToken)" path="/exception"/>
    protected Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);

    /// <summary>Sends a POST request with a JSON body and deserializes the response to <typeparamref name="T"/>.</summary>
    /// <inheritdoc cref="SendAsync{T}(HttpMethod, string, object?, CancellationToken)" path="/exception"/>
    protected Task<T> PostAsync<T>(string path, object? body, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Post, path, body, cancellationToken);

    /// <summary>Sends a PUT request with a JSON body and deserializes the response to <typeparamref name="T"/>.</summary>
    /// <inheritdoc cref="SendAsync{T}(HttpMethod, string, object?, CancellationToken)" path="/exception"/>
    protected Task<T> PutAsync<T>(string path, object? body, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Put, path, body, cancellationToken);

    /// <summary>Sends a DELETE request and deserializes the JSON response body to <typeparamref name="T"/>.</summary>
    /// <inheritdoc cref="SendAsync{T}(HttpMethod, string, object?, CancellationToken)" path="/exception"/>
    protected Task<T> DeleteAsync<T>(string path, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Delete, path, null, cancellationToken);

    /// <summary>
    /// Core HTTP dispatch: builds the URI from <see cref="Port"/>, attaches the
    /// auth headers from <see cref="ApiKey"/> and/or <see cref="BearerToken"/>, optionally serializes
    /// <paramref name="body"/> as JSON, sends the request, and deserializes the response.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="ApiKey"/> nor <see cref="BearerToken"/> is set.</exception>
    /// <exception cref="McpValidationException">HTTP 400 Bad Request.</exception>
    /// <exception cref="McpUnauthorizedException">HTTP 401 Unauthorized.</exception>
    /// <exception cref="McpNotFoundException">HTTP 404 Not Found.</exception>
    /// <exception cref="McpConflictException">HTTP 409 Conflict.</exception>
    /// <exception cref="McpServerException">Any other non-success HTTP status.</exception>
    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        if (RequireBearerToken && string.IsNullOrWhiteSpace(BearerToken))
            throw new InvalidOperationException(
                "BearerToken is required but not set. The client was configured with a JWT token " +
                "and must not fall back to API key authentication. Re-authenticate via OIDC first.");

        if (string.IsNullOrWhiteSpace(ApiKey) && string.IsNullOrWhiteSpace(BearerToken))
            throw new InvalidOperationException(
                "ApiKey or BearerToken must be set before calling an endpoint. " +
                "Read the workspace token from the AGENTS-README-FIRST.yaml marker file or authenticate with OIDC.");

        var uri = new Uri($"{_scheme}://{_host}:{Port}/{path.TrimStart('/')}");
        using var request = new HttpRequestMessage(method, uri);

        // JWT and API key are mutually exclusive auth mechanisms.
        // When a Bearer token is present, it is the sole auth header — API keys are
        // an agent-only convenience and must not be sent alongside a JWT.
        var authMode = "none";
        if (!string.IsNullOrWhiteSpace(BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
            authMode = "Bearer";
        }
        else if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);
            authMode = $"ApiKey({ApiKey.Substring(0, Math.Min(8, ApiKey.Length))}…)";
        }

        if (!string.IsNullOrWhiteSpace(WorkspacePath))
            request.Headers.TryAddWithoutValidation("X-Workspace-Path", WorkspacePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        System.Diagnostics.Trace.TraceInformation(
            $"[McpClient] {method} {uri} | Auth={authMode} | WorkspacePath={WorkspacePath ?? "(none)"}");

        if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, s_jsonOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                $"[McpClient] NETWORK ERROR {method} {uri}: {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        using (response)
        {
            System.Diagnostics.Trace.TraceInformation(
                $"[McpClient] {method} {uri} → HTTP {(int)response.StatusCode}");
            return await ReadResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends a GET request for an SSE (Server-Sent Events) endpoint and yields each
    /// <c>data:</c> line as a string. The stream terminates when the server sends an
    /// <c>event: done</c> message or closes the connection.
    /// </summary>
    /// <param name="path">Relative API path (e.g. <c>mcp/todo/{id}/prompt/status</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of text lines from the SSE response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="ApiKey"/> nor <see cref="BearerToken"/> is set.</exception>
    /// <exception cref="McpServerException">Any non-success HTTP status.</exception>
    protected async IAsyncEnumerable<string> StreamSseAsync(
        string path, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (RequireBearerToken && string.IsNullOrWhiteSpace(BearerToken))
            throw new InvalidOperationException(
                "BearerToken is required but not set. The client was configured with a JWT token " +
                "and must not fall back to API key authentication. Re-authenticate via OIDC first.");

        if (string.IsNullOrWhiteSpace(ApiKey) && string.IsNullOrWhiteSpace(BearerToken))
            throw new InvalidOperationException(
                "ApiKey or BearerToken must be set before calling an endpoint. " +
                "Read the workspace token from the AGENTS-README-FIRST.yaml marker file or authenticate with OIDC.");

        var uri = new Uri($"{_scheme}://{_host}:{Port}/{path.TrimStart('/')}");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrWhiteSpace(BearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
        else if (!string.IsNullOrWhiteSpace(ApiKey))
            request.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);
        if (!string.IsNullOrWhiteSpace(WorkspacePath))
            request.Headers.TryAddWithoutValidation("X-Workspace-Path", WorkspacePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _http.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(
#if !NETSTANDARD2_0
                cancellationToken
#endif
            ).ConfigureAwait(false);
            ThrowForStatus(response.StatusCode, body);
        }

#if NETSTANDARD2_0
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!cancellationToken.IsCancellationRequested)
        {
#if NETSTANDARD2_0
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
#else
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#endif
            if (line is null) break; // stream closed

            if (line.StartsWith("event: done", StringComparison.Ordinal))
                break;

            if (line.StartsWith("data: ", StringComparison.Ordinal))
                yield return line.Substring(6);
        }
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(
#if !NETSTANDARD2_0
            cancellationToken
#endif
        ).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            ThrowForStatus(response.StatusCode, content);

        return JsonSerializer.Deserialize<T>(content, s_jsonOptions)
            ?? throw new McpServerException("Response deserialized to null.", (int)response.StatusCode);
    }

    private static void ThrowForStatus(HttpStatusCode statusCode, string content)
    {
        var message = TryExtractError(content) ?? $"HTTP {(int)statusCode}: {content}";
        throw statusCode switch
        {
            HttpStatusCode.BadRequest => new McpValidationException(message),
            HttpStatusCode.Unauthorized => new McpUnauthorizedException(message),
            HttpStatusCode.NotFound => new McpNotFoundException(message),
            HttpStatusCode.Conflict => new McpConflictException(message),
            _ => new McpServerException(message, (int)statusCode),
        };
    }

    private static string? TryExtractError(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString();
            if (doc.RootElement.TryGetProperty("errorMessage", out var errMsg))
                return errMsg.GetString();
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
            // Not JSON — use raw content.
        }
        return null;
    }
}
