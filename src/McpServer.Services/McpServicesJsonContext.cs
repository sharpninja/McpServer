using System.Collections.Generic;
using System.Text.Json;
using McpServer.Support.Mcp.Storage.Entities;
using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<TodoFlatTask>))]
[JsonSerializable(typeof(List<WorkspaceConfigEntry>))]
[JsonSerializable(typeof(TodoItemEntity))]
[JsonSerializable(typeof(TodoFlatItem))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(TriageReportDetail))]
[JsonSerializable(typeof(TriageGroupDetail))]
[JsonSerializable(typeof(TriageService.ResearchOutput))]
[JsonSerializable(typeof(FederationEnrollmentRequest))]
[JsonSerializable(typeof(FederationEnrollmentResponse))]
[JsonSerializable(typeof(FederationHeartbeatRequest))]
[JsonSerializable(typeof(FederationWorkspaceRegistrationRequest))]
[JsonSerializable(typeof(FederationLocalProxyMetadataPayload))]
[JsonSerializable(typeof(FederationWorkspaceMetadataPayload))]
[JsonSerializable(typeof(AgentLaunchEventDetails))]
[JsonSerializable(typeof(AgentExitEventDetails))]
[JsonSerializable(typeof(CopilotDirectiveDto))]
[JsonSerializable(typeof(FederationConnectionResult))]
[JsonSerializable(typeof(List<FederationSyncItem>))]
[JsonSerializable(typeof(FederationLocalExecutionRequest))]
[JsonSerializable(typeof(FederationSyncAckRequest))]
[JsonSerializable(typeof(FederationQueuedOperationAcceptedResponse))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSerializable(typeof(FederationExecutionEnvelope))]
[JsonSerializable(typeof(FederationOperationRequest))]
[JsonSerializable(typeof(FederationOperationResponse))]
internal sealed partial class McpServicesJsonContext : JsonSerializerContext;