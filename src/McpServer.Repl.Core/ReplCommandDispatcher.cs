// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Server-side command dispatcher
// FR-MCP-REPL-003: Command Namespace Parity - Request routing to client passthrough
// TR-MCP-REPL-004: Command Registry and Dispatcher - Envelope-to-handler routing
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes

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
/// Default <see cref="IReplCommandDispatcher"/> implementation. Routes <c>hello</c> envelopes
/// to a handshake response and <c>request</c> envelopes with the <c>client.&lt;clientName&gt;.&lt;methodName&gt;</c>
/// method shape to <see cref="IGenericClientPassthrough.InvokeAsync"/>. All other method
/// namespaces produce a <c>method_not_found</c> error envelope.
/// </summary>
public sealed class ReplCommandDispatcher : IReplCommandDispatcher
{
    private const string ServerProtocolVersion = "1.0";
    private readonly IGenericClientPassthrough _passthrough;
    private readonly IRequirementsWorkflow? _requirementsWorkflow;

    /// <summary>
    /// Initializes a new <see cref="ReplCommandDispatcher"/>.
    /// </summary>
    /// <param name="passthrough">The generic client passthrough used to invoke <c>client.*.*</c> methods.</param>
    /// <param name="requirementsWorkflow">The optional requirements workflow used to invoke <c>workflow.requirements.*</c> methods.</param>
    public ReplCommandDispatcher(IGenericClientPassthrough passthrough, IRequirementsWorkflow? requirementsWorkflow = null)
    {
        _passthrough = passthrough ?? throw new ArgumentNullException(nameof(passthrough));
        _requirementsWorkflow = requirementsWorkflow;
    }

    /// <inheritdoc />
    public async Task<IYamlEnvelope> DispatchAsync(IYamlEnvelope envelope, CancellationToken cancellationToken)
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
                return await DispatchRequestAsync(request, cancellationToken).ConfigureAwait(false);

            default:
                return BuildError(
                    requestId: "unknown",
                    code: "invalid_envelope",
                    message: $"Unsupported envelope type: {envelope.Type}");
        }
    }

    private async Task<IYamlEnvelope> DispatchRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        var method = request.Method ?? "";

        if (method.StartsWith("client.", StringComparison.Ordinal))
        {
            return await DispatchClientRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (method.StartsWith(RequirementsCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            return await DispatchRequirementsRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return BuildError(
            requestId: request.RequestId,
            code: "method_not_found",
            message: $"Method '{method}' is not routed by this dispatcher. " +
                     $"Supported namespaces: client.<clientName>.<methodName>, {RequirementsCommandShapes.MethodNamespace}.*.");
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
                RequirementsCommandShapes.DeleteTestMethod =>
                    await DeleteAndReturnAsync(() => _requirementsWorkflow.DeleteTestAsync(RequireString(args, "id"), cancellationToken), RequireString(args, "id")).ConfigureAwait(false),
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
                        RequireString(args, "content"),
                        GetString(args, "format") ?? "markdown",
                        GetString(args, "mergeStrategy") ?? "merge",
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

    private static string? GetString(IReadOnlyDictionary<string, object?> args, string name)
    {
        return args.TryGetValue(name, out var value) && value is not null
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
            : null;
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

        return new[] { Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
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
