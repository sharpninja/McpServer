using System.Text.Json;
using System.Text.Json.Serialization;
using QBrainAi.Support.Mcp.Models;

namespace QBrainAi.Support.Mcp.Ingestion;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(UnifiedSessionLogDto))]
[JsonSerializable(typeof(CopilotStatisticsDto))]
[JsonSerializable(typeof(List<UnifiedRequestEntryDto>))]
internal sealed partial class SessionLogIngestionJsonContext : JsonSerializerContext;