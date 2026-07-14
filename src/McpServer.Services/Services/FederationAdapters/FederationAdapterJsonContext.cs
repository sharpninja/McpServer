using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Services.FederationAdapters;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WorkspaceDto))]
[JsonSerializable(typeof(TodoCreateRequest))]
[JsonSerializable(typeof(TodoUpdateRequest))]
[JsonSerializable(typeof(TodoFlatTask))]
[JsonSerializable(typeof(List<TodoFlatTask>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(TodoFederationStateAdapter.TodoSnapshotPayload))]
[JsonSerializable(typeof(MemoryItem))]
[JsonSerializable(typeof(MemoryFederationStateAdapter.MemoryApplyPayload))]
[JsonSerializable(typeof(UnifiedSessionLogDto))]
[JsonSerializable(typeof(SessionLogFederationStateAdapter.SessionLogSnapshotPayload))]
[JsonSerializable(typeof(RequirementsFederationStateAdapter.RequirementsPayload))]
[JsonSerializable(typeof(ToolsBucketsFederationStateAdapter.ToolsBucketsPayload))]
[JsonSerializable(typeof(AgentsFederationStateAdapter.AgentsPayload))]
[JsonSerializable(typeof(LocalOnlyFederationPayload))]
internal sealed partial class FederationAdapterJsonContext : JsonSerializerContext;