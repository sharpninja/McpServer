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
using Microsoft.Extensions.Logging;

namespace McpServer.Client;

/// <summary>
/// Shared mutable container for the workspace path. A single instance is shared across
/// all <see cref="McpClientBase"/> sub-clients created from the same options, so updating
/// the path once is instantly visible to every sub-client at the next request.
/// </summary>
internal sealed class WorkspacePathHolder
{
    public volatile string Path = string.Empty;
}

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
    private static readonly JsonSerializerOptions s_jsonOptionsIncludingNulls = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private readonly HttpClient _http;
    private readonly string _scheme;
    private readonly string _host;
    internal readonly WorkspacePathHolder _workspacePathHolder;
    private readonly ILogger? _logger;

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
        : this(http, options, null)
    {
    }

    /// <summary>
    /// Internal constructor that accepts a shared <see cref="WorkspacePathHolder"/> so all
    /// sub-clients created from the same <see cref="McpServerClient"/> read from a single
    /// workspace-path source of truth.
    /// </summary>
    internal McpClientBase(HttpClient http, McpServerClientOptions options, WorkspacePathHolder? sharedHolder)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        if (options is null) throw new ArgumentNullException(nameof(options));

        _logger = options.LoggerFactory?.CreateLogger(GetType().Name);

        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("McpServer");

        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
        Port = options.BaseUrl.Port;

        _workspacePathHolder = sharedHolder ?? new WorkspacePathHolder();
        _workspacePathHolder.Path = options.WorkspacePath ?? string.Empty;

        // Set ApiKey first, then BearerToken — BearerToken setter clears ApiKey
        // when a JWT is provided, enforcing mutual exclusivity from construction.
        ApiKey = options.ApiKey ?? string.Empty;
        BearerToken = options.BearerToken ?? string.Empty;
        _credentialDiagnostic = options.CredentialDiagnostic;
    }

    private readonly string? _credentialDiagnostic;

    /// <summary>
    /// API key for workspace authentication, sent as the <c>X-Api-Key</c> header on every
    /// request. <b>Mutually exclusive with <see cref="BearerToken"/>.</b> Setting a non-empty
    /// API key clears any bearer token and disables bearer-only enforcement so callers can
    /// intentionally rotate from interactive user auth back to agent-style marker auth.
    ///
    /// <para>Obtain the key from the <c>AGENTS-README-FIRST.yaml</c> marker file that the
    /// MCP Server writes to each workspace root on startup.</para>
    /// </summary>
    public string ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                _bearerToken = string.Empty;
                RequireBearerToken = false;
            }
        }
    }
    private string _apiKey = string.Empty;

    /// <summary>
    /// JWT bearer token sent as the <c>Authorization: Bearer</c> header on every request.
    /// <b>Mutually exclusive with <see cref="ApiKey"/>.</b> Setting this to a non-empty value
    /// clears the API key and enables <see cref="RequireBearerToken"/> for the active auth mode.
    /// Clearing the bearer token also clears bearer-only enforcement so callers can later switch
    /// to marker-file API-key authentication without rebuilding the client.
    /// </summary>
    public string BearerToken
    {
        get => _bearerToken;
        set
        {
            _bearerToken = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_bearerToken))
            {
                _apiKey = string.Empty;
                RequireBearerToken = true;
            }
            else
            {
                RequireBearerToken = false;
            }
        }
    }
    private string _bearerToken = string.Empty;

    /// <summary>
    /// Clears both API key and bearer token, resetting the client to an unauthenticated
    /// state. After calling this method, a new API key or bearer token can be set.
    /// <see cref="RequireBearerToken"/> is also reset.
    /// </summary>
    public void Logout()
    {
        _apiKey = string.Empty;
        _bearerToken = string.Empty;
        RequireBearerToken = false;
    }

    /// <summary>
    /// When <see langword="true"/>, every outbound request <b>must</b> carry a Bearer token.
    /// An <see cref="InvalidOperationException"/> is thrown if <see cref="BearerToken"/> is
    /// empty at call time — the client will never silently fall back to an API key.
    /// This flag is set automatically the first time <see cref="BearerToken"/> is assigned
    /// a non-empty value.
    /// </summary>
    public bool RequireBearerToken { get; set; }

    /// <summary>
    /// Workspace path sent as the <c>X-Workspace-Path</c> header on every request.
    /// Backed by a shared <see cref="WorkspacePathHolder"/> so all sub-clients created
    /// from the same <see cref="McpServerClient"/> read and write the same value.
    /// </summary>
    public string WorkspacePath
    {
        get => _workspacePathHolder.Path;
        set => _workspacePathHolder.Path = value ?? string.Empty;
    }

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

    /// <summary>Sends a PATCH request with a JSON body and deserializes the response to <typeparamref name="T"/>.</summary>
    /// <inheritdoc cref="SendAsync{T}(HttpMethod, string, object?, CancellationToken)" path="/exception"/>
    protected Task<T> PatchAsync<T>(string path, object? body, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Patch, path, body, cancellationToken);

    /// <summary>
    /// Sends a PATCH request while preserving explicit <see langword="null"/> values in the JSON body.
    /// Use this for dictionary patch endpoints where <see langword="null"/> removes a value.
    /// </summary>
    /// <inheritdoc cref="SendAsync{T}(HttpMethod, string, object?, CancellationToken)" path="/exception"/>
    protected Task<T> PatchIncludingNullsAsync<T>(string path, object? body, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Patch, path, body, s_jsonOptionsIncludingNulls, cancellationToken);

    /// <summary>Sends a DELETE request and deserializes the JSON response body to <typeparamref name="T"/>.</summary>
    /// <inheritdoc cref="SendAsync{T}(HttpMethod, string, object?, CancellationToken)" path="/exception"/>
    protected Task<T> DeleteAsync<T>(string path, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Delete, path, null, cancellationToken);

    /// <summary>
    /// Sends an HTTP request and returns the raw successful response message.
    /// Callers must dispose the returned response.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Relative API path.</param>
    /// <param name="body">Optional JSON body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful HTTP response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="ApiKey"/> nor <see cref="BearerToken"/> is set.</exception>
    /// <exception cref="McpValidationException">HTTP 400 Bad Request.</exception>
    /// <exception cref="McpUnauthorizedException">HTTP 401 Unauthorized.</exception>
    /// <exception cref="McpNotFoundException">HTTP 404 Not Found.</exception>
    /// <exception cref="McpConflictException">HTTP 409 Conflict.</exception>
    /// <exception cref="McpServerException">Any other non-success HTTP status.</exception>
    protected Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
        => SendRawAsync(method, path, body, HttpCompletionOption.ResponseContentRead, null, cancellationToken);

    /// <summary>
    /// Sends an HTTP request and returns the raw successful response message using the specified
    /// completion option and optional Accept header.
    /// </summary>
    protected async Task<HttpResponseMessage> SendRawAsync(
        HttpMethod method,
        string path,
        object? body,
        HttpCompletionOption completionOption,
        string? acceptMediaType,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

        var uri = new Uri($"{_scheme}://{_host}:{Port}/{path.TrimStart('/')}");
        using var request = new HttpRequestMessage(method, uri);

        if (!string.IsNullOrWhiteSpace(BearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
        else if (!string.IsNullOrWhiteSpace(ApiKey))
            request.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);

        if (!string.IsNullOrWhiteSpace(WorkspacePath))
            request.Headers.TryAddWithoutValidation("X-Workspace-Path", WorkspacePath);

        AppendCustomHeaders(request);

        if (!string.IsNullOrWhiteSpace(acceptMediaType))
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptMediaType));

        if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, s_jsonOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, completionOption, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[McpClient] NETWORK ERROR {Method} {Uri}", method, uri);
            throw;
        }

        if (response.IsSuccessStatusCode)
            return response;

        using (response)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
            ThrowForStatus(response.StatusCode, content);
        }

        throw new McpServerException("Unexpected HTTP failure.", 500);
    }

    /// <summary>
    /// Sends an HTTP request and returns only the successful status code.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Relative API path.</param>
    /// <param name="body">Optional JSON body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response status code.</returns>
    protected async Task<HttpStatusCode> SendForStatusAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendRawAsync(method, path, body, cancellationToken);
        return response.StatusCode;
    }

    /// <summary>
    /// Sends a GET request and returns binary response content and content type.
    /// </summary>
    /// <param name="path">Relative API path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response bytes and media type.</returns>
    protected async Task<(byte[] Content, string? ContentType)> GetBytesAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await SendRawAsync(HttpMethod.Get, path, null, cancellationToken).ConfigureAwait(true);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(true);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        return (bytes, mediaType);
    }

    /// <summary>
    /// Allows derived clients to append endpoint-specific headers after the shared
    /// authentication and workspace headers have been applied.
    /// </summary>
    /// <param name="request">The outbound request receiving any derived-client headers.</param>
    protected virtual void AppendCustomHeaders(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
    }

    /// <summary>
    /// Pre-flight auth check called before every outbound request. Throws with a
    /// descriptive message when no valid credential is available.
    /// </summary>
    private void EnsureAuthenticated()
    {
        var hasBearer = !string.IsNullOrWhiteSpace(BearerToken);
        var hasApiKey = !string.IsNullOrWhiteSpace(ApiKey);

        if (RequireBearerToken && !hasBearer)
            throw new InvalidOperationException(
                "Authentication failed: bearer-token authentication is currently required for this client, " +
                "but no bearer token is configured. Re-authenticate via OIDC or switch the client to API-key authentication.");

        if (!hasBearer && !hasApiKey)
        {
            var baseMessage = "Authentication required: no credential is configured on this client. " +
                "Set BearerToken (for interactive users via OIDC) or ApiKey (for agents via " +
                "the AGENTS-README-FIRST.yaml marker file) before calling any endpoint.";
            if (!string.IsNullOrWhiteSpace(_credentialDiagnostic))
                baseMessage = baseMessage + " Credential resolution diagnostic: " + _credentialDiagnostic;
            throw new InvalidOperationException(baseMessage);
        }
    }

    /// <summary>
    /// Core HTTP dispatch: builds the URI from <see cref="Port"/>, attaches the
    /// auth header (<see cref="BearerToken"/> xor <see cref="ApiKey"/> — mutually exclusive),
    /// optionally serializes <paramref name="body"/> as JSON, sends the request, and deserializes the response.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="ApiKey"/> nor <see cref="BearerToken"/> is set.</exception>
    /// <exception cref="McpValidationException">HTTP 400 Bad Request.</exception>
    /// <exception cref="McpUnauthorizedException">HTTP 401 Unauthorized.</exception>
    /// <exception cref="McpNotFoundException">HTTP 404 Not Found.</exception>
    /// <exception cref="McpConflictException">HTTP 409 Conflict.</exception>
    /// <exception cref="McpServerException">Any other non-success HTTP status.</exception>
    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
        => await SendAsync<T>(method, path, body, s_jsonOptions, cancellationToken).ConfigureAwait(true);

    /// <summary>
    /// Core HTTP dispatch with a caller-specified request serializer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="ApiKey"/> nor <see cref="BearerToken"/> is set.</exception>
    /// <exception cref="McpValidationException">HTTP 400 Bad Request.</exception>
    /// <exception cref="McpUnauthorizedException">HTTP 401 Unauthorized.</exception>
    /// <exception cref="McpNotFoundException">HTTP 404 Not Found.</exception>
    /// <exception cref="McpConflictException">HTTP 409 Conflict.</exception>
    /// <exception cref="McpServerException">Any other non-success HTTP status.</exception>
    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        JsonSerializerOptions requestSerializerOptions,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

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
        AppendCustomHeaders(request);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _logger?.LogInformation("[McpClient] {Method} {Uri} | Auth={AuthMode} | WorkspacePath={WorkspacePath}",
            method, uri, authMode, WorkspacePath ?? "(none)");

        if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, requestSerializerOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[McpClient] NETWORK ERROR {Method} {Uri}", method, uri);
            throw;
        }

        using (response)
        {
            _logger?.LogInformation("[McpClient] {Method} {Uri} → HTTP {StatusCode}",
                method, uri, (int)response.StatusCode);
            return await ReadResponseAsync<T>(response, cancellationToken);
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
        EnsureAuthenticated();

        var uri = new Uri($"{_scheme}://{_host}:{Port}/{path.TrimStart('/')}");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrWhiteSpace(BearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
        else if (!string.IsNullOrWhiteSpace(ApiKey))
            request.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);
        if (!string.IsNullOrWhiteSpace(WorkspacePath))
            request.Headers.TryAddWithoutValidation("X-Workspace-Path", WorkspacePath);
        AppendCustomHeaders(request);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _http.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
            ThrowForStatus(response.StatusCode, body);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(true);
            if (line is null) break; // stream closed

            if (line.StartsWith("event: done", StringComparison.Ordinal))
                break;

            if (line.StartsWith("data: ", StringComparison.Ordinal))
                yield return line.Substring(6);
        }
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);

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
        catch (JsonException)
        {
            // Not JSON — use raw content.
        }
        return null;
    }
}
