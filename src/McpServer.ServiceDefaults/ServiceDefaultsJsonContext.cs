using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.Hosting;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(HttpErrorResponse))]
[JsonSerializable(typeof(HealthCheckResponse))]
internal sealed partial class ServiceDefaultsJsonContext : JsonSerializerContext;

internal sealed record HealthCheckResponse(
    string Status,
    string Version,
    HealthCheckEntryResponse[] Checks,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Nonce = null);

internal sealed record HealthCheckEntryResponse(
    string Name,
    string Status,
    string? Description,
    double Duration,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Exception = null);
