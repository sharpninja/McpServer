using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Client;

/// <summary>Base class for MCP sub-clients providing shared HTTP helpers.</summary>
public abstract class McpClientBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly McpServerClientOptions _options;

    /// <summary>Initializes a new instance of the sub-client.</summary>
    protected McpClientBase(HttpClient http, McpServerClientOptions options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Builds an absolute URI from a relative path.</summary>
    protected Uri BuildUri(string relativePath)
    {
        var baseUrl = _options.BaseUrl.ToString().TrimEnd('/');
        return new Uri($"{baseUrl}/{relativePath.TrimStart('/')}");
    }

    /// <summary>Sends a GET request and deserializes the response.</summary>
    protected async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path));
        ApplyHeaders(request);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a POST request with a JSON body and deserializes the response.</summary>
    protected async Task<T> PostAsync<T>(string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(path));
        ApplyHeaders(request);
        if (body is not null)
            request.Content = CreateJsonContent(body);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a PUT request with a JSON body and deserializes the response.</summary>
    protected async Task<T> PutAsync<T>(string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, BuildUri(path));
        ApplyHeaders(request);
        if (body is not null)
            request.Content = CreateJsonContent(body);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a DELETE request and deserializes the response.</summary>
    protected async Task<T> DeleteAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, BuildUri(path));
        ApplyHeaders(request);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static StringContent CreateJsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
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

        return JsonSerializer.Deserialize<T>(content, JsonOptions)
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
