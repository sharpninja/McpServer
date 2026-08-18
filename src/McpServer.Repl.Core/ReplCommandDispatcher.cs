// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Server-side command dispatcher
// FR-MCP-REPL-003: Command Namespace Parity - Request routing to client passthrough
// TR-MCP-REPL-004: Command Registry and Dispatcher - Envelope-to-handler routing
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes

using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using McpServer.Client;
using McpServer.Client.Models;
namespace McpServer.Repl.Core;

/// <summary>
/// Dispatches parsed YAML envelopes to the appropriate handler and returns the response
/// envelope. Responsible for routing <c>hello</c> handshakes and <c>request</c> envelopes
/// by method namespace (currently <c>client.*.*</c> via <see cref="IGenericClientPassthrough"/>).
/// Unknown namespaces produce a <c>method_not_found</c> error envelope so the agent loop
/// can respond and continue instead of crashing.
/// </summary>
public interface IReplCommandDispatcher
{
    /// <summary>
    /// Dispatches a parsed YAML envelope and returns the response envelope (result or error).
    /// Never throws for recoverable dispatch failures — unexpected exceptions are caught and
    /// wrapped in an error envelope so the caller's read/write loop can remain alive.
    /// </summary>
    /// <param name="envelope">The inbound envelope to dispatch. Must have a non-null payload.</param>
    /// <param name="cancellationToken">Cancellation token propagated to handlers.</param>
    /// <returns>The response envelope to emit back to the caller.</returns>
    Task<IYamlEnvelope> DispatchAsync(IYamlEnvelope envelope, CancellationToken cancellationToken);
}

/// <summary>
/// Dispatches parsed YAML envelopes and can emit additional envelopes while a command is still running.
/// This is used for commands that naturally stream progress events before returning a final result.
/// </summary>
public interface IStreamingReplCommandDispatcher : IReplCommandDispatcher
{
    /// <summary>
    /// Dispatches a parsed YAML envelope and emits any intermediate envelopes through the supplied callback.
    /// </summary>
    /// <param name="envelope">The inbound envelope to dispatch. Must have a non-null payload.</param>
    /// <param name="emitEnvelopeAsync">Callback used to write intermediate envelopes to the caller, or null to buffer only.</param>
    /// <param name="cancellationToken">Cancellation token propagated to handlers.</param>
    /// <returns>The final response envelope to emit back to the caller.</returns>
    Task<IYamlEnvelope> DispatchAsync(
        IYamlEnvelope envelope,
        Func<IYamlEnvelope, Task>? emitEnvelopeAsync,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IReplCommandDispatcher"/> implementation. Routes <c>hello</c> envelopes
/// to a handshake response and <c>request</c> envelopes with the <c>client.&lt;clientName&gt;.&lt;methodName&gt;</c>
/// method shape to <see cref="IGenericClientPassthrough.InvokeAsync"/>. All other method
/// namespaces produce a <c>method_not_found</c> error envelope.
/// </summary>
public sealed class ReplCommandDispatcher : IStreamingReplCommandDispatcher
{
    private const string StreamCommandTimeoutEnvVar = "MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS";
    private const string ServerProtocolVersion = "1.0";
    private static readonly TimeSpan DefaultStreamCommandTimeout = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FlexibleBooleanJsonConverter() },
        TypeInfoResolver = JsonTypeInfoResolver.Combine(ReplCoreJsonContext.Default, McpClientJsonContext.Default),
    };

    private readonly IGenericClientPassthrough _passthrough;
    private readonly ISessionLogWorkflow? _sessionLogWorkflow;
    private readonly ISessionLogPersistenceStrategy? _sessionLogPersistenceStrategy;
    private readonly IRequirementsWorkflow? _requirementsWorkflow;
    private readonly ITodoWorkflow? _todoWorkflow;
    private readonly IMemoryWorkflow? _memoryWorkflow;
    private readonly IGraphRagWorkflow? _graphRagWorkflow;
    private readonly ITriageWorkflow? _triageWorkflow;
    private readonly IAgentHelpWorkflow? _agentHelpWorkflow;
    private readonly ITranscriptIngestionWorkflow? _transcriptIngestionWorkflow;
    private readonly IHandoffWorkflow? _handoffWorkflow;
    private readonly IClientMutationPolicy? _clientMutationPolicy;

    /// <summary>
    /// Initializes a new <see cref="ReplCommandDispatcher"/>.
    /// </summary>
    /// <param name="passthrough">The generic client passthrough used to invoke <c>client.*.*</c> methods.</param>
    /// <param name="sessionLogWorkflow">The optional session-log workflow used to invoke <c>workflow.sessionlog.*</c> methods.</param>
    /// <param name="sessionLogPersistenceStrategy">The optional REPL-native session-log persistence coordinator.</param>
    /// <param name="requirementsWorkflow">The optional requirements workflow used to invoke <c>workflow.requirements.*</c> methods.</param>
    /// <param name="todoWorkflow">The optional TODO workflow used to invoke <c>workflow.todo.*</c> methods.</param>
    /// <param name="memoryWorkflow">The optional memory workflow used to invoke <c>workflow.memory.*</c> methods.</param>
    /// <param name="clientMutationPolicy">The optional policy used to block unsafe generic <c>client.*</c> mutations.</param>
    /// <param name="graphRagWorkflow">The optional GraphRAG workflow used to invoke <c>workflow.graphrag.*</c> methods.</param>
    /// <param name="triageWorkflow">The optional triage workflow used to invoke <c>workflow.triage.*</c> methods.</param>
    /// <param name="agentHelpWorkflow">The optional Agent Help workflow used to invoke <c>workflow.agenthelp.*</c> methods.</param>
    /// <param name="transcriptIngestionWorkflow">The optional transcript ingestion workflow used to invoke <c>repl.sessionlog.*Transcripts</c> methods.</param>
    /// <param name="handoffWorkflow">The optional handoff workflow used to invoke <c>workflow.handoff.*</c> methods.</param>
    public ReplCommandDispatcher(
        IGenericClientPassthrough passthrough,
        ISessionLogWorkflow? sessionLogWorkflow = null,
        IRequirementsWorkflow? requirementsWorkflow = null,
        ITodoWorkflow? todoWorkflow = null,
        IMemoryWorkflow? memoryWorkflow = null,
        IClientMutationPolicy? clientMutationPolicy = null,
        IGraphRagWorkflow? graphRagWorkflow = null,
        ITriageWorkflow? triageWorkflow = null,
        IAgentHelpWorkflow? agentHelpWorkflow = null,
        ISessionLogPersistenceStrategy? sessionLogPersistenceStrategy = null,
        ITranscriptIngestionWorkflow? transcriptIngestionWorkflow = null,
        IHandoffWorkflow? handoffWorkflow = null)
    {
        _passthrough = passthrough ?? throw new ArgumentNullException(nameof(passthrough));
        _sessionLogWorkflow = sessionLogWorkflow;
        _sessionLogPersistenceStrategy = sessionLogPersistenceStrategy;
        _requirementsWorkflow = requirementsWorkflow;
        _todoWorkflow = todoWorkflow;
        _memoryWorkflow = memoryWorkflow;
        _graphRagWorkflow = graphRagWorkflow;
        _triageWorkflow = triageWorkflow;
        _agentHelpWorkflow = agentHelpWorkflow;
        _transcriptIngestionWorkflow = transcriptIngestionWorkflow;
        _handoffWorkflow = handoffWorkflow;
        _clientMutationPolicy = clientMutationPolicy;
    }

    /// <inheritdoc />
    public Task<IYamlEnvelope> DispatchAsync(IYamlEnvelope envelope, CancellationToken cancellationToken)
        => DispatchAsync(envelope, emitEnvelopeAsync: null, cancellationToken);

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<IYamlEnvelope> DispatchAsync(
        IYamlEnvelope envelope,
        Func<IYamlEnvelope, Task>? emitEnvelopeAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        switch (envelope.Type)
        {
            case "hello":
                return BuildHelloResponse(envelope.Payload as IHelloPayload);

            case "request":
                if (envelope.Payload is not IRequestPayload request)
                {
                    return BuildError(
                        requestId: "unknown",
                        code: "invalid_envelope",
                        message: "Request envelope is missing a request payload.");
                }
                return await DispatchRequestAsync(request, emitEnvelopeAsync, cancellationToken).ConfigureAwait(false);

            case "batch":
                return BuildUnsupportedBatchEnvelopeError(envelope.Payload);

            default:
                return BuildError(
                    requestId: "unknown",
                    code: "invalid_envelope",
                    message: $"Unsupported envelope type: {envelope.Type}");
        }
    }

    private async Task<IYamlEnvelope> DispatchRequestAsync(
        IRequestPayload request,
        Func<IYamlEnvelope, Task>? emitEnvelopeAsync,
        CancellationToken cancellationToken)
    {
        var method = request.Method ?? "";
        var schemaValidation = ReplYamlMessageValidator.ValidateRequest(request);
        if (!schemaValidation.IsValid)
        {
            return BuildError(
                requestId: string.IsNullOrWhiteSpace(request.RequestId) ? "unknown" : request.RequestId,
                code: "schema_validation_failed",
                message: "YAML request failed schema validation.",
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = method,
                    ["errors"] = schemaValidation.Errors,
                });
        }

