using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Indexing;

[JsonSerializable(typeof(List<ChunkIdMapping>))]
internal sealed partial class VectorIndexJsonContext : JsonSerializerContext;