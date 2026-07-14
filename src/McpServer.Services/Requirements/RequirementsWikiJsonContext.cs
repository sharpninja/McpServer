using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Requirements;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(RequirementsWikiManifest))]
internal sealed partial class RequirementsWikiJsonContext : JsonSerializerContext;

internal sealed record RequirementsWikiManifest(
    string Schema,
    string Platform,
    DateTimeOffset GeneratedAtUtc,
    string[] Documents);
