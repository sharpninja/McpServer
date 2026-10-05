using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.Services.FederationAdapters;

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