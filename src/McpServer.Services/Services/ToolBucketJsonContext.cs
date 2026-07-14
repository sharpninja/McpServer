using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
[JsonSerializable(typeof(JsonElement[]))]
[JsonSerializable(typeof(ToolBucketService.ToolManifestFile))]
internal sealed partial class ToolBucketJsonContext : JsonSerializerContext;