        if (string.Equals(method, SessionLogCommandShapes.PersistTurnMethod, StringComparison.Ordinal))
        {
            return await DispatchSessionLogPersistenceRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(method, SessionLogCommandShapes.IngestTranscriptsMethod, StringComparison.Ordinal) ||
            string.Equals(method, SessionLogCommandShapes.NormalizeTranscriptsMethod, StringComparison.Ordinal))
        {
            return await DispatchTranscriptIngestionRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (method.StartsWith("client.", StringComparison.Ordinal))
        {
            return await DispatchClientRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // FR-MCP-REPL-006: every workflow.* namespace is DEPRECATED in favor of the
        // client.<Client>.<Method> passthrough surface (and the stateless session
        // lifecycle verbs). Responses carry deprecated: true so callers migrate.
        if (method.StartsWith(RequirementsCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchRequirementsRequestAsync(request, cancellationToken).ConfigureAwait(false));
        }

        if (method.StartsWith(SessionLogCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchSessionLogRequestAsync(request, cancellationToken).ConfigureAwait(false));
        }

        if (method.StartsWith(TodoCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchTodoRequestAsync(request, emitEnvelopeAsync, cancellationToken).ConfigureAwait(false));
        }

        if (method.StartsWith(MemoryCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchMemoryRequestAsync(request, cancellationToken).ConfigureAwait(false));
        }

        if (method.StartsWith(TriageCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchTriageRequestAsync(request, cancellationToken).ConfigureAwait(false));
        }

        if (method.StartsWith(AgentHelpCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchAgentHelpRequestAsync(request, cancellationToken).ConfigureAwait(false));
        }

        if (method.StartsWith(GraphRagCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchGraphRagRequestAsync(request, cancellationToken).ConfigureAwait(false));
        }

        if (method.StartsWith(HandoffCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return MarkWorkflowDeprecated(await DispatchHandoffRequestAsync(request, cancellationToken).ConfigureAwait(false));
        }

        return BuildError(
            requestId: request.RequestId,
            code: "method_not_found",
            message: $"Method '{method}' is not routed by this dispatcher. " +
                     $"Primary namespace: client.<clientName>.<methodName>. " +
                     $"Deprecated namespaces (migrate to client.*): {SessionLogCommandShapes.MethodNamespace}.*, {RequirementsCommandShapes.MethodNamespace}.*, {TodoCommandShapes.MethodNamespace}.*, {MemoryCommandShapes.MethodNamespace}.*, {TriageCommandShapes.MethodNamespace}.*, {AgentHelpCommandShapes.MethodNamespace}.*, {GraphRagCommandShapes.MethodNamespace}.*, {HandoffCommandShapes.MethodNamespace}.*.");
    }

    private async Task<IYamlEnvelope> DispatchSessionLogPersistenceRequestAsync(
        IRequestPayload request,
        CancellationToken cancellationToken)
    {
        if (_sessionLogPersistenceStrategy is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Session-log persistence strategies are not registered.");
        }

        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        try
        {
            var sessionLog = BuildSessionLogRecovery(args);
            var persistence = await _sessionLogPersistenceStrategy
                .PersistAsync(sessionLog, cancellationToken)
                .ConfigureAwait(false);
            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = new Dictionary<string, object?>
                    {
                        ["persisted"] = persistence.Persisted,
                        ["degraded"] = persistence.Degraded,
                        ["persistenceStrategy"] = persistence.Strategy,
                        ["failsafePath"] = persistence.FailsafePath,
                        ["message"] = persistence.Message,
                    },
                },
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "session_log_persistence_failed",
                message: exception.Message,
                details: new Dictionary<string, object?>
                {
                    ["exceptionType"] = exception.GetType().FullName,
                });
        }
    }

    private static IYamlEnvelope MarkWorkflowDeprecated(IYamlEnvelope response)
    {
        if (response.Type == "result" && response.Payload is ResultPayload result)
        {
            result.Deprecated = true;
        }

        return response;
    }

    /// <summary>
    /// FR-MCP-REPL-006: routes workflow.sessionlog lifecycle verbs that carry
    /// explicit (agent, sessionId) identifiers straight to the stateless
    /// SessionLog client lifecycle methods. Returns null when the verb is not a
    /// lifecycle verb or the identifiers are absent (legacy stateful path).
    /// </summary>
    private async Task<IYamlEnvelope?> TryDispatchStatelessLifecycleAsync(
        IRequestPayload request,
        Dictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var agent = GetString(args, "agent") ?? GetString(args, "sourceType");
        var sessionId = GetString(args, "sessionId");
        if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        string clientMethod;
        Dictionary<string, object?> clientArgs;
        Dictionary<string, object?> resultShape;

        switch (request.Method)
        {
            case SessionLogCommandShapes.OpenSessionMethod:
                // Call workflow.Open to set local REPL state (_state) so subsequent beginTurn/appendActions (workflow.*) find active session.
                // Also route via client for server-side open/submit (passthrough).
                // agent/sessionId already validated non-empty above; no magic "Codex" default.
                if (_sessionLogWorkflow is not null)
                {
                    await _sessionLogWorkflow.OpenSessionAsync(
                        agent,
                        RequireString(args, "sessionId"),
                        RequireString(args, "title"),
                        GetString(args, "model") ?? "unknown",
                        cancellationToken).ConfigureAwait(false);
                }
                clientMethod = "OpenSessionAsync";
                clientArgs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["agent"] = agent,
                    ["sessionId"] = sessionId,
                    ["title"] = GetString(args, "title"),
                    ["model"] = GetString(args, "model"),
                };
                resultShape = new Dictionary<string, object?> { ["sessionId"] = sessionId, ["opened"] = true };
                break;

            case SessionLogCommandShapes.BeginTurnMethod:
                // Mirror to local workflow state so that subsequent appendActions (even if sent without ids)
                // will find an active turn in _state. The client passthrough handles the server side.
                if (_sessionLogWorkflow is not null)
                {
                    await _sessionLogWorkflow.BeginTurnAsync(
                        RequireString(args, "requestId"),
                        RequireString(args, "queryTitle"),
                        RequireString(args, "queryText"),
                        cancellationToken,
                        GetString(args, "planFile") ?? "None",
                        GetString(args, "todoId") ?? "None").ConfigureAwait(false);
                }
                clientMethod = "BeginTurnAsync";
                clientArgs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["agent"] = agent,
                    ["sessionId"] = sessionId,
                    ["requestId"] = GetString(args, "requestId"),
                    ["queryTitle"] = GetString(args, "queryTitle"),
                    ["queryText"] = GetString(args, "queryText"),
                    ["model"] = GetString(args, "model"),
                    ["planFile"] = GetString(args, "planFile") ?? "None",
                    ["todoId"] = GetString(args, "todoId") ?? "None",
                };
                resultShape = new Dictionary<string, object?> { ["requestId"] = GetString(args, "requestId"), ["status"] = "in_progress" };
                break;

            case SessionLogCommandShapes.CompleteTurnMethod:
                if (_sessionLogWorkflow is not null)
                {
                    // Mirror completion to clear local active turn for any follow-on legacy commands.
                    await ApplyQueryTitleOverrideAsync(_sessionLogWorkflow, args, cancellationToken).ConfigureAwait(false);
                    var resp = GetString(args, "response") ?? "completed";
                    await _sessionLogWorkflow.CompleteTurnAsync(resp, cancellationToken).ConfigureAwait(false);
                }
                clientMethod = "CompleteTurnAsync";
                clientArgs = BuildFinalizeArgs(agent, sessionId, args, failureNote: null);
                resultShape = new Dictionary<string, object?> { ["requestId"] = GetString(args, "requestId"), ["status"] = "completed" };
                break;

            case SessionLogCommandShapes.FailTurnMethod:
                if (_sessionLogWorkflow is not null)
                {
                    var err = GetString(args, "errorMessage") ?? GetString(args, "failureNote") ?? "failed";
                    await _sessionLogWorkflow.FailTurnAsync(err, GetString(args, "errorCode"), cancellationToken).ConfigureAwait(false);
                }
                clientMethod = "FailTurnAsync";
                clientArgs = BuildFinalizeArgs(agent, sessionId, args, failureNote: GetString(args, "errorMessage") ?? GetString(args, "failureNote"));
                resultShape = new Dictionary<string, object?> { ["requestId"] = GetString(args, "requestId"), ["status"] = "failed" };
                break;

            case SessionLogCommandShapes.AppendActionsMethod:
                // When ids are present we can still honor appendActions by routing through the (now populated) local workflow state.
                // This keeps the legacy append path working even after a begin that carried explicit ids.
                if (_sessionLogWorkflow is not null)
                {
                    await ApplyQueryTitleOverrideAsync(_sessionLogWorkflow, args, cancellationToken).ConfigureAwait(false);
                    var acts = GetSessionActions(args, "actions");
                    if (acts.Count > 0)
                    {
                        await _sessionLogWorkflow.AppendActionsAsync(acts, cancellationToken).ConfigureAwait(false);
                    }
                }
                // Return success directly (the workflow Submit already persisted the turn+actions).
                return new YamlEnvelope
                {
                    Type = "result",
                    Payload = new ResultPayload
                    {
                        RequestId = request.RequestId,
                        Result = new Dictionary<string, object?> { ["appended"] = true },
                        Deprecated = true,
                    },
                };

