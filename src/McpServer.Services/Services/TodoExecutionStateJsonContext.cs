using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
[JsonSerializable(typeof(TodoExecutionStateDocument))]
internal sealed partial class TodoExecutionStateJsonContext : JsonSerializerContext;
