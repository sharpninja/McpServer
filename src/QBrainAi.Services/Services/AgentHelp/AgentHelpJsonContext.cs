using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBrainAi.Support.Mcp.Services.AgentHelp;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(AgentHelpIncidentRecord))]
internal sealed partial class AgentHelpJsonContext : JsonSerializerContext;