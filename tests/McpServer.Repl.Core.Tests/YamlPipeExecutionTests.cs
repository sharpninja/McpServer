// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Pipe execution acceptance
// FR-MCP-REPL-002: REPL Lifecycle Management - Multi-line YAML framing and dispatch
// TR-MCP-REPL-001: YAML Envelope Protocol - Production serializer round-trip
// TR-MCP-REPL-003: Command Loop Lifecycle - Multi-line accumulation and dispatch
// TR-MCP-REPL-004: Command Registry and Dispatcher - Request routing
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes end-to-end

using System.Text;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Acceptance tests for YAML pipe execution in the REPL agent-stdio mode.
/// Drives the fix for the reported bug where piped YAML was echoed instead of executed.
/// Tests cover: production YamlSerializer round-trip, ReplCommandDispatcher routing (hello,
/// client.*.*, unknown method), and AgentStdioProtocol multi-line framing with dispatch.
/// Uses NSubstitute for IGenericClientPassthrough; all other components are real implementations.
/// Validates TEST-MCP-REPL-001: well-formed YAML envelopes are parsed and dispatched rather
/// than echoed.
/// </summary>
public class YamlPipeExecutionTests
{
    // ---------------------------------------------------------------
    // YamlSerializer (production) round-trip tests
    // ---------------------------------------------------------------

    /// <summary>
    /// Deserializing a multi-line hello envelope yields an envelope with Type="hello"
    /// and a HelloPayload carrying the declared protocol version.
    /// Validates the production serializer parses the shape documented in
    /// docs/REPL-AGENT-GUIDE.md rather than echoing raw text.
    /// </summary>
    [Fact]
    public void YamlSerializer_Deserialize_HelloEnvelope_ReturnsTypedPayload()
    {
        var sut = new YamlSerializer();
        var yaml = "type: hello\npayload:\n  protocolVersion: \"1.0\"\n  capabilities:\n    - auth\n    - workspace-multi\n";

        var envelope = sut.Deserialize(yaml);

        Assert.Equal("hello", envelope.Type);
        var hello = Assert.IsAssignableFrom<IHelloPayload>(envelope.Payload);
        Assert.Equal("1.0", hello.ProtocolVersion);
        Assert.NotNull(hello.Capabilities);
        Assert.Contains("auth", hello.Capabilities!);
        Assert.Contains("workspace-multi", hello.Capabilities!);
    }

    /// <summary>
    /// Deserializing a request envelope yields a RequestPayload with the method name,
    /// request id, and parameter dictionary extracted from the YAML body.
    /// Validates that params are not dropped during parse.
    /// </summary>
    [Fact]
    public void YamlSerializer_Deserialize_RequestEnvelope_ReturnsTypedPayload()
    {
        var sut = new YamlSerializer();
        var yaml = "type: request\npayload:\n  requestId: req-001\n  method: client.todo.QueryAsync\n  params:\n    keyword: auth\n    done: false\n";

        var envelope = sut.Deserialize(yaml);

        Assert.Equal("request", envelope.Type);
        var request = Assert.IsAssignableFrom<IRequestPayload>(envelope.Payload);
        Assert.Equal("req-001", request.RequestId);
        Assert.Equal("client.todo.QueryAsync", request.Method);
        Assert.NotNull(request.Params);
        Assert.Equal("auth", request.Params!["keyword"]);
        Assert.Equal("false", request.Params!["done"]?.ToString());
    }

    /// <summary>
    /// Serializing a result envelope produces YAML with "type: result" and the request id.
    /// This is the shape emitted back to stdout after dispatch, replacing the bug's JSON echo.
    /// </summary>
    [Fact]
    public void YamlSerializer_Serialize_ResultEnvelope_ProducesExpectedYaml()
    {
        var sut = new YamlSerializer();
        var envelope = new YamlEnvelope
        {
            Type = "result",
            Payload = new ResultPayload
            {
                RequestId = "req-001",
                Result = new Dictionary<string, object?> { ["ok"] = true }
            }
        };

        var yaml = sut.Serialize(envelope);

        Assert.Contains("type: result", yaml);
        Assert.Contains("requestId: req-001", yaml);
    }

    // ---------------------------------------------------------------
    // ReplCommandDispatcher routing tests
    // ---------------------------------------------------------------

