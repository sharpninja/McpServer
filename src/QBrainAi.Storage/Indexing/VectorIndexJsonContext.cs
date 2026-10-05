using System.Text.Json.Serialization;

namespace QBrainAi.Support.Mcp.Indexing;

[JsonSerializable(typeof(List<ChunkIdMapping>))]
internal sealed partial class VectorIndexJsonContext : JsonSerializerContext;