            default:
                return null;
        }

        try
        {
            await _passthrough.InvokeAsync("SessionLog", clientMethod, clientArgs, cancellationToken).ConfigureAwait(false);
            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = resultShape,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private static Dictionary<string, object?> BuildFinalizeArgs(
        string agent,
        string sessionId,
        Dictionary<string, object?> args,
        string? failureNote)
    {
        // Forward the turn payload either verbatim (params.payload) or assembled
        // from the flat legacy parameter names so existing callers keep working.
        var payload = args.TryGetValue("payload", out var explicitPayload) && explicitPayload is not null
            ? explicitPayload
            : BuildPayloadFromFlatArgs(args, failureNote);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["agent"] = agent,
            ["sessionId"] = sessionId,
            ["requestId"] = GetString(args, "requestId"),
            ["payload"] = payload,
        };
    }

    private static Dictionary<string, object?> BuildPayloadFromFlatArgs(Dictionary<string, object?> args, string? failureNote)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in new[]
                 {
                      "response", "interpretation", "tokenCount", "model", "queryTitle", "tags", "contextList",
                     "designDecisions", "commits", "actions", "filesModified", "blockers", "processingDialog",
                 })
        {
            if (args.TryGetValue(key, out var value) && value is not null)
            {
                payload[key] = value;
            }
        }

        if (!string.IsNullOrWhiteSpace(failureNote))
        {
            payload["failureNote"] = failureNote;
        }

        return payload;
    }

    private static async Task ApplyQueryTitleOverrideAsync(
        ISessionLogWorkflow workflow,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var queryTitle = GetString(args, "queryTitle");
        if (!string.IsNullOrWhiteSpace(queryTitle))
        {
            await workflow.UpdateTurnTitleAsync(queryTitle, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IYamlEnvelope> DispatchSessionLogRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        // FR-MCP-REPL-006: lifecycle verbs carrying explicit (agent, sessionId)
        // identifiers route STATELESSLY through the SessionLog client - no
        // in-process active-session state is consulted or required. The legacy
        // stateful path below remains only for callers that omit the identifiers.
        var statelessResponse = await TryDispatchStatelessLifecycleAsync(request, args, cancellationToken).ConfigureAwait(false);
        if (statelessResponse is not null)
        {
            return statelessResponse;
        }

        if (_sessionLogWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Session-log workflow is not registered.");
        }

        var workflow = _sessionLogWorkflow;

        try
        {
            object? result;
            switch (request.Method)
            {
                case SessionLogCommandShapes.BootstrapMethod:
                    await workflow.BootstrapAsync(cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["initialized"] = true };
                    break;

                case SessionLogCommandShapes.OpenSessionMethod:
                    // Legacy no-ids path: still require a sensible agent from params; no silent Codex.
                    await workflow.OpenSessionAsync(
                        GetString(args, "agent") ?? GetString(args, "sourceType") ?? "unknown",
                        RequireString(args, "sessionId"),
                        RequireString(args, "title"),
                        GetString(args, "model") ?? "unknown",
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?>
                    {
                        ["sessionId"] = RequireString(args, "sessionId"),
                        ["opened"] = true,
                    };
                    break;

                case SessionLogCommandShapes.CurrentSessionMethod:
                    result = workflow.CurrentSession();
                    break;

                case SessionLogCommandShapes.BeginTurnMethod:
                    await workflow.BeginTurnAsync(
                        RequireString(args, "requestId"),
                        RequireString(args, "queryTitle"),
                        RequireString(args, "queryText"),
                        cancellationToken,
                        GetString(args, "planFile") ?? "None",
                        GetString(args, "todoId") ?? "None").ConfigureAwait(false);
                    result = new Dictionary<string, object?>
                    {
                        ["requestId"] = RequireString(args, "requestId"),
                        ["status"] = "in_progress",
                    };
                    break;

                case SessionLogCommandShapes.UpdateTurnMethod:
                    await ApplyQueryTitleOverrideAsync(workflow, args, cancellationToken).ConfigureAwait(false);
                    await workflow.UpdateTurnAsync(
                        GetString(args, "response"),
                        GetString(args, "interpretation"),
                        GetInt(args, "tokenCount"),
                        GetStringList(args, "tags"),
                        GetStringList(args, "contextList"),
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["updated"] = true };
                    break;

                case SessionLogCommandShapes.CompleteTurnMethod:
                    await ApplyQueryTitleOverrideAsync(workflow, args, cancellationToken).ConfigureAwait(false);
                    await workflow.CompleteTurnAsync(
                        RequireString(args, "response"),
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["status"] = "completed" };
                    break;

                case SessionLogCommandShapes.FailTurnMethod:
                    await workflow.FailTurnAsync(
                        RequireString(args, "errorMessage"),
                        GetString(args, "errorCode"),
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["status"] = "failed" };
                    break;

                case SessionLogCommandShapes.AppendDialogMethod:
                    await workflow.AppendDialogAsync(
                        GetDialogItems(args, "dialogItems"),
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["appended"] = true };
                    break;

                case SessionLogCommandShapes.AppendActionsMethod:
                    await ApplyQueryTitleOverrideAsync(workflow, args, cancellationToken).ConfigureAwait(false);
                    await workflow.AppendActionsAsync(
                        GetSessionActions(args, "actions"),
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["appended"] = true };
                    break;

                case SessionLogCommandShapes.QueryHistoryMethod:
                    result = await workflow.QueryHistoryAsync(
                        GetString(args, "agent") ?? GetString(args, "sourceType"),
                        GetInt(args, "limit") ?? 10,
                        GetInt(args, "offset") ?? 0,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case SessionLogCommandShapes.ImportRecoveryMethod:
                    result = await workflow.ImportRecoveryAsync(
                        BuildSessionLogRecovery(args),
                        cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    return BuildError(
                        requestId: request.RequestId,
                        code: "method_not_found",
                        message: $"Method '{request.Method}' is not routed by the session-log workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private async Task<IYamlEnvelope> DispatchTranscriptIngestionRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_transcriptIngestionWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Transcript ingestion workflow is not registered.");
        }

        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        try
        {
            var normalize = string.Equals(request.Method, SessionLogCommandShapes.NormalizeTranscriptsMethod, StringComparison.Ordinal);
            var transcriptRequest = BuildTranscriptIngestPathRequest(args, normalize);
            var result = normalize
                ? await _transcriptIngestionWorkflow.NormalizeTranscriptsAsync(transcriptRequest, cancellationToken).ConfigureAwait(false)
                : await _transcriptIngestionWorkflow.IngestTranscriptsAsync(transcriptRequest, cancellationToken).ConfigureAwait(false);

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private static TranscriptIngestPathRequest BuildTranscriptIngestPathRequest(
        IReadOnlyDictionary<string, object?> args,
        bool normalize)
    {
        var source = ParseEnum(GetString(args, "source"), TranscriptSourceKind.Auto, "source");
        if (normalize)
        {
            var profileName = GetString(args, "targetProfile") ?? GetString(args, "compatibilityProfile");
            var profile = ParseEnum(profileName, TranscriptCompatibilityProfile.None, "targetProfile");
            if (profile == TranscriptCompatibilityProfile.None)
                throw new ArgumentException("Missing required parameter: targetProfile");

            return new TranscriptIngestPathRequest
            {
                Path = RequireString(args, "path"),
                Agent = RequireString(args, "agent"),
                Source = source,
                Recursive = GetBool(args, "recursive") ?? true,
                Strict = GetBool(args, "strict") ?? true,
                Persist = GetBool(args, "persist") ?? false,
                CompatibilityProfile = profile,
                EmitNormalizedProfile = true,
            };
        }

        var compatibilityProfile = ParseEnum(GetString(args, "compatibilityProfile"), TranscriptCompatibilityProfile.None, "compatibilityProfile");
        return new TranscriptIngestPathRequest
        {
            Path = RequireString(args, "path"),
            Agent = RequireString(args, "agent"),
            Source = source,
            Recursive = GetBool(args, "recursive") ?? true,
            Strict = GetBool(args, "strict") ?? true,
            Persist = GetBool(args, "persist") ?? true,
            CompatibilityProfile = compatibilityProfile,
            EmitNormalizedProfile = GetBool(args, "emitNormalizedProfile") ?? compatibilityProfile != TranscriptCompatibilityProfile.None,
        };
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException($"Invalid {parameterName}: {value}");
    }

    private async Task<IYamlEnvelope> DispatchTodoRequestAsync(
        IRequestPayload request,
        Func<IYamlEnvelope, Task>? emitEnvelopeAsync,
        CancellationToken cancellationToken)
    {
        if (_todoWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "TODO workflow is not registered.");
        }

        var workflow = _todoWorkflow;
        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        try
        {
            object? result;
            switch (request.Method)
            {
                case TodoCommandShapes.QueryMethod:
                    result = await workflow.QueryAsync(
                        GetString(args, "keyword"),
                        GetString(args, "priority"),
                        GetString(args, "section"),
                        GetString(args, "id"),
                        GetBool(args, "done"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.GetMethod:
                    result = await workflow.GetAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.SelectMethod:
                    await workflow.SelectAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?>
                    {
                        ["selected"] = true,
                        ["id"] = RequireString(args, "id"),
                    };
                    break;

                case TodoCommandShapes.CreateMethod:
                    result = await workflow.CreateAsync(BuildTodoCreateRequest(GetRequestArgs(args)), cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.UpdateMethod:
                    result = await workflow.UpdateAsync(
                        RequireString(args, "id"),
                        BuildTodoUpdateRequest(GetRequestArgs(args)),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.UpdateSelectedMethod:
                    result = await workflow.UpdateAsync(BuildTodoUpdateRequest(GetRequestArgs(args)), cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.DeleteMethod:
                    result = await DeleteAndReturnAsync(
                        () => workflow.DeleteAsync(RequireString(args, "id"), cancellationToken),
                        RequireString(args, "id")).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.DeleteSelectedMethod:
                    await workflow.DeleteAsync(cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["deleted"] = true };
                    break;

                case TodoCommandShapes.AnalyzeRequirementsMethod:
                    var policyError = BuildClientMutationPolicyErrorIfRejected(
                        request.RequestId,
                        "todo",
                        "AnalyzeRequirementsAsync",
                        args);
                    if (policyError is not null)
                        return policyError;

                    result = await workflow.AnalyzeRequirementsAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.StreamStatusMethod:
                    result = await CollectTodoEventsAsync(
                        request.RequestId,
                        request.Method,
                        (callback, streamCancellationToken) => workflow.StreamStatusAsync(
                            RequireString(args, "id"),
                            callback,
                            streamCancellationToken),
                        emitEnvelopeAsync,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.StreamPlanMethod:
                    result = await CollectTodoEventsAsync(
                        request.RequestId,
                        request.Method,
                        (callback, streamCancellationToken) => workflow.StreamPlanAsync(
                            RequireString(args, "id"),
                            callback,
                            streamCancellationToken),
                        emitEnvelopeAsync,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.StreamImplementMethod:
                    result = await CollectTodoEventsAsync(
                        request.RequestId,
                        request.Method,
                        (callback, streamCancellationToken) => workflow.StreamImplementAsync(
                            RequireString(args, "id"),
                            callback,
                            streamCancellationToken),
                        emitEnvelopeAsync,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.GetProjectionStatusMethod:
                    result = await workflow.GetProjectionStatusAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false);
                    break;

                case TodoCommandShapes.RepairProjectionMethod:
                    await workflow.RepairProjectionAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?>
                    {
                        ["repaired"] = true,
                        ["id"] = RequireString(args, "id"),
                    };
                    break;

                case TodoCommandShapes.CurrentSelectionMethod:
                    result = workflow.CurrentSelection();
                    break;

                default:
                    return BuildError(
                        requestId: request.RequestId,
                        code: "method_not_found",
                        message: $"Method '{request.Method}' is not routed by the TODO workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private async Task<IYamlEnvelope> DispatchMemoryRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_memoryWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Memory workflow is not registered.");
        }

        var workflow = _memoryWorkflow;
        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        try
        {
            object? result = request.Method switch
            {
                MemoryCommandShapes.ListMethod =>
                    await workflow.ListAsync(
                        GetMemoryListScope(args, "scope"),
                        GetString(args, "category"),
                        GetString(args, "keyword"),
                        cancellationToken).ConfigureAwait(false),
                MemoryCommandShapes.GetMethod =>
                    await workflow.GetAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false),
                MemoryCommandShapes.AddMethod =>
                    await workflow.AddAsync(BuildMemoryAddRequest(GetRequestArgs(args)), cancellationToken).ConfigureAwait(false),
                MemoryCommandShapes.UpdateMethod =>
                    await workflow.UpdateAsync(
                        RequireString(args, "id"),
                        BuildMemoryUpdateRequest(GetRequestArgs(args)),
                        cancellationToken).ConfigureAwait(false),
                MemoryCommandShapes.RemoveMethod =>
                    await workflow.RemoveAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false),
                _ => null,
            };

            if (result is null)
            {
                return BuildError(
                    requestId: request.RequestId,
                    code: "method_not_found",
                    message: $"Method '{request.Method}' is not routed by the memory workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private async Task<IYamlEnvelope> DispatchTriageRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_triageWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Triage workflow is not registered.");
        }

        var workflow = _triageWorkflow;
        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);
        var requestArgs = GetRequestArgs(args);

        try
        {
            object? result = request.Method switch
            {
                TriageCommandShapes.ReportMethod =>
                    await workflow.ReportAsync(BuildTriageReportRequest(requestArgs), cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.GetReportMethod =>
                    await workflow.GetReportAsync(RequireString(args, requestArgs, "reportId"), cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.QueryGroupsMethod =>
                    await workflow.QueryGroupsAsync(
                        GetString(requestArgs, "status"),
                        GetString(requestArgs, "workspacePath"),
                        cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.GetDashboardMethod =>
                    await workflow.GetDashboardAsync(
                        GetString(requestArgs, "workspacePath"),
                        cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.GetGroupMethod =>
                    await workflow.GetGroupAsync(RequireString(args, requestArgs, "groupId"), cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.QueryRunsMethod =>
                    await workflow.QueryRunsAsync(
                        GetString(requestArgs, "status"),
                        GetString(requestArgs, "groupId"),
                        GetString(requestArgs, "workspacePath"),
                        cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.GetRunMethod =>
                    await workflow.GetRunAsync(RequireString(args, requestArgs, "runId"), cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.QueryCreatedTodosMethod =>
                    await workflow.QueryCreatedTodosAsync(
                        GetString(requestArgs, "workspacePath"),
                        cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.FlushGroupMethod =>
                    await workflow.FlushGroupAsync(RequireString(args, requestArgs, "groupId"), cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.RetryGroupMethod =>
                    await workflow.RetryGroupAsync(
                        RequireString(args, requestArgs, "groupId"),
                        GetBool(requestArgs, "force") ?? false,
                        cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.DeleteGroupMethod =>
                    await workflow.DeleteGroupAsync(
                        RequireString(args, requestArgs, "groupId"),
                        GetString(requestArgs, "reason"),
                        cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.CreateGroupMethod =>
                    await workflow.CreateGroupFromSelectionAsync(BuildTriageGroupSelectionRequest(requestArgs), cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.ConsolidateIntoGroupMethod =>
                    await workflow.ConsolidateIntoGroupAsync(
                        RequireString(args, requestArgs, "targetGroupId"),
                        BuildTriageGroupSelectionRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false),
                TriageCommandShapes.MergeGroupsMethod =>
                    await workflow.MergeGroupsAsync(
                        RequireString(args, requestArgs, "targetGroupId"),
                        BuildTriageGroupSelectionRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false),
                _ => null,
            };

            if (result is null)
            {
                return BuildError(
                    requestId: request.RequestId,
                    code: "method_not_found",
                    message: $"Method '{request.Method}' is not routed by the triage workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private async Task<IYamlEnvelope> DispatchHandoffRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_handoffWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Handoff workflow is not registered.");
        }

        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);
        var requestArgs = GetRequestArgs(args);

        try
        {
            object? result = request.Method switch
            {
                HandoffCommandShapes.IngestMethod =>
                    await _handoffWorkflow.IngestAsync(BuildHandoffIngestionRequest(requestArgs), cancellationToken).ConfigureAwait(false),
                HandoffCommandShapes.GetMethod =>
                    await _handoffWorkflow.GetAsync(RequireString(args, requestArgs, "runId"), cancellationToken).ConfigureAwait(false),
                HandoffCommandShapes.ApproveMethod =>
                    await _handoffWorkflow.ApproveAsync(
                        RequireString(args, requestArgs, "runId"),
                        new McpServer.Client.Models.HandoffApprovalRequest
                        {
                            Approved = GetBool(requestArgs, "approved") ?? false,
                            Reviewer = GetString(requestArgs, "reviewer"),
                            Notes = GetString(requestArgs, "notes"),
                        },
                        cancellationToken).ConfigureAwait(false),
                _ => null,
            };

            if (result is null)
            {
                return BuildError(
                    requestId: request.RequestId,
                    code: "method_not_found",
                    message: $"Method '{request.Method}' is not routed by the handoff workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private static McpServer.Client.Models.HandoffIngestionRequest BuildHandoffIngestionRequest(IReadOnlyDictionary<string, object?> args)
    {
        if (!Enum.TryParse<McpServer.Client.Models.HandoffSourceKind>(GetString(args, "sourceKind"), ignoreCase: true, out var sourceKind)
            || !Enum.IsDefined(sourceKind))
        {
            throw new ArgumentException("sourceKind must be Path, Content, or Artifact.");
        }

        var modeText = GetString(args, "mode");
        var mode = McpServer.Client.Models.HandoffIngestionMode.DraftOnly;
        if (!string.IsNullOrWhiteSpace(modeText)
            && (!Enum.TryParse(modeText, ignoreCase: true, out mode) || !Enum.IsDefined(mode)))
        {
            throw new ArgumentException("mode must be DraftOnly, RequireReview, or CreateWhenConfident.");
        }

        return new McpServer.Client.Models.HandoffIngestionRequest
        {
            SourceKind = sourceKind,
            Path = GetString(args, "path"),
            Content = GetString(args, "content"),
            ArtifactId = GetString(args, "artifactId"),
            Mode = mode,
            Force = GetBool(args, "force") ?? false,
            AgentName = GetString(args, "agentName"),
            PromptTemplateId = GetString(args, "promptTemplateId"),
        };
    }

    private async Task<IYamlEnvelope> DispatchAgentHelpRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_agentHelpWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Agent Help workflow is not registered.");
        }

        var workflow = _agentHelpWorkflow;
        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);
        var requestArgs = GetRequestArgs(args);

        try
        {
            object? result = request.Method switch
            {
                AgentHelpCommandShapes.CreateSessionMethod =>
                    await workflow.CreateSessionAsync(BuildAgentHelpSessionCreateRequest(requestArgs), cancellationToken).ConfigureAwait(false),
                AgentHelpCommandShapes.SubmitTurnMethod =>
                    await workflow.SubmitTurnAsync(
                        RequireString(args, requestArgs, "sessionId"),
                        BuildAgentHelpTurnRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false),
                AgentHelpCommandShapes.GetStatusMethod =>
                    await workflow.GetStatusAsync(RequireString(args, requestArgs, "sessionId"), cancellationToken).ConfigureAwait(false),
                AgentHelpCommandShapes.GetTranscriptMethod =>
                    await workflow.GetTranscriptAsync(RequireString(args, requestArgs, "sessionId"), cancellationToken).ConfigureAwait(false),
                _ => null,
            };

            if (result is null)
            {
                return BuildError(
                    requestId: request.RequestId,
                    code: "method_not_found",
                    message: $"Method '{request.Method}' is not routed by the Agent Help workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private async Task<IYamlEnvelope> DispatchGraphRagRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_graphRagWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "GraphRAG workflow is not registered.");
        }

        var workflow = _graphRagWorkflow;
        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);
        var requestArgs = GetRequestArgs(args);

        try
        {
            object? result;
            switch (request.Method)
            {
                case GraphRagCommandShapes.StatusMethod:
                    result = await workflow.GetStatusAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.IndexMethod:
                    result = await workflow.IndexAsync(
                        GetBool(requestArgs, "force") ?? false,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.QueryMethod:
                    var queryRequest = BuildGraphRagQueryRequest(requestArgs);
                    result = await workflow.QueryAsync(
                        queryRequest.Query,
                        queryRequest.Mode,
                        queryRequest.MaxChunks,
                        queryRequest.IncludeContextChunks,
                        queryRequest.MaxEntities,
                        queryRequest.MaxRelationships,
                        queryRequest.CommunityDepth,
                        queryRequest.ResponseTokenBudget,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.IngestMethod:
                    result = await workflow.IngestTextAsync(
                        BuildGraphRagIngestTextRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.DocumentsListMethod:
                    result = await workflow.ListDocumentsAsync(
                        GetInt(requestArgs, "skip") ?? 0,
                        GetInt(requestArgs, "take") ?? 50,
                        GetString(requestArgs, "sourceType"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.DocumentsChunksMethod:
                    result = await workflow.GetDocumentChunksAsync(
                        RequireString(args, requestArgs, "documentId"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.DocumentsDeleteMethod:
                    result = await workflow.DeleteDocumentAsync(
                        RequireString(args, requestArgs, "documentId"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.EntitiesCreateMethod:
                    result = await workflow.CreateEntityAsync(
                        BuildGraphEntityRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.EntitiesListMethod:
                    result = await workflow.ListEntitiesAsync(
                        GetInt(requestArgs, "skip") ?? 0,
                        GetInt(requestArgs, "take") ?? 50,
                        GetString(requestArgs, "entityType"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.EntitiesGetMethod:
                    result = await workflow.GetEntityAsync(
                        RequireString(args, requestArgs, "entityId"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.EntitiesUpdateMethod:
                    result = await workflow.UpdateEntityAsync(
                        RequireString(args, requestArgs, "entityId"),
                        BuildGraphEntityRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.EntitiesDeleteMethod:
                    await workflow.DeleteEntityAsync(
                        RequireString(args, requestArgs, "entityId"),
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["deleted"] = true };
                    break;

                case GraphRagCommandShapes.RelationshipsCreateMethod:
                    result = await workflow.CreateRelationshipAsync(
                        BuildGraphRelationshipRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.RelationshipsListMethod:
                    result = await workflow.ListRelationshipsAsync(
                        GetInt(requestArgs, "skip") ?? 0,
                        GetInt(requestArgs, "take") ?? 50,
                        GetString(requestArgs, "entityId"),
                        GetString(requestArgs, "type"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.RelationshipsGetMethod:
                    result = await workflow.GetRelationshipAsync(
                        RequireString(args, requestArgs, "relationshipId"),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.RelationshipsUpdateMethod:
                    result = await workflow.UpdateRelationshipAsync(
                        RequireString(args, requestArgs, "relationshipId"),
                        BuildGraphRelationshipRequest(requestArgs),
                        cancellationToken).ConfigureAwait(false);
                    break;

                case GraphRagCommandShapes.RelationshipsDeleteMethod:
                    await workflow.DeleteRelationshipAsync(
                        RequireString(args, requestArgs, "relationshipId"),
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?> { ["deleted"] = true };
                    break;

                default:
                    return BuildError(
                        requestId: request.RequestId,
                        code: "method_not_found",
                        message: $"Method '{request.Method}' is not routed by the GraphRAG workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private async Task<IYamlEnvelope> DispatchRequirementsRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_requirementsWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Requirements workflow is not registered.");
        }

        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        try
        {
            object? result = request.Method switch
            {
                RequirementsCommandShapes.ListFrMethod =>
                    await _requirementsWorkflow.ListFrAsync(GetString(args, "area"), GetString(args, "status"), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.RepairPlaceholdersMethod =>
                    new Dictionary<string, object?>
                    {
                        ["purged"] = await _requirementsWorkflow.PurgeInvalidPlaceholdersAsync(cancellationToken).ConfigureAwait(false)
                    },
                RequirementsCommandShapes.GetFrMethod =>
                    await _requirementsWorkflow.GetFrAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateFrMethod =>
                    await _requirementsWorkflow.CreateFrAsync(new FrCreateRequestModel
                    {
                        Id = RequireString(args, "id"),
                        Title = RequireString(args, "title"),
                        Description = RequireString(args, "description"),
                        Priority = RequireString(args, "priority"),
                        Area = RequireString(args, "area"),
                        Notes = GetString(args, "notes"),
                        AcceptanceCriteria = GetAcceptanceCriteria(args, "acceptanceCriteria"),
                        ScopeStartLayerKey = GetString(args, "scopeStartLayerKey"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateFrBatchMethod =>
                    await _requirementsWorkflow.CreateFrBatchAsync(RequireParams<CreateFrBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateFrMethod =>
                    await _requirementsWorkflow.UpdateFrAsync(new FrUpdateRequestModel
                    {
                        Id = GetString(args, "id"),
                        Title = GetString(args, "title"),
                        Description = GetString(args, "description"),
                        Status = GetString(args, "status"),
                        Priority = GetString(args, "priority"),
                        Notes = GetString(args, "notes"),
                        AcceptanceCriteria = GetAcceptanceCriteria(args, "acceptanceCriteria"),
                        ScopeStartLayerKey = GetString(args, "scopeStartLayerKey"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateFrBatchMethod =>
                    await _requirementsWorkflow.UpdateFrBatchAsync(RequireParams<UpdateFrBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.DeleteFrMethod =>
                    await DeleteAndReturnAsync(() => _requirementsWorkflow.DeleteFrAsync(RequireString(args, "id"), cancellationToken), RequireString(args, "id")).ConfigureAwait(false),
                RequirementsCommandShapes.ListTrMethod =>
                    await _requirementsWorkflow.ListTrAsync(GetString(args, "area"), GetString(args, "subarea"), GetString(args, "status"), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.GetTrMethod =>
                    await _requirementsWorkflow.GetTrAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateTrMethod =>
                    await _requirementsWorkflow.CreateTrAsync(new TrCreateRequestModel
                    {
                        Id = RequireString(args, "id"),
                        Title = RequireString(args, "title"),
                        Description = RequireString(args, "description"),
                        Priority = RequireString(args, "priority"),
                        Area = RequireString(args, "area"),
                        Subarea = RequireString(args, "subarea"),
                        Notes = GetString(args, "notes"),
                        AcceptanceCriteria = GetAcceptanceCriteria(args, "acceptanceCriteria"),
                        ScopeStartLayerKey = GetString(args, "scopeStartLayerKey"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateTrBatchMethod =>
                    await _requirementsWorkflow.CreateTrBatchAsync(RequireParams<CreateTrBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateTrMethod =>
                    await _requirementsWorkflow.UpdateTrAsync(new TrUpdateRequestModel
                    {
                        Id = GetString(args, "id"),
                        Title = GetString(args, "title"),
                        Description = GetString(args, "description"),
                        Status = GetString(args, "status"),
                        Priority = GetString(args, "priority"),
                        Notes = GetString(args, "notes"),
                        AcceptanceCriteria = GetAcceptanceCriteria(args, "acceptanceCriteria"),
                        ScopeStartLayerKey = GetString(args, "scopeStartLayerKey"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateTrBatchMethod =>
                    await _requirementsWorkflow.UpdateTrBatchAsync(RequireParams<UpdateTrBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.DeleteTrMethod =>
                    await DeleteAndReturnAsync(() => _requirementsWorkflow.DeleteTrAsync(RequireString(args, "id"), cancellationToken), RequireString(args, "id")).ConfigureAwait(false),
                RequirementsCommandShapes.ListTestMethod =>
                    await _requirementsWorkflow.ListTestAsync(GetString(args, "area"), GetString(args, "status"), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.GetTestMethod =>
                    await _requirementsWorkflow.GetTestAsync(RequireString(args, "id"), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateTestMethod =>
                    await _requirementsWorkflow.CreateTestAsync(new TestCreateRequestModel
                    {
                        Id = RequireString(args, "id"),
                        Title = RequireString(args, "title"),
                        Description = RequireString(args, "description"),
                        Priority = RequireString(args, "priority"),
                        Area = RequireString(args, "area"),
                        TestType = GetString(args, "testType") ?? "unit",
                        Notes = GetString(args, "notes"),
                        AcceptanceCriteria = GetAcceptanceCriteria(args, "acceptanceCriteria"),
                        ScopeStartLayerKey = GetString(args, "scopeStartLayerKey"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateTestBatchMethod =>
                    await _requirementsWorkflow.CreateTestBatchAsync(RequireParams<CreateTestBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateTestMethod =>
                    await _requirementsWorkflow.UpdateTestAsync(new TestUpdateRequestModel
                    {
                        Id = GetString(args, "id"),
                        Title = GetString(args, "title"),
                        Description = GetString(args, "description"),
                        Status = GetString(args, "status"),
                        Priority = GetString(args, "priority"),
                        Notes = GetString(args, "notes"),
                        AcceptanceCriteria = GetAcceptanceCriteria(args, "acceptanceCriteria"),
                        ScopeStartLayerKey = GetString(args, "scopeStartLayerKey"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateTestBatchMethod =>
                    await _requirementsWorkflow.UpdateTestBatchAsync(RequireParams<UpdateTestBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.DeleteTestMethod =>
                    await DeleteAndReturnAsync(() => _requirementsWorkflow.DeleteTestAsync(RequireString(args, "id"), cancellationToken), RequireString(args, "id")).ConfigureAwait(false),
                RequirementsCommandShapes.CreateBatchMethod =>
                    await _requirementsWorkflow.CreateBatchAsync(RequireParams<CreateRequirementsBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateBatchMethod =>
                    await _requirementsWorkflow.UpdateBatchAsync(RequireParams<UpdateRequirementsBatchRequest>(args), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.ListMappingsMethod =>
                    await _requirementsWorkflow.ListMappingsAsync(GetString(args, "frId"), GetString(args, "trId"), GetString(args, "testId"), cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateMappingMethod =>
                    await _requirementsWorkflow.CreateMappingAsync(new MappingCreateRequestModel
                    {
                        FrId = GetString(args, "frId"),
                        TrId = GetString(args, "trId"),
                        TestId = GetString(args, "testId"),
                        TrIds = GetStringList(args, "trIds"),
                        TestIds = GetStringList(args, "testIds"),
                        Notes = GetString(args, "notes"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.DeleteMappingMethod =>
                    await DeleteMappingAndReturnAsync(
                        GetString(args, "frId"),
                        GetString(args, "trId"),
                        GetString(args, "testId"),
                        cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.GenerateDocumentMethod =>
                    await _requirementsWorkflow.GenerateDocumentAsync(
                        GetString(args, "format") ?? "markdown",
                        GetString(args, "docType") ?? "all",
                        GetString(args, "workspacePath"),
                        cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.IngestDocumentMethod =>
                    await _requirementsWorkflow.IngestDocumentAsync(
                        GetString(args, "content") ?? string.Empty,
                        GetString(args, "format") ?? "markdown",
                        GetString(args, "mergeStrategy") ?? "merge",
                        GetRequirementsDocumentMap(args),
                        GetString(args, "sourceFormat"),
                        GetString(args, "preferredWikiFormat"),
                        cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.ListLayersMethod =>
                    await _requirementsWorkflow.ListRequirementLayersAsync(cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CreateLayerMethod =>
                    await _requirementsWorkflow.CreateRequirementLayerAsync(new RequirementScopeLayerCreateRequestModel
                    {
                        Key = RequireString(args, "key"),
                        Order = GetInt(args, "order") ?? throw new ArgumentException("Missing required parameter: order"),
                        Name = RequireString(args, "name"),
                        Description = GetString(args, "description"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.UpdateLayerMethod =>
                    await _requirementsWorkflow.UpdateRequirementLayerAsync(new RequirementScopeLayerUpdateRequestModel
                    {
                        Key = RequireString(args, "key"),
                        Name = GetString(args, "name"),
                        Description = GetString(args, "description"),
                        ScopeEndLayerKey = GetString(args, "scopeEndLayerKey"),
                    }, cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.EffectiveMethod =>
                    await _requirementsWorkflow.GetEffectiveRequirementsAsync(
                        GetString(args, "layerKey"),
                        GetString(args, "productScope") ?? "product",
                        cancellationToken).ConfigureAwait(false),
                RequirementsCommandShapes.CurrentSelectionMethod => _requirementsWorkflow.CurrentSelection(),
                _ => null,
            };

            if (result is null && request.Method is not RequirementsCommandShapes.CurrentSelectionMethod)
            {
                return BuildError(
                    requestId: request.RequestId,
                    code: "method_not_found",
                    message: $"Method '{request.Method}' is not routed by the requirements workflow.");
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["methodName"] = request.Method,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private async Task<IYamlEnvelope> DispatchClientRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        // method shape: client.<clientName>.<methodName>
        var parts = request.Method.Split('.', 3);
        if (parts.Length != 3 || parts[0] != "client" ||
            string.IsNullOrEmpty(parts[1]) || string.IsNullOrEmpty(parts[2]))
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: $"Method '{request.Method}' does not match the expected 'client.<clientName>.<methodName>' shape.");
        }

        var clientName = parts[1];
        var methodName = parts[2];
        var args = request.Params is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        var policyError = BuildClientMutationPolicyErrorIfRejected(request.RequestId, clientName, methodName, args);
        if (policyError is not null)
            return policyError;

        try
        {
            var result = await _passthrough.InvokeAsync(clientName, methodName, args, cancellationToken).ConfigureAwait(false);
            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["clientName"] = clientName,
                    ["methodName"] = methodName,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private static UnifiedSessionLogDto BuildSessionLogRecovery(IReadOnlyDictionary<string, object?> args)
    {
        var sessionLog = args.TryGetValue("sessionLog", out var sessionLogValue)
            ? ConvertValue<UnifiedSessionLogDto>(sessionLogValue)
            : null;
        sessionLog ??= new UnifiedSessionLogDto();

        sessionLog.SourceType = FirstNonEmpty(
            sessionLog.SourceType,
            GetString(args, "sourceType"),
            GetString(args, "agent"));
        sessionLog.SessionId = FirstNonEmpty(sessionLog.SessionId, GetString(args, "sessionId"));
        sessionLog.Title = FirstNonEmpty(sessionLog.Title, GetString(args, "title"));
        sessionLog.Model = FirstNonEmpty(sessionLog.Model, GetString(args, "model"));
        sessionLog.Started = FirstNonEmpty(sessionLog.Started, GetString(args, "started"));
        sessionLog.LastUpdated = FirstNonEmpty(sessionLog.LastUpdated, GetString(args, "lastUpdated"));
        sessionLog.Status = FirstNonEmpty(sessionLog.Status, GetString(args, "status"));
        sessionLog.Turns ??= args.TryGetValue("turns", out var turnsValue)
            ? ConvertValue<List<UnifiedRequestEntryDto>>(turnsValue)
            : null;

        return sessionLog;
    }

    private static IReadOnlyList<IDialogItem> GetDialogItems(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return Array.Empty<IDialogItem>();
        }

        return ConvertValue<List<DialogItemAdapter>>(value)?.Cast<IDialogItem>().ToArray()
            ?? Array.Empty<IDialogItem>();
    }

    private static IReadOnlyList<ISessionAction> GetSessionActions(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return Array.Empty<ISessionAction>();
        }

        return ConvertValue<List<SessionActionAdapter>>(value)?.Cast<ISessionAction>().ToArray()
            ?? Array.Empty<ISessionAction>();
    }

    private static T? ConvertValue<T>(object? value)
    {
        if (value is null)
        {
            return default;
        }

        if (value is T typed)
        {
            return typed;
        }

        var normalized = NormalizeJsonElement(value);
        var json = ToJsonNode(normalized)?.ToJsonString() ?? "null";
        return (T?)JsonSerializer.Deserialize(json, JsonOptions.GetTypeInfo(typeof(T)));
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return ToJsonNode(NormalizeJsonElement(element));
        }

        if (value is IDictionary dictionary)
        {
            var jsonObject = new JsonObject();
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    jsonObject[key] = ToJsonNode(entry.Value);
                }
            }

            return jsonObject;
        }

        if (value is IEnumerable sequence and not string)
        {
            var jsonArray = new JsonArray();
            foreach (var item in sequence)
            {
                jsonArray.Add(ToJsonNode(item));
            }

            return jsonArray;
        }

        return value switch
        {
            string text => JsonValue.Create(text),
            bool boolean => JsonValue.Create(boolean),
            byte number => JsonValue.Create(number),
            sbyte number => JsonValue.Create(number),
            short number => JsonValue.Create(number),
            ushort number => JsonValue.Create(number),
            int number => JsonValue.Create(number),
            uint number => JsonValue.Create(number),
            long number => JsonValue.Create(number),
            ulong number => JsonValue.Create(number),
            float number => JsonValue.Create(number),
            double number => JsonValue.Create(number),
            decimal number => JsonValue.Create(number),
            DateTime valueDate => JsonValue.Create(valueDate),
            DateTimeOffset valueDate => JsonValue.Create(valueDate),
            Guid valueGuid => JsonValue.Create(valueGuid),
            _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)),
        };
    }

    private static IReadOnlyList<AcceptanceCriterion>? GetAcceptanceCriteria(
        IReadOnlyDictionary<string, object?> args,
        string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return ConvertValue<IReadOnlyList<AcceptanceCriterion>>(value);
    }

    private static T RequireParams<T>(IReadOnlyDictionary<string, object?> args)
        where T : class
    {
        var value = ConvertValue<T>(args);
        if (value is null)
        {
            throw new ArgumentException("Request params could not be converted to the expected batch payload.");
        }

        return value;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static TodoCreateRequestAdapter BuildTodoCreateRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new TodoCreateRequestAdapter
        {
            Id = RequireString(args, "id"),
            Title = RequireString(args, "title"),
            Section = RequireString(args, "section"),
            Priority = RequireString(args, "priority"),
            Estimate = GetString(args, "estimate"),
            Description = GetStringList(args, "description"),
            TechnicalDetails = GetStringList(args, "technicalDetails"),
            ImplementationTasks = GetTodoSubtasks(args, "implementationTasks"),
            Note = GetString(args, "note"),
            Remaining = GetString(args, "remaining"),
            DependsOn = GetStringList(args, "dependsOn"),
            FunctionalRequirements = GetStringList(args, "functionalRequirements"),
            TechnicalRequirements = GetStringList(args, "technicalRequirements"),
        };
    }

    private static TodoUpdateRequestAdapter BuildTodoUpdateRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new TodoUpdateRequestAdapter
        {
            Title = GetString(args, "title"),
            Priority = GetString(args, "priority"),
            Section = GetString(args, "section"),
            Done = GetBool(args, "done"),
            Estimate = GetString(args, "estimate"),
            Description = GetOptionalStringList(args, "description"),
            TechnicalDetails = GetOptionalStringList(args, "technicalDetails"),
            ImplementationTasks = GetOptionalTodoSubtasks(args, "implementationTasks"),
            Note = GetString(args, "note"),
            CompletedDate = GetString(args, "completedDate"),
            DoneSummary = GetString(args, "doneSummary"),
            Remaining = GetString(args, "remaining"),
            DependsOn = GetOptionalStringList(args, "dependsOn"),
            FunctionalRequirements = GetOptionalStringList(args, "functionalRequirements"),
            TechnicalRequirements = GetOptionalStringList(args, "technicalRequirements"),
        };
    }

    private static MemoryAddRequest BuildMemoryAddRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new MemoryAddRequest
        {
            Id = GetString(args, "id"),
            Category = RequireString(args, "category"),
            Scope = GetMemoryScope(args, "scope") ?? MemoryScope.Workspace,
            Text = RequireString(args, "text"),
            UpdatedBy = GetString(args, "updatedBy"),
        };
    }

    private static MemoryUpdateRequest BuildMemoryUpdateRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new MemoryUpdateRequest
        {
            Category = GetString(args, "category"),
            Scope = GetMemoryScope(args, "scope"),
            Text = GetString(args, "text"),
            UpdatedBy = GetString(args, "updatedBy"),
        };
    }

    private static TriageReportRequest BuildTriageReportRequest(IReadOnlyDictionary<string, object?> args) => new()
    {
        Title = RequireString(args, "title"),
        Summary = RequireString(args, "summary"),
        ObservedBehavior = GetString(args, "observedBehavior"),
        ExpectedBehavior = GetString(args, "expectedBehavior"),
        Severity = GetString(args, "severity"),
        Component = GetString(args, "component"),
        DedupeKey = GetString(args, "dedupeKey"),
        ErrorSignature = GetString(args, "errorSignature"),
        AffectedPaths = GetStringList(args, "affectedPaths"),
        AffectedSymbols = GetStringList(args, "affectedSymbols"),
        Evidence = GetStringMap(args, "evidence"),
        ReproductionHints = GetStringList(args, "reproductionHints"),
        Tags = GetStringList(args, "tags"),
        ReporterAgent = GetString(args, "reporterAgent"),
        SessionId = GetString(args, "sessionId"),
        TurnId = GetString(args, "turnId"),
        CurrentTodoId = GetString(args, "currentTodoId"),
        WorkspacePath = GetString(args, "workspacePath"),
        IdempotencyKey = GetString(args, "idempotencyKey"),
    };

    private static TriageGroupSelectionRequest BuildTriageGroupSelectionRequest(IReadOnlyDictionary<string, object?> args) => new()
    {
        GroupIds = GetStringList(args, "groupIds"),
        ReportIds = GetStringList(args, "reportIds"),
        Title = GetString(args, "title"),
        Summary = GetString(args, "summary"),
    };

    private static AgentHelpSessionCreateRequest BuildAgentHelpSessionCreateRequest(IReadOnlyDictionary<string, object?> args)
        => new()
        {
            WorkspacePath = GetString(args, "workspacePath"),
            Topic = GetString(args, "topic"),
            DeviceId = GetString(args, "deviceId"),
            ExecutionStrategy = GetString(args, "executionStrategy"),
            AgentSeed = GetString(args, "agentSeed"),
            ClientName = GetString(args, "clientName"),
            AgentName = GetString(args, "agentName"),
            AgentPath = GetString(args, "agentPath"),
            AgentModel = GetString(args, "agentModel"),
            TodoId = GetString(args, "todoId"),
            CallerAgent = GetString(args, "callerAgent"),
            CallerSessionId = GetString(args, "callerSessionId"),
            CallerRequestId = GetString(args, "callerRequestId"),
            IssueSummary = GetString(args, "issueSummary"),
        };

    private static AgentHelpTurnRequest BuildAgentHelpTurnRequest(IReadOnlyDictionary<string, object?> args) => new()
    {
        UserMessage = RequireString(args, "userMessage"),
        ClientTimestampUtc = GetString(args, "clientTimestampUtc"),
    };

    private static Dictionary<string, object?> GetRequestArgs(Dictionary<string, object?> args)
    {
        if (!args.TryGetValue("request", out var requestValue) || requestValue is null)
        {
            return args;
        }

        return ToStringObjectDictionary(requestValue) ?? args;
    }

    private static Dictionary<string, object?>? ToStringObjectDictionary(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is Dictionary<string, object?> typed)
        {
            return new Dictionary<string, object?>(typed, StringComparer.OrdinalIgnoreCase);
        }

        if (value is IReadOnlyDictionary<string, object?> readOnly)
        {
            return new Dictionary<string, object?>(readOnly, StringComparer.OrdinalIgnoreCase);
        }

        if (value is IDictionary rawMap)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in rawMap)
            {
                var key = Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    normalized[key] = NormalizeJsonElement(entry.Value);
                }
            }

            return normalized;
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                normalized[property.Name] = NormalizeJsonElement(property.Value);
            }

            return normalized;
        }

        return null;
    }

    private static MemoryScope? GetMemoryScope(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is MemoryScope typed)
        {
            return typed;
        }

        if (value is JsonElement element)
        {
            value = element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt32(out var numeric) => numeric,
                _ => element.ToString(),
            };
        }

        if (value is string text)
        {
            if (int.TryParse(text, out _)
                || !Enum.TryParse(text, ignoreCase: true, out MemoryScope parsed)
                || !Enum.IsDefined(parsed))
            {
                throw new ArgumentException("Memory scope must be Global or Workspace.");
            }

            return parsed;
        }

        if (value is IConvertible convertible)
        {
            var convertibleText = Convert.ToString(convertible, System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(convertibleText)
                && !int.TryParse(convertibleText, out _)
                && Enum.TryParse(convertibleText, ignoreCase: true, out MemoryScope parsed)
                && Enum.IsDefined(parsed))
            {
                return parsed;
            }
        }

        throw new ArgumentException("Memory scope must be Global or Workspace.");
    }

    private static MemoryScope? GetMemoryListScope(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is JsonElement { ValueKind: JsonValueKind.String } element)
        {
            value = element.GetString();
        }

        if (value is string text && string.Equals(text.Trim(), "Effective", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return GetMemoryScope(args, name);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Memory list scope must be Effective, Global, or Workspace.", ex);
        }
    }

    private static object? NormalizeJsonElement(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Object => ToStringObjectDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(v => NormalizeJsonElement(v)).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDouble(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString(),
        };
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

    private static bool? GetBool(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is bool typed)
        {
            return typed;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(element.GetString(), out var parsed) => parsed,
                JsonValueKind.Number when element.TryGetInt32(out var numeric) => numeric != 0,
                _ => null,
            };
        }

        if (value is IConvertible convertible)
        {
            var text = Convert.ToString(convertible, System.Globalization.CultureInfo.InvariantCulture);
            if (bool.TryParse(text, out var parsed))
            {
                return parsed;
            }

            if (long.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var numeric))
            {
                return numeric != 0;
            }
        }

        return null;
    }

    private static int? GetInt(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is int typed)
        {
            return typed;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out var parsed) => parsed,
                JsonValueKind.String when int.TryParse(element.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null,
            };
        }

        if (value is IConvertible convertible)
        {
            var text = Convert.ToString(convertible, System.Globalization.CultureInfo.InvariantCulture);
            if (int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.ToString();
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RequireString(IReadOnlyDictionary<string, object?> args, string name)
    {
        var value = GetString(args, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required parameter: {name}");
        }

        return value;
    }

    private static string RequireString(
        IReadOnlyDictionary<string, object?> args,
        IReadOnlyDictionary<string, object?> fallbackArgs,
        string name)
    {
        var value = GetString(args, name) ?? GetString(fallbackArgs, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required parameter: {name}");
        }

        return value;
    }

    private static double? GetDouble(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is double typed)
        {
            return typed;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetDouble(out var parsed) => parsed,
                JsonValueKind.String when double.TryParse(element.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null,
            };
        }

        if (value is IConvertible convertible)
        {
            var text = Convert.ToString(convertible, System.Globalization.CultureInfo.InvariantCulture);
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static GraphRagQueryRequest BuildGraphRagQueryRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new GraphRagQueryRequest
        {
            Query = RequireString(args, "query"),
            Mode = GetString(args, "mode"),
            MaxChunks = GetInt(args, "maxChunks"),
            IncludeContextChunks = GetBool(args, "includeContextChunks") ?? true,
            MaxEntities = GetInt(args, "maxEntities"),
            MaxRelationships = GetInt(args, "maxRelationships"),
            CommunityDepth = GetInt(args, "communityDepth"),
            ResponseTokenBudget = GetInt(args, "responseTokenBudget"),
        };
    }

    private static GraphRagIngestTextRequest BuildGraphRagIngestTextRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new GraphRagIngestTextRequest
        {
            Content = RequireString(args, "content"),
            Title = GetString(args, "title"),
            SourceType = GetString(args, "sourceType"),
            SourceKey = GetString(args, "sourceKey"),
            TriggerReindex = GetBool(args, "triggerReindex") ?? false,
        };
    }

    private static GraphEntityRequest BuildGraphEntityRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new GraphEntityRequest
        {
            Name = RequireString(args, "name"),
            EntityType = RequireString(args, "entityType"),
            Description = GetString(args, "description"),
            Metadata = GetString(args, "metadata"),
        };
    }

    private static GraphRelationshipRequest BuildGraphRelationshipRequest(IReadOnlyDictionary<string, object?> args)
    {
        return new GraphRelationshipRequest
        {
            SourceEntityId = RequireString(args, "sourceEntityId"),
            TargetEntityId = RequireString(args, "targetEntityId"),
            RelationshipType = RequireString(args, "relationshipType"),
            Description = GetString(args, "description"),
            Weight = GetDouble(args, "weight") ?? 1.0,
            Metadata = GetString(args, "metadata"),
        };
    }

    private static IReadOnlyDictionary<string, RequirementsIngestDocument>? GetRequirementsDocumentMap(IReadOnlyDictionary<string, object?> args)
    {
        if (!args.TryGetValue("documents", out var value) || value is null)
        {
            return null;
        }

        if (value is IReadOnlyDictionary<string, RequirementsIngestDocument> typed)
        {
            return typed;
        }

        if (value is not System.Collections.IDictionary rawMap)
        {
            return null;
        }

        var documents = new Dictionary<string, RequirementsIngestDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in rawMap)
        {
            var path = Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            documents[path] = ConvertRequirementDocument(entry.Value);
        }

        return documents.Count == 0 ? null : documents;
    }

    private static RequirementsIngestDocument ConvertRequirementDocument(object? value)
    {
        if (value is RequirementsIngestDocument typed)
        {
            return typed;
        }

        if (value is string textContent)
        {
            return new RequirementsIngestDocument { Content = textContent };
        }

        if (value is not System.Collections.IDictionary fields)
        {
            return new RequirementsIngestDocument { Content = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) };
        }

        var content = GetField(fields, "content");
        var contentBase64 = GetField(fields, "contentBase64");
        var lastModifiedRaw = GetField(fields, "lastModifiedUtc");
        var lastModifiedUtc = DateTimeOffset.TryParse(lastModifiedRaw, out var parsed)
            ? parsed.ToUniversalTime()
            : (DateTimeOffset?)null;

        return new RequirementsIngestDocument
        {
            Content = content,
            ContentBase64 = contentBase64,
            LastModifiedUtc = lastModifiedUtc
        };
    }

    private static string? GetField(System.Collections.IDictionary fields, string name)
    {
        foreach (System.Collections.DictionaryEntry entry in fields)
        {
            if (entry.Key?.ToString()?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
            {
                return Convert.ToString(entry.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetStringList(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is string single)
        {
            return string.IsNullOrWhiteSpace(single)
                ? Array.Empty<string>()
                : new[] { single };
        }

        if (value is IEnumerable<object?> values)
        {
            return values
                .Select(v => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToArray();
        }

        if (value is IEnumerable rawValues)
        {
            return rawValues
                .Cast<object?>()
                .Select(v => Convert.ToString(NormalizeJsonElement(v), System.Globalization.CultureInfo.InvariantCulture))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToArray();
        }

        return new[] { Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    private static IReadOnlyList<string>? GetOptionalStringList(IReadOnlyDictionary<string, object?> args, string name)
        => args.ContainsKey(name) ? GetStringList(args, name) : null;

    private static IReadOnlyDictionary<string, string>? GetStringMap(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        var raw = ToStringObjectDictionary(value);
        if (raw is null || raw.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in raw)
        {
            var text = Convert.ToString(NormalizeJsonElement(pair.Value), System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(text))
            {
                result[pair.Key] = text!;
            }
        }

        return result.Count == 0 ? null : result;
    }

    private static IReadOnlyList<ITodoSubtask> GetTodoSubtasks(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
        {
            return Array.Empty<ITodoSubtask>();
        }

        if (value is string single)
        {
            return string.IsNullOrWhiteSpace(single)
                ? Array.Empty<ITodoSubtask>()
                : new ITodoSubtask[] { new TodoSubtaskAdapter { Task = single, Done = false } };
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element
                .EnumerateArray()
                .Select(v => ConvertTodoSubtask(v))
                .Where(v => v is not null)
                .Select(v => v!)
                .ToArray();
        }

        if (value is IEnumerable values)
        {
            return values
                .Cast<object?>()
                .Select(ConvertTodoSubtask)
                .Where(v => v is not null)
                .Select(v => v!)
                .ToArray();
        }

        var task = ConvertTodoSubtask(value);
        return task is null ? Array.Empty<ITodoSubtask>() : new[] { task };
    }

    private static IReadOnlyList<ITodoSubtask>? GetOptionalTodoSubtasks(IReadOnlyDictionary<string, object?> args, string name)
        => args.ContainsKey(name) ? GetTodoSubtasks(args, name) : null;

    private static ITodoSubtask? ConvertTodoSubtask(object? value)
    {
        value = NormalizeJsonElement(value);
        if (value is null)
        {
            return null;
        }

        if (value is ITodoSubtask typed)
        {
            return typed;
        }

        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? null
                : new TodoSubtaskAdapter { Task = text, Done = false };
        }

        var fields = ToStringObjectDictionary(value);
        if (fields is null)
        {
            var fallbackText = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(fallbackText)
                ? null
                : new TodoSubtaskAdapter { Task = fallbackText, Done = false };
        }

        return new TodoSubtaskAdapter
        {
            Task = RequireString(fields, "task"),
            Done = GetBool(fields, "done") ?? false,
        };
    }

    private static async Task<IReadOnlyList<IStreamingEvent>> CollectTodoEventsAsync(
        string requestId,
        string method,
        Func<Func<IStreamingEvent, Task>, CancellationToken, Task> stream,
        Func<IYamlEnvelope, Task>? emitEnvelopeAsync,
        CancellationToken cancellationToken)
    {
        var events = new List<IStreamingEvent>();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ResolveStreamCommandTimeout());

        try
        {
            await stream(async evt =>
            {
                events.Add(evt);
                if (emitEnvelopeAsync is not null)
                {
                    await emitEnvelopeAsync(BuildTodoStreamEventEnvelope(requestId, method, evt)).ConfigureAwait(false);
                }
            }, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (events.Count == 0 || !events[^1].EventType.EndsWith(".cancelled", StringComparison.OrdinalIgnoreCase))
            {
                var timeoutEvent = new DispatcherStreamingEvent(
                    "stream.timeout",
                    new Dictionary<string, object?>
                    {
                        ["requestId"] = requestId,
                        ["message"] = $"Stream command timed out after {ResolveStreamCommandTimeout().TotalSeconds:0} seconds.",
                    },
                    DateTimeOffset.UtcNow,
                    events.Count + 1);
                events.Add(timeoutEvent);
                if (emitEnvelopeAsync is not null)
                {
                    await emitEnvelopeAsync(BuildTodoStreamEventEnvelope(requestId, method, timeoutEvent)).ConfigureAwait(false);
                }
            }
        }

        return events;
    }

    private static TimeSpan ResolveStreamCommandTimeout()
    {
        var configured = Environment.GetEnvironmentVariable(StreamCommandTimeoutEnvVar);
        if (int.TryParse(configured, out var seconds) && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return DefaultStreamCommandTimeout;
    }

    private static IYamlEnvelope BuildTodoStreamEventEnvelope(string requestId, string method, IStreamingEvent evt)
        => new YamlEnvelope
        {
            Type = "event",
            Payload = new EventPayload
            {
                Event = method,
                Data = new Dictionary<string, object?>
                {
                    ["requestId"] = requestId,
                    ["eventType"] = evt.EventType,
                    ["sequence"] = evt.Sequence,
                    ["data"] = evt.Data,
                },
                Timestamp = evt.Timestamp,
            },
        };

    private sealed class DispatcherStreamingEvent : IStreamingEvent
    {
        public DispatcherStreamingEvent(string eventType, object? data, DateTimeOffset timestamp, int sequence)
        {
            EventType = eventType;
            Data = data;
            Timestamp = timestamp;
            Sequence = sequence;
        }

        public string EventType { get; }
        public object? Data { get; }
        public DateTimeOffset Timestamp { get; }
        public int Sequence { get; }
    }

    private static async Task<object> DeleteAndReturnAsync(Func<Task> delete, string id)
    {
        await delete().ConfigureAwait(false);
        return new Dictionary<string, object?>
        {
            ["deleted"] = true,
            ["id"] = id,
        };
    }

    private async Task<object> DeleteMappingAndReturnAsync(string? frId, string? trId, string? testId, CancellationToken cancellationToken)
    {
        if (_requirementsWorkflow is null)
        {
            throw new InvalidOperationException("Requirements workflow is not registered.");
        }

        await _requirementsWorkflow.DeleteMappingAsync(frId, trId, testId, cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, object?>
        {
            ["deleted"] = true,
            ["frId"] = frId,
            ["trId"] = trId,
            ["testId"] = testId,
        };
    }

    internal sealed class DialogItemAdapter : IDialogItem
    {
        public DateTimeOffset Timestamp { get; init; }

        public string Role { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;
    }

    internal sealed class SessionActionAdapter : ISessionAction
    {
        public int Order { get; init; }

        public string Description { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public string FilePath { get; init; } = string.Empty;
    }

    private sealed class TodoCreateRequestAdapter : ITodoCreateRequest
    {
        public string Id { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Section { get; init; } = string.Empty;

        public string Priority { get; init; } = string.Empty;

        public string? Estimate { get; init; }

        public IReadOnlyList<string>? Description { get; init; }

        public IReadOnlyList<string>? TechnicalDetails { get; init; }

        public IReadOnlyList<ITodoSubtask>? ImplementationTasks { get; init; }

        public string? Note { get; init; }

        public string? Remaining { get; init; }

        public IReadOnlyList<string>? DependsOn { get; init; }

        public IReadOnlyList<string>? FunctionalRequirements { get; init; }

        public IReadOnlyList<string>? TechnicalRequirements { get; init; }
    }

    private sealed class TodoUpdateRequestAdapter : ITodoUpdateRequest
    {
        public string? Title { get; init; }

        public string? Priority { get; init; }

        public string? Section { get; init; }

        public bool? Done { get; init; }

        public string? Estimate { get; init; }

        public IReadOnlyList<string>? Description { get; init; }

        public IReadOnlyList<string>? TechnicalDetails { get; init; }

        public IReadOnlyList<ITodoSubtask>? ImplementationTasks { get; init; }

        public string? Note { get; init; }

        public string? CompletedDate { get; init; }

        public string? DoneSummary { get; init; }

        public string? Remaining { get; init; }

        public IReadOnlyList<string>? DependsOn { get; init; }

        public IReadOnlyList<string>? FunctionalRequirements { get; init; }

        public IReadOnlyList<string>? TechnicalRequirements { get; init; }
    }

    private sealed class TodoSubtaskAdapter : ITodoSubtask
    {
        public string Task { get; init; } = string.Empty;

        public bool Done { get; init; }
    }

    private static IYamlEnvelope BuildHelloResponse(IHelloPayload? request)
    {
        var capabilities = new List<string> { "client-passthrough" };
        if (request?.Capabilities is not null)
        {
            capabilities.AddRange(request.Capabilities);
        }

        return new YamlEnvelope
        {
            Type = "hello",
            Payload = new HelloPayload
            {
                ProtocolVersion = ServerProtocolVersion,
                Capabilities = capabilities,
            },
        };
    }

    private static IYamlEnvelope BuildUnsupportedBatchEnvelopeError(object? payload)
    {
        return BuildError(
            requestId: TryGetBatchRequestId(payload) ?? "unknown",
            code: "unsupported_batch_envelope",
            message: "Batch envelopes are not supported by agent-stdio. Send one request envelope per YAML document, separated by '---'.",
            details: new Dictionary<string, object?>
            {
                ["unsupportedType"] = "batch",
                ["supportedEnvelopeTypes"] = new[] { "hello", "request" },
                ["supportedMultiRequestShape"] = "YAML stream with one envelope per document separated by '---'.",
            });
    }

    private static string? TryGetBatchRequestId(object? payload)
    {
        var payloadDictionary = ToStringObjectDictionary(payload);
        if (payloadDictionary is null)
        {
            return null;
        }

        if (TryGetDictionaryString(payloadDictionary, "requestId", out var directRequestId))
        {
            return directRequestId;
        }

        if (!payloadDictionary.TryGetValue("requests", out var requests) &&
            !payloadDictionary.TryGetValue("envelopes", out requests))
        {
            return null;
        }

        if (requests is IEnumerable requestItems and not string)
        {
            foreach (var item in requestItems)
            {
                var requestDictionary = ToStringObjectDictionary(item);
                if (TryGetDictionaryString(requestDictionary, "requestId", out var requestId))
                {
                    return requestId;
                }
            }
        }

        return null;
    }

    private static bool TryGetDictionaryString(
        IReadOnlyDictionary<string, object?>? dictionary,
        string key,
        out string? value)
    {
        value = null;
        if (dictionary is null ||
            !dictionary.TryGetValue(key, out var raw) ||
            raw is null)
        {
            return false;
        }

        value = raw.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private IYamlEnvelope? BuildClientMutationPolicyErrorIfRejected(
        string requestId,
        string clientName,
        string methodName,
        IReadOnlyDictionary<string, object?> args)
    {
        var decision = _clientMutationPolicy?.Evaluate(clientName, methodName, args) ?? ClientMutationPolicyDecision.Allow();
        if (decision.Allowed)
            return null;

        return BuildError(
            requestId: requestId,
            code: string.IsNullOrWhiteSpace(decision.ErrorCode) ? "mutation_not_transactional" : decision.ErrorCode,
            message: string.IsNullOrWhiteSpace(decision.Message)
                ? $"Client method client.{clientName}.{methodName} is blocked by mutation policy."
                : decision.Message,
            details: new Dictionary<string, object?>
            {
                ["clientName"] = clientName,
                ["methodName"] = methodName,
                ["policy"] = nameof(IClientMutationPolicy),
            });
    }

    private static IYamlEnvelope BuildError(
        string requestId,
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new YamlEnvelope
        {
            Type = "error",
            Payload = new ErrorPayload
            {
                RequestId = requestId,
                Code = code,
                Message = message,
                Details = details,
            },
        };
    }
}
