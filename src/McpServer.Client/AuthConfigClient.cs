using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for public auth configuration endpoint (<c>/auth/config</c>).
/// This endpoint is unauthenticated, so requests bypass the base class auth check.
/// </summary>
public sealed class AuthConfigClient : McpClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = McpClientJsonContext.Default };

    private readonly HttpClient _http;
    private readonly string _scheme;
    private readonly string _host;

    /// <inheritdoc />
    public AuthConfigClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
        _http = http;
        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
    }

    internal AuthConfigClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
        _http = http;
        _scheme = options.BaseUrl.Scheme;
        _host = options.BaseUrl.Host;
    }

    /// <summary>Gets public OIDC configuration metadata. No authentication required.</summary>
    public async Task<AuthConfigResponse> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"{_scheme}://{_host}:{Port}/auth/config");
        using var response = await _http.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return (AuthConfigResponse)JsonSerializer.Deserialize(json, s_jsonOptions.GetTypeInfo(typeof(AuthConfigResponse)))!;
    }

    /// <summary>
    /// Requests an OAuth device authorization code through <c>POST /auth/device</c>.
    /// No authentication is required because this is part of auth bootstrap.
    /// </summary>
    /// <param name="request">Device authorization form values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Device authorization response from the configured provider.</returns>
    public async Task<AuthDeviceAuthorizationResponse> RequestDeviceAuthorizationAsync(
        AuthDeviceAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fields = new List<KeyValuePair<string, string?>>();
        if (request.AdditionalParameters is not null)
            fields.AddRange(request.AdditionalParameters);
        fields.Add(new("client_id", request.ClientId));
        fields.Add(new("scope", request.Scope));

        return await PostFormAsync<AuthDeviceAuthorizationResponse>("auth/device", fields, cancellationToken);
    }

    /// <summary>
    /// Requests OAuth tokens through <c>POST /auth/token</c>.
    /// No authentication is required because this is part of auth bootstrap.
    /// </summary>
    /// <param name="request">Token request form values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response from the configured provider.</returns>
    public async Task<AuthTokenResponse> RequestTokenAsync(
        AuthTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fields = new List<KeyValuePair<string, string?>>();
        if (request.AdditionalParameters is not null)
            fields.AddRange(request.AdditionalParameters);
        fields.Add(new("grant_type", request.GrantType));
        fields.Add(new("client_id", request.ClientId));
        fields.Add(new("device_code", request.DeviceCode));
        fields.Add(new("code", request.Code));
        fields.Add(new("redirect_uri", request.RedirectUri));
        fields.Add(new("code_verifier", request.CodeVerifier));
        fields.Add(new("refresh_token", request.RefreshToken));
        fields.Add(new("scope", request.Scope));

        return await PostFormAsync<AuthTokenResponse>("auth/token", fields, cancellationToken);
    }

    private async Task<T> PostFormAsync<T>(
        string path,
        IEnumerable<KeyValuePair<string, string?>> fields,
        CancellationToken cancellationToken)
    {
        var uri = new Uri($"{_scheme}://{_host}:{Port}/{path.TrimStart('/')}");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(RemoveBlankFields(fields));

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(true);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);

        if (!response.IsSuccessStatusCode)
        {
            throw new McpServerException(
                $"HTTP {(int)response.StatusCode}: {content}",
                (int)response.StatusCode);
        }

        return (T)(JsonSerializer.Deserialize(content, s_jsonOptions.GetTypeInfo(typeof(T)))
            ?? throw new McpServerException("Response deserialized to null.", (int)response.StatusCode));
    }

    private static IEnumerable<KeyValuePair<string, string>> RemoveBlankFields(IEnumerable<KeyValuePair<string, string?>> fields)
    {
        foreach (var field in fields)
        {
            if (!string.IsNullOrWhiteSpace(field.Value))
                yield return new KeyValuePair<string, string>(field.Key, field.Value!);
        }
    }
}
