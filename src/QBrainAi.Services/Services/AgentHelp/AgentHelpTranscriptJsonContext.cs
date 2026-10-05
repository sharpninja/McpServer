using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBrainAi.Support.Mcp.Services.AgentHelp;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = false)]
[JsonSerializable(typeof(AgentHelpTranscriptEntry))]
internal sealed partial class AgentHelpTranscriptJsonContext : JsonSerializerContext;