    /// <summary>
    /// A hello envelope is answered with a result envelope whose payload echoes the server's
    /// declared protocol version. Validates the handshake does not fall through to echo.
    /// </summary>
    [Fact]
    public async Task Dispatcher_HelloEnvelope_ReturnsHelloResult()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var sut = new ReplCommandDispatcher(passthrough);

        var envelope = new YamlEnvelope
        {
            Type = "hello",
            Payload = new HelloPayload
            {
                ProtocolVersion = "1.0",
                Capabilities = new[] { "auth" }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("hello", response.Type);
        var hello = Assert.IsAssignableFrom<IHelloPayload>(response.Payload);
        Assert.Equal("1.0", hello.ProtocolVersion);
    }

    /// <summary>
    /// A client.* request is routed to IGenericClientPassthrough with the parsed client name,
    /// method name, and argument dictionary. The passthrough's return value becomes the result
    /// envelope's Result.
    /// </summary>
    [Fact]
    public async Task Dispatcher_ClientRequest_DelegatesToPassthrough()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync("todo", "QueryAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new { totalCount = 3 }));

        var sut = new ReplCommandDispatcher(passthrough);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-42",
                Method = "client.todo.QueryAsync",
                Params = new Dictionary<string, object?> { ["keyword"] = "auth" }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        var result = Assert.IsAssignableFrom<IResultPayload>(response.Payload);
        Assert.Equal("req-42", result.RequestId);
        Assert.NotNull(result.Result);

        await passthrough.Received(1).InvokeAsync(
            "todo",
            "QueryAsync",
            Arg.Is<Dictionary<string, object?>>(d => DictionaryContainsValue(d, "keyword", "auth")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Requirements batch commands are schema-validated by the dispatcher before invoking
    /// the requirements workflow, preventing empty or malformed records arrays from reaching
    /// endpoint-backed workflow methods.
    /// </summary>
    [Fact]
    public async Task Dispatcher_InvalidRequirementsBatch_ReturnsSchemaValidationFailed()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-batch-invalid",
                Method = RequirementsCommandShapes.CreateFrBatchMethod,
                Params = new Dictionary<string, object?>
                {
                    ["records"] = Array.Empty<object>(),
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("error", response.Type);
        var err = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("schema_validation_failed", err.Code);
        Assert.NotNull(err.Details);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(err.Details!["errors"]);
        Assert.Contains(errors, error => error.Contains("records", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A valid requirements batch request survives schema validation and is converted to the
    /// typed batch request DTO before the workflow is invoked.
    /// </summary>
    [Fact]
    public async Task Dispatcher_ValidRequirementsBatch_DelegatesToRequirementsWorkflow()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        requirements
            .CreateFrBatchAsync(Arg.Any<CreateFrBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RequirementsBatchResult
            {
                Success = true,
                Operation = "create",
                Kind = "fr",
                Total = 1,
            }));
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-batch-valid",
                Method = RequirementsCommandShapes.CreateFrBatchMethod,
                Params = new Dictionary<string, object?>
                {
                    ["records"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["id"] = "FR-MCP-001",
                            ["title"] = "Batch FR",
                            ["body"] = "Create requirements in batches.",
                        },
                    },
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await requirements.Received(1).CreateFrBatchAsync(
            Arg.Is<CreateFrBatchRequest>(request =>
                request != null &&
                request.Records.Count == 1 &&
                request.Records[0].Id == "FR-MCP-001" &&
                request.Records[0].Title == "Batch FR" &&
                request.Records[0].Body == "Create requirements in batches."),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies FR batch update dispatch preserves structured acceptance criteria.
    /// </summary>
    [Fact]
    public async Task Dispatcher_UpdateFrBatch_PreservesAcceptanceCriteria()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        requirements
            .UpdateFrBatchAsync(Arg.Any<UpdateFrBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RequirementsBatchResult
            {
                Success = true,
                Operation = "update",
                Kind = "fr",
                Total = 1,
            }));
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-batch-update-ac",
                Method = RequirementsCommandShapes.UpdateFrBatchMethod,
                Params = new Dictionary<string, object?>
                {
                    ["records"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["id"] = "FR-MCP-113",
                            ["title"] = "Plugin requirement batch payload parsing",
                            ["description"] = "The system SHALL preserve nested acceptance criteria in requirement batch updates.",
                            ["acceptanceCriteria"] = new[]
                            {
                                new Dictionary<string, object?>
                                {
                                    ["id"] = "FR-MCP-113-AC001",
                                    ["text"] = "Batch update preserves nested acceptance criteria.",
                                    ["isSatisfied"] = false,
                                },
                            },
                        },
                    },
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await requirements.Received(1).UpdateFrBatchAsync(
            Arg.Is<UpdateFrBatchRequest>(request =>
                request != null &&
                request.Records.Count == 1 &&
                request.Records[0].Id == "FR-MCP-113" &&
                request.Records[0].AcceptanceCriteria != null &&
                request.Records[0].AcceptanceCriteria!.Count == 1 &&
                request.Records[0].AcceptanceCriteria![0].Id == "FR-MCP-113-AC001" &&
                request.Records[0].AcceptanceCriteria![0].Text == "Batch update preserves nested acceptance criteria." &&
                request.Records[0].AcceptanceCriteria![0].IsSatisfied == false),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies YAML request envelopes preserve nested acceptance criteria booleans in typed batch dispatch.
    /// </summary>
    [Fact]
    public async Task AgentStdioProtocol_UpdateFrBatchYaml_PreservesAcceptanceCriteriaBoolean()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        requirements
            .UpdateFrBatchAsync(Arg.Any<UpdateFrBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RequirementsBatchResult
            {
                Success = true,
                Operation = "update",
                Kind = "fr",
                Total = 1,
            }));
        var sut = new AgentStdioProtocol(new YamlSerializer(), new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements));

        var input = new StringBuilder()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-batch-update-yaml-ac")
            .AppendLine("  method: workflow.requirements.updateFrBatch")
            .AppendLine("  params:")
            .AppendLine("    records:")
            .AppendLine("    - id: FR-MCP-113")
            .AppendLine("      title: Plugin requirement batch payload parsing")
            .AppendLine("      description: The system SHALL preserve nested acceptance criteria in requirement batch updates.")
            .AppendLine("      acceptanceCriteria:")
            .AppendLine("      - id: FR-MCP-113-AC001")
            .AppendLine("        text: Batch update preserves nested acceptance criteria.")
            .AppendLine("        isSatisfied: false")
            .AppendLine()
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("type: result", output, StringComparison.Ordinal);
        await requirements.Received(1).UpdateFrBatchAsync(
            Arg.Is<UpdateFrBatchRequest>(request =>
                request != null &&
                request.Records.Count == 1 &&
                request.Records[0].AcceptanceCriteria != null &&
                request.Records[0].AcceptanceCriteria!.Count == 1 &&
                request.Records[0].AcceptanceCriteria![0].IsSatisfied == false),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A workflow.sessionlog.importRecovery request is routed to the session-log workflow
    /// with the recovered turns parsed from YAML params. This route performs the safe
    /// query-merge-submit behavior in the workflow layer.
    /// </summary>
    [Fact]
    public async Task Dispatcher_SessionLogImportRecovery_DelegatesToSessionLogWorkflow()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var sessionLog = Substitute.For<ISessionLogWorkflow>();
        sessionLog.ImportRecoveryAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SessionLogRecoveryImportResult
            {
                SourceType = "Codex",
                SessionId = "Codex-20260514T000000Z-recovery",
                ImportedTurns = 1,
                TotalTurns = 1
            }));

        var sut = new ReplCommandDispatcher(passthrough, sessionLogWorkflow: sessionLog);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-sessionlog-import",
                Method = SessionLogCommandShapes.ImportRecoveryMethod,
                Params = new Dictionary<string, object?>
                {
                    ["sourceType"] = "Codex",
                    ["sessionId"] = "Codex-20260514T000000Z-recovery",
                    ["title"] = "Recovered session",
                    ["model"] = "gpt-5",
                    ["turns"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["requestId"] = "req-20260514T000100Z-imported",
                            ["timestamp"] = "2026-05-14T00:01:00Z",
                            ["queryTitle"] = "Imported",
                            ["queryText"] = "Imported text",
                            ["status"] = "completed",
                        }
                    },
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await sessionLog.Received(1).ImportRecoveryAsync(
            Arg.Is<UnifiedSessionLogDto>(dto =>
                dto != null &&
                dto.SourceType == "Codex" &&
                dto.SessionId == "Codex-20260514T000000Z-recovery" &&
                dto.Turns != null &&
                dto.Turns.Count == 1 &&
                dto.Turns[0].RequestId == "req-20260514T000100Z-imported"),
            Arg.Any<CancellationToken>());
        await passthrough.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, default!, default);
    }

    /// <summary>
    /// A workflow.todo.query request is routed through the typed TODO workflow instead of
    /// falling through to the generic client passthrough. This keeps YAML REPL callers on the
    /// same command-shape contract as in-process MCP tools.
    /// </summary>
    [Fact]
    public async Task Dispatcher_TodoQueryRequest_DelegatesToTodoWorkflow()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var todo = Substitute.For<ITodoWorkflow>();
        var queryResult = Substitute.For<ITodoQueryResult>();
        queryResult.TotalCount.Returns(0);
        queryResult.Items.Returns(Array.Empty<ITodoItem>());
        todo.QueryAsync("auth", "high", "Backlog", "MCP-TODO-001", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(queryResult));

        var sut = new ReplCommandDispatcher(passthrough, todoWorkflow: todo);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-todo-query",
                Method = TodoCommandShapes.QueryMethod,
                Params = new Dictionary<string, object?>
                {
                    ["keyword"] = "auth",
                    ["priority"] = "high",
                    ["section"] = "Backlog",
                    ["id"] = "MCP-TODO-001",
                    ["done"] = "false",
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await todo.Received(1).QueryAsync("auth", "high", "Backlog", "MCP-TODO-001", false, Arg.Any<CancellationToken>());
        await passthrough.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, default!, default);
    }

    /// <summary>
    /// A flat workflow.todo.create request is normalized into the typed create-request
    /// interface before dispatch. YAML callers do not have to know the client method's
    /// internal <c>request</c> parameter name.
    /// </summary>
    [Fact]
    public async Task Dispatcher_TodoCreateRequest_AcceptsFlatYamlShape()
    {
        var todo = Substitute.For<ITodoWorkflow>();
        var mutation = CreateMutationResult();
        todo.CreateAsync(Arg.Any<ITodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutation));

        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), todoWorkflow: todo);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-todo-create",
                Method = TodoCommandShapes.CreateMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "MCP-TODO-001",
                    ["title"] = "Fix TODO contract",
                    ["section"] = "Backlog",
                    ["priority"] = "high",
                    ["description"] = new[] { "route workflow.todo.* through TODO workflow" },
                    ["implementationTasks"] = new object[]
                    {
                        new Dictionary<string, object?> { ["task"] = "Add dispatcher route", ["done"] = true },
                        "Add plugin regression",
                    },
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await todo.Received(1).CreateAsync(
            Arg.Is<ITodoCreateRequest>(request =>
                request != null &&
                request.Id == "MCP-TODO-001" &&
                request.Title == "Fix TODO contract" &&
                request.Section == "Backlog" &&
                request.Priority == "high" &&
                request.Description != null &&
                request.Description!.SequenceEqual(new[] { "route workflow.todo.* through TODO workflow" }) &&
                request.ImplementationTasks != null &&
                request.ImplementationTasks!.Count == 2 &&
                request.ImplementationTasks![0].Task == "Add dispatcher route" &&
                request.ImplementationTasks![0].Done &&
                request.ImplementationTasks![1].Task == "Add plugin regression" &&
                !request.ImplementationTasks![1].Done),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A nested <c>request:</c> workflow.todo.update request is normalized into the typed update
    /// request while retaining the top-level TODO id. Folded YAML scalars arrive as ordinary
    /// strings and must stay that way.
    /// </summary>
    [Fact]
    public async Task Dispatcher_TodoUpdateRequest_AcceptsNestedRequestShape()
    {
        var todo = Substitute.For<ITodoWorkflow>();
        var mutation = CreateMutationResult();
        todo.UpdateAsync("MCP-TODO-001", Arg.Any<ITodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutation));

        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), todoWorkflow: todo);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-todo-update",
                Method = TodoCommandShapes.UpdateMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "MCP-TODO-001",
                    ["request"] = new Dictionary<string, object?>
                    {
                        ["done"] = true,
                        ["doneSummary"] = "Dispatcher accepts nested request shape.",
                        ["implementationTasks"] = new[]
                        {
                            new Dictionary<string, object?> { ["task"] = "Verify YAML contract", ["done"] = "true" },
                        },
                    },
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await todo.Received(1).UpdateAsync(
            "MCP-TODO-001",
            Arg.Is<ITodoUpdateRequest>(request =>
                request != null &&
                request.Done == true &&
                request.DoneSummary == "Dispatcher accepts nested request shape." &&
                request.ImplementationTasks != null &&
                request.ImplementationTasks!.Count == 1 &&
                request.ImplementationTasks![0].Task == "Verify YAML contract" &&
                request.ImplementationTasks![0].Done),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// JSON flow style is valid YAML. The production YAML parser must feed workflow.todo.create
    /// with the same data as block-style YAML so callers can use either representation.
    /// </summary>
    [Fact]
    public async Task Dispatcher_TodoCreateRequest_AcceptsJsonFlowStyleYaml()
    {
        var todo = Substitute.For<ITodoWorkflow>();
        var mutation = CreateMutationResult();
        todo.CreateAsync(Arg.Any<ITodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutation));

        var serializer = new YamlSerializer();
        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), todoWorkflow: todo);
        var envelope = serializer.Deserialize("""
            type: request
            payload:
              requestId: req-todo-create-flow
              method: workflow.todo.create
              params: { id: MCP-TODO-002, title: Flow style create, section: Backlog, priority: medium, implementationTasks: [{ task: Normalize flow style, done: false }] }
            """);

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await todo.Received(1).CreateAsync(
            Arg.Is<ITodoCreateRequest>(request =>
                request != null &&
                request.Id == "MCP-TODO-002" &&
                request.Title == "Flow style create" &&
                request.ImplementationTasks != null &&
                request.ImplementationTasks!.Count == 1 &&
                request.ImplementationTasks![0].Task == "Normalize flow style" &&
                !request.ImplementationTasks![0].Done),
            Arg.Any<CancellationToken>());
    }

    private static bool DictionaryContainsValue(Dictionary<string, object?>? dict, string key, object expected)
    {
        return dict is not null && dict.TryGetValue(key, out var value) && Equals(value, expected);
    }

    private static ITodoMutationResult CreateMutationResult()
    {
        var item = Substitute.For<ITodoItem>();
        item.Id.Returns("MCP-TODO-001");
        item.Title.Returns("Fix TODO contract");
        item.Section.Returns("Backlog");
        item.Priority.Returns("high");
        item.Done.Returns(false);
        item.Description.Returns(Array.Empty<string>());
        item.TechnicalDetails.Returns(Array.Empty<string>());
        item.ImplementationTasks.Returns(Array.Empty<ITodoSubtask>());
        item.DependsOn.Returns(Array.Empty<string>());
        item.FunctionalRequirements.Returns(Array.Empty<string>());
        item.TechnicalRequirements.Returns(Array.Empty<string>());

        var result = Substitute.For<ITodoMutationResult>();
        result.Success.Returns(true);
        result.Item.Returns(item);
        return result;
    }

    /// <summary>
    /// A request with an unsupported method namespace (not <c>client.*</c> and not a built-in)
    /// returns an error envelope with code <c>method_not_found</c> and the original request id,
    /// so callers can correlate the failure.
    /// </summary>
    [Fact]
    public async Task Dispatcher_UnknownMethod_ReturnsMethodNotFoundError()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var sut = new ReplCommandDispatcher(passthrough);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-99",
                Method = "bogus.namespace.DoSomething",
                Params = null
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("error", response.Type);
        var err = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("req-99", err.RequestId);
        Assert.Equal("method_not_found", err.Code);
    }

    /// <summary>
    /// A batch envelope returns an actionable protocol error instead of a generic unsupported
    /// envelope response. The diagnostic preserves the first nested request id when present and
    /// points callers at the supported YAML stream shape.
    /// </summary>
    [Fact]
    public async Task Dispatcher_BatchEnvelope_ReturnsActionableUnsupportedBatchError()
    {
        var serializer = new YamlSerializer();
        var envelope = serializer.Deserialize("""
            type: batch
            payload:
              requests:
                - requestId: req-batch-001
                  method: client.todo.QueryAsync
                  params:
                    keyword: auth
            """);
        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>());

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("error", response.Type);
        var err = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("req-batch-001", err.RequestId);
        Assert.Equal("unsupported_batch_envelope", err.Code);
        Assert.Contains("agent-stdio", err.Message);
        Assert.Contains("---", err.Message);
        Assert.NotNull(err.Details);
        Assert.True(err.Details!.ContainsKey("supportedMultiRequestShape"));
    }

    /// <summary>
    /// When the passthrough throws, the dispatcher wraps the failure in an error envelope
    /// carrying the original request id and code <c>method_invocation_error</c> — it must not
    /// let the exception escape past the dispatch boundary, so the agent loop stays alive.
    /// </summary>
    [Fact]
    public async Task Dispatcher_ClientRequestThrows_ReturnsErrorEnvelope()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns<Task<object?>>(_ => throw new InvalidOperationException("boom"));

        var sut = new ReplCommandDispatcher(passthrough);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-7",
                Method = "client.context.SearchAsync",
                Params = new Dictionary<string, object?> { ["query"] = "test" }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("error", response.Type);
        var err = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("req-7", err.RequestId);
        Assert.Equal("method_invocation_error", err.Code);
        Assert.Contains("boom", err.Message);
    }

    // ---------------------------------------------------------------
    // AgentStdioProtocol multi-line framing and end-to-end dispatch
    // ---------------------------------------------------------------

    /// <summary>
    /// A single multi-line YAML request piped on stdin must be accumulated into one complete
    /// envelope, parsed, and dispatched — not processed line-by-line. The output stream must
    /// contain exactly one response envelope, not one echo per line. This is the primary
    /// acceptance test for the bug described as "YAML pipe is being echoed back, not executed."
    /// </summary>
    [Fact]
    public async Task AgentStdioProtocol_MultiLineRequestTerminatedByBlankLine_DispatchedOnce()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync("todo", "QueryAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new Dictionary<string, object?> { ["totalCount"] = 0 }));

        var dispatcher = new ReplCommandDispatcher(passthrough);
        var serializer = new YamlSerializer();
        var sut = new AgentStdioProtocol(serializer, dispatcher);

        var input = new StringBuilder()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-multi-001")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine("  params:")
            .AppendLine("    keyword: hello")
            .AppendLine() // blank line terminates the document
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("type: result", output);
        Assert.Contains("req-multi-001", output);
        Assert.DoesNotContain("\"type\":\"echo\"", output);

        await passthrough.Received(1).InvokeAsync(
            "todo",
            "QueryAsync",
            Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Two envelopes separated by a blank line are each parsed and dispatched independently.
    /// The resulting output must contain two distinct result envelopes with the correct
    /// matching request ids.
    /// </summary>
    [Fact]
    public async Task AgentStdioProtocol_TwoEnvelopes_DispatchedIndependently()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>("ok"));

        var dispatcher = new ReplCommandDispatcher(passthrough);
        var sut = new AgentStdioProtocol(new YamlSerializer(), dispatcher);

        var input = new StringBuilder()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-a")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-b")
            .AppendLine("  method: client.context.SearchAsync")
            .AppendLine()
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("req-a", output);
        Assert.Contains("req-b", output);
        await passthrough.Received(2).InvokeAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A YAML document stream with explicit <c>---</c> separators is parsed as multiple
    /// envelopes. This is the framing convention used by the existing YamlFramingTests
    /// and FakeYamlSerializer for SerializeStream / DeserializeStream.
    /// </summary>
    [Fact]
    public async Task AgentStdioProtocol_DocumentSeparators_DispatchedIndependently()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>("ok"));

        var sut = new AgentStdioProtocol(new YamlSerializer(), new ReplCommandDispatcher(passthrough));

        var input = new StringBuilder()
            .AppendLine("---")
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-doc-1")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine("---")
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-doc-2")
            .AppendLine("  method: client.context.SearchAsync")
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("req-doc-1", output);
        Assert.Contains("req-doc-2", output);
    }

    /// <summary>
    /// A malformed YAML envelope produces a single error envelope with code
    /// <c>invalid_envelope</c>; the loop must continue so the next envelope can be processed.
    /// </summary>
    [Fact]
    public async Task AgentStdioProtocol_MalformedYaml_WritesErrorAndContinues()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>("ok"));

        var sut = new AgentStdioProtocol(new YamlSerializer(), new ReplCommandDispatcher(passthrough));

        var input = new StringBuilder()
            .AppendLine("type: request")
            .AppendLine("payload: [this is not valid yaml")
            .AppendLine()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-after-error")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine()
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("type: error", output);
        Assert.Contains("req-after-error", output);
    }
}
