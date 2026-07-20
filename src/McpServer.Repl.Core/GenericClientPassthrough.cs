// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Generic client passthrough operations
// FR-MCP-REPL-003: Command Namespace Parity - Workspace and context operation forwarding
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Client passthrough implementation
// TEST-MCP-REPL-010: Workspace management REPL commands match REST endpoints

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Generic client passthrough implementation
// FR-MCP-REPL-003: Command Namespace Parity - Client operation forwarding implementation
// FR-MCP-REPL-005: Orchestration State Visibility - State query implementation
// TR-MCP-REPL-004: Command Registry and Dispatcher - Generic passthrough handler
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Client passthrough delegation
// TR-MCP-REPL-007: State Query Commands - Client passthrough dynamic binding
// TEST-MCP-REPL-008: Context REPL operations match REST endpoints
// TEST-MCP-REPL-011: Generic client passthrough delegates to correct client methods

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Production implementation of <see cref="IGenericClientPassthrough"/> that uses reflection to
/// dynamically invoke methods on <see cref="McpServerClient"/> sub-clients.
/// </summary>
/// <remarks>
/// This implementation resolves client properties by name (case-insensitive), resolves methods by name,
/// coerces YAML dictionary arguments to method parameter types using <see cref="System.Text.Json"/>,
/// invokes methods via reflection, and returns serialized results.
/// </remarks>
public sealed class GenericClientPassthrough : IGenericClientPassthrough
{
    private readonly McpServerClient _client;
    private readonly IClientMutationPolicy? _clientMutationPolicy;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new FlexibleBooleanJsonConverter() },
        TypeInfoResolver = McpClientJsonContext.Default,
    };

    /// <summary>
    /// Initializes a new instance of <see cref="GenericClientPassthrough"/> with the specified client.
    /// </summary>
    /// <param name="client">The MCP server client containing all sub-clients.</param>
    /// <param name="clientMutationPolicy">Optional mutation policy used before reflection invocation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is null.</exception>
    public GenericClientPassthrough(McpServerClient client, IClientMutationPolicy? clientMutationPolicy = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _clientMutationPolicy = clientMutationPolicy;
    }

    /// <inheritdoc />
    public async Task<object?> InvokeAsync(
        string clientName,
        string methodName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(methodName);
        ArgumentNullException.ThrowIfNull(arguments);

        var decision = _clientMutationPolicy?.Evaluate(clientName, methodName, arguments) ?? ClientMutationPolicyDecision.Allow();
        if (!decision.Allowed)
            throw new ClientMutationPolicyException(clientName, methodName, decision);

        // Step 1: Resolve client by name (case-insensitive)
        var clientProperty = ResolveClientProperty(clientName);
        var clientInstance = clientProperty.GetValue(_client);

        if (clientInstance is null)
        {
            throw new InvalidOperationException(
                $"Client property '{clientName}' resolved to null. Valid clients: {GetValidClientNames()}");
        }

        // Step 2: Resolve method by name
        var method = ResolveMethod(GetPreservedClientType(clientProperty.Name), methodName);

        // Step 3: Bind arguments to method parameters
        var parameters = method.GetParameters();
        var boundArgs = BindArguments(parameters, arguments, cancellationToken);

        // Step 4: Invoke method via reflection
        try
        {
            var result = method.Invoke(clientInstance, boundArgs);
            // Step 5: Await if the result is a Task
            if (result is Task task)
            {
                return await AwaitTaskResultAsync(task, method.ReturnType).ConfigureAwait(false);
            }

            return result;
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException(
                $"Method invocation failed for {clientName}.{methodName}: {ex.InnerException?.Message ?? ex.Message}",
                ex.InnerException ?? ex);
        }
    }

    /// <summary>
    /// Resolves a client property from <see cref="McpServerClient"/> by name (case-insensitive).
    /// </summary>
    private PropertyInfo ResolveClientProperty(string clientName)
    {
        var clientType = typeof(McpServerClient);
        var properties = clientType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var match = properties.FirstOrDefault(p =>
            string.Equals(p.Name, clientName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new InvalidOperationException(
                $"Unknown client: {clientName}. Valid clients: {GetValidClientNames()}");
        }

        return match;
    }
    /// <summary>
    /// Resolves a statically preserved client type for trim-safe method reflection.
    /// </summary>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type GetPreservedClientType(string clientName)
    {
        return clientName.ToUpperInvariant() switch
        {
            "TODO" => typeof(TodoClient),
            "CONTEXT" => typeof(ContextClient),
            "GRAPHRAG" => typeof(GraphRagClient),
            "SESSIONLOG" => typeof(SessionLogClient),
            "MEMORY" => typeof(MemoryClient),
            "GITHUB" => typeof(GitHubClient),
            "REQUIREMENTS" => typeof(RequirementsClient),
            "VOICE" => typeof(VoiceClient),
            "EVENTS" => typeof(EventStreamClient),
            "REPO" => typeof(RepoClient),
            "DESKTOP" => typeof(DesktopClient),
            "TUNNEL" => typeof(TunnelClient),
            "WORKSPACE" => typeof(WorkspaceClient),
            "CONFIGURATION" => typeof(ConfigurationClient),
            "TOOLS" => typeof(ToolRegistryClient),
            "AUTHCONFIG" => typeof(AuthConfigClient),
            "DIAGNOSTIC" => typeof(DiagnosticClient),
            "TEMPLATE" => typeof(TemplateClient),
            "AGENTPOOL" => typeof(AgentPoolClient),
            "AGENT" => typeof(AgentClient),
            "HEALTH" => typeof(HealthClient),
            "FEDERATION" => typeof(FederationClient),
            "KEYSERVER" => typeof(KeyServerClient),
            "SUBSCRIBER" => typeof(SubscriberClient),
            "TURNTRANSACTIONS" => typeof(TurnTransactionsClient),
            "TRIAGE" => typeof(TriageClient),
            "AGENTHELP" => typeof(AgentHelpClient),
            _ => throw new InvalidOperationException(
                $"Unknown client: {clientName}. Valid clients: {GetValidClientNames()}"),
        };
    }

    /// <summary>
    /// Resolves a public async method on the client by name.
    /// </summary>
    private MethodInfo ResolveMethod(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type clientType,
        string methodName)
    {
        var methods = clientType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Try case-sensitive first
        var match = methods.FirstOrDefault(m => m.Name == methodName);

        // Fall back to case-insensitive
        if (match is null)
        {
            match = methods.FirstOrDefault(m =>
                string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null)
        {
            var validMethods = string.Join(", ", methods
                .Where(m => m.ReturnType.IsGenericType &&
                            m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                .Select(m => m.Name)
                .Distinct()
                .OrderBy(n => n));

            throw new InvalidOperationException(
                $"Unknown method: {methodName} on client: {clientType.Name}. Valid methods: {validMethods}");
        }

        return match;
    }

    private static async Task<object?> AwaitTaskResultAsync(Task task, Type taskType)
    {
        switch (task)
        {
            case Task<ActiveTodoContext> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ActiveTodoResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AdbStepResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentDefinition> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentDefinitionListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentEventListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentHelpSessionCreateResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentHelpSessionStatusDto> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentHelpTranscriptResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentHelpTurnResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentPoolConnectResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentPoolEnqueueResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentPoolMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentPoolPromptResolutionResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentProcessInfo> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentRunningListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentSeedDefaultsResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentValidateResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentWorkspaceConfig> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AgentWorkspaceListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AppendTodoCheckpointResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AuthConfigResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AuthDeviceAuthorizationResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<AuthTokenResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<bool> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<BucketBrowseResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<BucketListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<BucketMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<BucketSyncResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ContextPack> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ContextSearchResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ContextSourcesResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<CreateIterationPhaseResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<CreateTodosFromPlanResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<DesktopLaunchResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<DiagnosticAppSettingsPathResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<DiagnosticExecutionPathResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<DialogAppendResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<Dictionary<string, string>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<DiffgramCommitResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<EffectiveRequirementsResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationConflictInfo> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationConnectionInfo> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationEnrollmentResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationHeartbeatResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationOperationResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationPushResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationQueueStatusResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationStatusResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationTargetInfo> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FederationWorkspaceInfo> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FrEntry> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<FrTrMapping> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubAuthorizeUrlResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubAuthStatusResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubCreateIssueResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubIssueDetail> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubIssueListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubLabelsResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubOAuthConfigResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubOperationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubPullListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubWorkflowRunDetail> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GitHubWorkflowRunListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GlobalPromptResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphEntityListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphEntityResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRagDocumentChunksResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRagDocumentDeleteResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRagDocumentListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRagIngestTextResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRagQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRagStatusResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRelationshipListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<GraphRelationshipResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<HealthCheckResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<HttpStatusCode> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<int> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<AgentPoolAgentStatus>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<AgentPoolQueueItem>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<FrEntry>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<FrTrMapping>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<RequirementScopeLayer>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<TestEntry>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<TransactionPubSubMessageStatus>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IReadOnlyList<TrEntry>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<IssueSyncResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<LinkTodoToSessionTurnsResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<FederationConflictInfo>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<FederationProxyInfo>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<FederationStateAdapterCoverage>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<FederationSyncItem>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<FederationTargetInfo>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<FederationWorkspaceInfo>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<TunnelProviderInfo>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<List<WorkspaceRouteInfo>> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<MarkerFileTimestampResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<MarkerRegenerationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<MemoryItem> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<MemoryMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<MemoryQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<PartyKeyDescriptor> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<PartyRegistrationResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RebuildIndexResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RecordTodoValidationResultResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RepoEditResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RepoFileReadResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RepoListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RepoWriteResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RequirementsAnalysisResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RequirementsBatchResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RequirementScopeLayer> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RequirementsGeneratedDocument> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RequirementsIngestResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<RequirementsMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ServerStartupResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SessionLifecycleOpenResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SessionLogMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SessionLogQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SessionLogSubmitResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SessionLogTurnSubmitResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SessionLogWorkspaceStampRepairResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SetTodoTestPlanResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<SingleIssueSyncResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<string> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TemplateItem> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TemplateMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TemplateQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TemplateResolveResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TemplateTestResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TestEntry> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TodoAuditQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TodoDeltaContext> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TodoFlatItem> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TodoMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TodoProjectionRepairResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TodoProjectionStatusResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TodoQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ToolDto> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ToolMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<ToolSearchResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionAbortResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionManifestSignResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionManifestTraceRecord> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionManifestTraceReport> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionManifestVerifyResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionPubSubReplayResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionPubSubRetentionResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TransactionStatusResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TranscriptIngestRunResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TrEntry> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageCreatedTodoQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageDashboardResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageGroupDeleteResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageGroupDetail> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageGroupEditResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageGroupQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageReportDetail> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageReportSubmitResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageResearchRunDetail> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TriageRunQueryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TunnelDiscoveryResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TunnelProviderInfo> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<TurnTransactionStatusResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<UpdateTodoStatusResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<VoiceEscapeResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<VoiceInterruptResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<VoiceSessionCreateResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<VoiceSessionStatus> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<VoiceTranscriptResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<VoiceTurnResponse> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WebsiteIngestResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WorkspaceCurrentRequirementLayer> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WorkspaceDto> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WorkspaceInitResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WorkspaceListResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WorkspaceMutationResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WorkspacePolicyApplyResult> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            case Task<WorkspaceProcessStatus> typedTask:
                return await AwaitTypedTaskResultAsync(typedTask).ConfigureAwait(false);
            default:
                return await AwaitNonGenericTaskAsync(task, taskType).ConfigureAwait(false);
        }
    }

    private static async Task<object?> AwaitTypedTaskResultAsync<TResult>(Task<TResult> task)
    {
        return await task.ConfigureAwait(false);
    }

    private static async Task<object?> AwaitNonGenericTaskAsync(Task task, Type taskType)
    {
        await task.ConfigureAwait(false);
        if (taskType != typeof(Task))
        {
            throw new InvalidOperationException($"Unsupported task result type: {taskType.FullName ?? taskType.Name}");
        }

        return null;
    }

    /// <summary>
    /// Binds arguments from the dictionary to method parameters.
    /// </summary>
    private object?[] BindArguments(
        ParameterInfo[] parameters,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var boundArgs = new object?[parameters.Length];
        var nullabilityContext = new NullabilityInfoContext();

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            // CancellationToken is always passed from the method parameter
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                boundArgs[i] = cancellationToken;
                continue;
            }

            // Try to find matching argument (case-insensitive)
            var argKey = arguments.Keys.FirstOrDefault(k =>
                string.Equals(k, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (argKey is null)
            {
                // Check if parameter is optional
                if (parameter.HasDefaultValue)
                {
                    boundArgs[i] = parameter.DefaultValue;
                    continue;
                }

                // Required parameter missing
                throw new ArgumentException(
                    $"Missing required parameter: {parameter.Name} (type: {parameter.ParameterType.Name})");
            }

            var argValue = arguments[argKey];

            // Get nullability info for the parameter
            var nullabilityInfo = nullabilityContext.Create(parameter);

            // Coerce argument to target type
            boundArgs[i] = CoerceArgument(argValue, parameter.ParameterType, parameter.Name, nullabilityInfo);
        }

        return boundArgs;
    }

    /// <summary>
    /// Coerces an argument value to the target parameter type.
    /// </summary>
    private object? CoerceArgument(object? value, Type targetType, string? parameterName, NullabilityInfo nullabilityInfo)
    {
        // Handle null values
        if (value is null)
        {
            // Check if the parameter accepts null
            bool acceptsNull = nullabilityInfo.WriteState == NullabilityState.Nullable ||
                              (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is not null);

            if (!acceptsNull)
            {
                throw new ArgumentException(
                    $"Null value provided for non-nullable parameter: {parameterName} (type: {targetType.Name})");
            }

            return null;
        }

        // If already correct type, return as-is
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        // Unwrap nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle enums
        if (underlyingType.IsEnum)
        {
            try
            {
                if (value is string stringValue)
                {
                    return Enum.Parse(underlyingType, stringValue, ignoreCase: true);
                }

                return Enum.ToObject(underlyingType, value);
            }
            catch (Exception ex)
            {
                var validValues = string.Join(", ", Enum.GetNames(underlyingType));
                throw new ArgumentException(
                    $"Invalid enum value for parameter '{parameterName}': {value}. Valid values: {validValues}",
                    ex);
            }
        }

        // Handle primitive types
        if (underlyingType.IsPrimitive || underlyingType == typeof(string) ||
            underlyingType == typeof(decimal) || underlyingType == typeof(DateTime) ||
            underlyingType == typeof(DateTimeOffset) || underlyingType == typeof(Guid))
        {
            try
            {
                return Convert.ChangeType(value, underlyingType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Type conversion error for parameter '{parameterName}': cannot convert '{value}' to {underlyingType.Name}",
                    ex);
            }
        }

        // Handle collections
        if (IsCollectionType(underlyingType))
        {
            try
            {
                return CoerceCollection(value, underlyingType, parameterName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Collection conversion error for parameter '{parameterName}': {ex.Message}",
                    ex);
            }
        }

        // Handle complex objects via JSON serialization
        try
        {
            var json = SerializeWithConfiguredMetadata(NormalizeForJson(value));
            return JsonSerializer.Deserialize(json, _jsonOptions.GetTypeInfo(underlyingType));
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"JSON deserialization error for parameter '{parameterName}': cannot deserialize to {underlyingType.Name}. {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Coerces a value to a collection type.
    /// </summary>
    private object? CoerceCollection(object? value, Type targetType, string? parameterName)
    {
        var json = SerializeWithConfiguredMetadata(NormalizeForJson(value));
        return JsonSerializer.Deserialize(json, _jsonOptions.GetTypeInfo(targetType));
    }

    /// <summary>
    /// Serializes normalized YAML values through configured JSON metadata.
    /// </summary>
    private static string SerializeWithConfiguredMetadata(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        return JsonSerializer.Serialize(value, _jsonOptions.GetTypeInfo(value.GetType()));
    }

    /// <summary>
    /// Normalizes raw YAML structures into JSON-compatible dictionaries and lists before model binding.
    /// </summary>
    private static object? NormalizeForJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => NormalizeJsonObject(element),
                JsonValueKind.Array => element.EnumerateArray().Select(v => NormalizeForJson(v)).ToList(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
                JsonValueKind.Number when element.TryGetDouble(out var number) => number,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString(),
            };
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                var key = Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    normalized[key] = NormalizeForJson(entry.Value);
                }
            }

            return normalized;
        }

        if (value is System.Collections.IEnumerable sequence and not string)
        {
            var normalized = new List<object?>();
            foreach (var item in sequence)
            {
                normalized.Add(NormalizeForJson(item));
            }

            return normalized;
        }

        return value;
    }

    private static Dictionary<string, object?> NormalizeJsonObject(JsonElement element)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            normalized[property.Name] = NormalizeForJson(property.Value);
        }

        return normalized;
    }

    private sealed class FlexibleBooleanJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String when bool.TryParse(reader.GetString(), out var value) => value,
                _ => throw new JsonException($"The JSON value could not be converted to {typeToConvert}.")
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    /// <summary>
    /// Checks if a type is a collection type.
    /// </summary>
    private static bool IsCollectionType(Type type)
    {
        if (type.IsArray) return true;
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            return genericDef == typeof(List<>) ||
                   genericDef == typeof(IList<>) ||
                   genericDef == typeof(IReadOnlyList<>) ||
                   genericDef == typeof(IEnumerable<>) ||
                   genericDef == typeof(ICollection<>) ||
                   genericDef == typeof(IReadOnlyCollection<>);
        }
        return false;
    }

    /// <summary>
    /// Gets a comma-separated list of valid client names.
    /// </summary>
    private static string GetValidClientNames()
    {
        var clientType = typeof(McpServerClient);
        var properties = clientType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var names = properties
            .Where(p => p.PropertyType.Name.EndsWith("Client"))
            .Select(p => p.Name)
            .OrderBy(n => n);
        return string.Join(", ", names);
    }
}
