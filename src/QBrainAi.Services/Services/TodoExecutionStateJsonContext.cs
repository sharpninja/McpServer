using System.Text.Json;
using System.Text.Json.Serialization;
using QBrainAi.Support.Mcp.Models;

namespace QBrainAi.Support.Mcp.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
[JsonSerializable(typeof(TodoExecutionStateDocument))]
internal sealed partial class TodoExecutionStateJsonContext : JsonSerializerContext;
