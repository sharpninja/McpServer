using System.Text.Json.Serialization;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Standardized client-visible error payload for unexpected HTTP server failures.
/// </summary>
public sealed class HttpErrorResponse
{
    /// <summary>
    /// HTTP status code.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    /// Stable error code/category.
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable summary.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Detailed but sanitized failure description.
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// Failing operation or route context.
    /// </summary>
    [JsonPropertyName("operation")]
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Request trace/correlation identifier.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp for the error payload.
    /// </summary>
    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; init; }
}
