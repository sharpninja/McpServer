// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Server-side command dispatcher
// FR-MCP-REPL-003: Command Namespace Parity - Request routing to client passthrough
// TR-MCP-REPL-004: Command Registry and Dispatcher - Envelope-to-handler routing
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    };

    private readonly IGenericClientPassthrough _passthrough;
    private readonly ISessionLogWorkflow? _sessionLogWorkflow;
    private readonly IRequirementsWorkflow? _requirementsWorkflow;
    private readonly ITodoWorkflow? _todoWorkflow;

    /// <summary>
    /// Initializes a new <see cref="ReplCommandDispatcher"/>.
    /// </summary>
    /// <param name="passthrough">The generic client passthrough used to invoke <c>client.*.*</c> methods.</param>
    /// <param name="sessionLogWorkflow">The optional session-log workflow used to invoke <c>workflow.sessionlog.*</c> methods.</param>
    /// <param name="requirementsWorkflow">The optional requirements workflow used to invoke <c>workflow.requirements.*</c> methods.</param>
    /// <param name="todoWorkflow">The optional TODO workflow used to invoke <c>workflow.todo.*</c> methods.</param>
    public ReplCommandDispatcher(
        IGenericClientPassthrough passthrough,
        ISessionLogWorkflow? sessionLogWorkflow = null,
        IRequirementsWorkflow? requirementsWorkflow = null,
        ITodoWorkflow? todoWorkflow = null)
    {
        _passthrough = passthrough ?? throw new ArgumentNullException(nameof(passthrough));
        _sessionLogWorkflow = sessionLogWorkflow;
        _requirementsWorkflow = requirementsWorkflow;
        _todoWorkflow = todoWorkflow;
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

        if (method.StartsWith("client.", StringComparison.Ordinal))
        {
            return await DispatchClientRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (method.StartsWith(RequirementsCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return await DispatchRequirementsRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (method.StartsWith(SessionLogCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return await DispatchSessionLogRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (method.StartsWith(TodoCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return await DispatchTodoRequestAsync(request, emitEnvelopeAsync, cancellationToken).ConfigureAwait(false);
        }

        return BuildError(
            requestId: request.RequestId,
            code: "method_not_found",
            message: $"Method '{method}' is not routed by this dispatcher. " +
                     $"Supported namespaces: client.<clientName>.<methodName>, {SessionLogCommandShapes.MethodNamespace}.*, {RequirementsCommandShapes.MethodNamespace}.*, {TodoCommandShapes.MethodNamespace}.*.");
    }

    private async Task<IYamlEnvelope> DispatchSessionLogRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        if (_sessionLogWorkflow is null)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: "Session-log workflow is not registered.");
        }

        var workflow = _sessionLogWorkflow;
        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

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
                    await workflow.OpenSessionAsync(
                        GetString(args, "agent") ?? GetString(args, "sourceType") ?? "Codex",
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
                        cancellationToken).ConfigureAwait(false);
                    result = new Dictionary<string, object?>
                    {
                        ["requestId"] = RequireString(args, "requestId"),
                        ["status"] = "in_progress",
                    };
                    break;

                case SessionLogCommandShapes.UpdateTurnMethod:
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
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
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
            Description = GetStringList(args, "description"),
            TechnicalDetails = GetStringList(args, "technicalDetails"),
            ImplementationTasks = GetTodoSubtasks(args, "implementationTasks"),
            Note = GetString(args, "note"),
            CompletedDate = GetString(args, "completedDate"),
            DoneSummary = GetString(args, "doneSummary"),
            Remaining = GetString(args, "remaining"),
            DependsOn = GetStringList(args, "dependsOn"),
            FunctionalRequirements = GetStringList(args, "functionalRequirements"),
            TechnicalRequirements = GetStringList(args, "technicalRequirements"),
        };
    }

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

    private sealed class DialogItemAdapter : IDialogItem
    {
        public DateTimeOffset Timestamp { get; init; }

        public string Role { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;
    }

    private sealed class SessionActionAdapter : ISessionAction
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
