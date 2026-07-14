using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services.AgentHelp;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(AgentHelpIncidentRecord))]
internal sealed partial class AgentHelpJsonContext : JsonSerializerContext;