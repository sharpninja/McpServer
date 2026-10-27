using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBrainAi.Support.Mcp.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(List<LegacyTask>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<LegacyCompletedGroup>))]
internal sealed partial class LegacyTodoSqliteJsonContext : JsonSerializerContext;
