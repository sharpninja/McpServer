using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(GitHubTokenStoreDocument))]
internal sealed partial class GitHubTokenStoreJsonContext : JsonSerializerContext;
