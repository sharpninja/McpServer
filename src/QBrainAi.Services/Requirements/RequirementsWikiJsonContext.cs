using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBrainAi.Support.Mcp.Requirements;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(RequirementsWikiManifest))]
internal sealed partial class RequirementsWikiJsonContext : JsonSerializerContext;

internal sealed record RequirementsWikiManifest(
    string Schema,
    string Platform,
    DateTimeOffset GeneratedAtUtc,
    string[] Documents,
    // 2026-11-26, TR-MCP-WIKIEXPORT-001 ac-WikiGroupRoundTrip and ac-WikiFooterRoundTrip:
    // identify derived pages and inline footer ownership without changing legacy manifests.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string[]? GroupDocuments = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool InlineFooter = false);
