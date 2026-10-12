using System.Text.Json.Serialization;

namespace QBrainAi.Support.Mcp.Storage.Indexing;

[JsonSerializable(typeof(List<ChunkIdMapping>))]
internal sealed partial class VectorIndexJsonContext : JsonSerializerContext;