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
    /// TEST-MCP-REQAC-001: single FR create dispatch preserves structured acceptance criteria.
    /// </summary>
    [Fact]
    public async Task Dispatcher_CreateFr_PreservesAcceptanceCriteria()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        var mutation = Substitute.For<IFrMutationResult>();
        mutation.Success.Returns(true);
        requirements
            .CreateFrAsync(Arg.Any<IFrCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutation));
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-create-fr-ac",
                Method = RequirementsCommandShapes.CreateFrMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "FR-MCP-REQAC-999",
                    ["title"] = "Preserve criteria",
                    ["description"] = "Single FR create preserves structured acceptance criteria.",
                    ["priority"] = "high",
                    ["area"] = "MCP",
                    ["acceptanceCriteria"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["id"] = "FR-MCP-REQAC-999-AC001",
                            ["text"] = "Create forwards structured criteria to the workflow.",
                            ["isSatisfied"] = false,
                            ["evidence"] = "dispatcher regression",
                        },
                    },
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await requirements.Received(1).CreateFrAsync(
            Arg.Is<IFrCreateRequest>(request => MatchesCreateCriteria(request)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-REQAC-001: single FR update dispatch preserves structured acceptance criteria.
    /// </summary>
    [Fact]
    public async Task Dispatcher_UpdateFr_PreservesAcceptanceCriteria()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        var mutation = Substitute.For<IFrMutationResult>();
        mutation.Success.Returns(true);
        requirements
            .UpdateFrAsync(Arg.Any<IFrUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutation));
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-update-fr-ac",
                Method = RequirementsCommandShapes.UpdateFrMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "FR-MCP-REQAC-999",
                    ["acceptanceCriteria"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["id"] = "FR-MCP-REQAC-999-AC002",
                            ["text"] = "Update forwards structured criteria to the workflow.",
                            ["isSatisfied"] = true,
                        },
                    },
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await requirements.Received(1).UpdateFrAsync(
            Arg.Is<IFrUpdateRequest>(request => MatchesUpdateCriteria(request)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-REQAC-001: single TR and TEST create dispatch preserves structured acceptance criteria.
    /// </summary>
    [Theory]
    [InlineData(RequirementsCommandShapes.CreateTrMethod, "TR-MCP-REQAC-999")]
    [InlineData(RequirementsCommandShapes.CreateTestMethod, "TEST-MCP-REQAC-999")]
    public async Task Dispatcher_CreateRequirement_PreservesAcceptanceCriteriaForTrAndTest(
        string method,
        string id)
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        var trMutation = Substitute.For<ITrMutationResult>();
        var testMutation = Substitute.For<ITestMutationResult>();
        trMutation.Success.Returns(true);
        testMutation.Success.Returns(true);
        requirements
            .CreateTrAsync(Arg.Any<ITrCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(trMutation));
        requirements
            .CreateTestAsync(Arg.Any<ITestCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testMutation));
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var response = await sut.DispatchAsync(
            BuildCreateRequirementEnvelope(method, id),
            CancellationToken.None);

        Assert.Equal("result", response.Type);
        if (method == RequirementsCommandShapes.CreateTrMethod)
        {
            await requirements.Received(1).CreateTrAsync(
                Arg.Is<ITrCreateRequest>(request => MatchesCreateCriteria(request)),
                Arg.Any<CancellationToken>());
            return;
        }

        await requirements.Received(1).CreateTestAsync(
            Arg.Is<ITestCreateRequest>(request => MatchesCreateCriteria(request)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-REQAC-001: single TR and TEST update dispatch preserves structured acceptance criteria.
    /// </summary>
    [Theory]
    [InlineData(RequirementsCommandShapes.UpdateTrMethod, "TR-MCP-REQAC-999")]
    [InlineData(RequirementsCommandShapes.UpdateTestMethod, "TEST-MCP-REQAC-999")]
    public async Task Dispatcher_UpdateRequirement_PreservesAcceptanceCriteriaForTrAndTest(
        string method,
        string id)
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        var trMutation = Substitute.For<ITrMutationResult>();
        var testMutation = Substitute.For<ITestMutationResult>();
        trMutation.Success.Returns(true);
        testMutation.Success.Returns(true);
        requirements
            .UpdateTrAsync(Arg.Any<ITrUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(trMutation));
        requirements
            .UpdateTestAsync(Arg.Any<ITestUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testMutation));
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var response = await sut.DispatchAsync(
            BuildUpdateRequirementEnvelope(method, id),
            CancellationToken.None);

        Assert.Equal("result", response.Type);
        if (method == RequirementsCommandShapes.UpdateTrMethod)
        {
            await requirements.Received(1).UpdateTrAsync(
                Arg.Is<ITrUpdateRequest>(request => MatchesUpdateCriteria(request)),
                Arg.Any<CancellationToken>());
            return;
        }

        await requirements.Received(1).UpdateTestAsync(
            Arg.Is<ITestUpdateRequest>(request => MatchesUpdateCriteria(request)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-REQAC-001: malformed single requirement acceptance criteria fail validation.
    /// </summary>
    [Fact]
    public async Task Dispatcher_CreateFr_WithMalformedAcceptanceCriteria_ReturnsSchemaValidationFailed()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-create-fr-bad-ac",
                Method = RequirementsCommandShapes.CreateFrMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "FR-MCP-REQAC-999",
                    ["title"] = "Reject bad criteria",
                    ["description"] = "Single FR create rejects malformed criteria.",
                    ["priority"] = "high",
                    ["area"] = "MCP",
                    ["acceptanceCriteria"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["id"] = "FR-MCP-REQAC-999-AC003",
                        },
                    },
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("error", response.Type);
        var err = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("schema_validation_failed", err.Code);
        await requirements.DidNotReceive().CreateFrAsync(Arg.Any<IFrCreateRequest>(), Arg.Any<CancellationToken>());
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
    /// TEST-MCP-REQAC-001: typed FR/TR/TEST batch create and update dispatch preserve structured acceptance criteria.
    /// </summary>
    [Theory]
    [InlineData(RequirementsCommandShapes.CreateFrBatchMethod)]
    [InlineData(RequirementsCommandShapes.UpdateFrBatchMethod)]
    [InlineData(RequirementsCommandShapes.CreateTrBatchMethod)]
    [InlineData(RequirementsCommandShapes.UpdateTrBatchMethod)]
    [InlineData(RequirementsCommandShapes.CreateTestBatchMethod)]
    [InlineData(RequirementsCommandShapes.UpdateTestBatchMethod)]
    public async Task Dispatcher_TypedRequirementBatch_PreservesAcceptanceCriteriaForAllKinds(string method)
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        SetupBatchResults(requirements);
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var response = await sut.DispatchAsync(
            BuildTypedBatchEnvelope(method),
            CancellationToken.None);

        Assert.Equal("result", response.Type);
        await AssertTypedBatchReceivedAsync(requirements, method);
    }

    /// <summary>
    /// TEST-MCP-REQAC-001: mixed requirement batch create and update dispatch preserve structured acceptance criteria.
    /// </summary>
    [Theory]
    [InlineData(RequirementsCommandShapes.CreateBatchMethod, "fr")]
    [InlineData(RequirementsCommandShapes.CreateBatchMethod, "tr")]
    [InlineData(RequirementsCommandShapes.CreateBatchMethod, "test")]
    [InlineData(RequirementsCommandShapes.UpdateBatchMethod, "fr")]
    [InlineData(RequirementsCommandShapes.UpdateBatchMethod, "tr")]
    [InlineData(RequirementsCommandShapes.UpdateBatchMethod, "test")]
    public async Task Dispatcher_MixedRequirementBatch_PreservesAcceptanceCriteriaForAllKinds(
        string method,
        string kind)
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var requirements = Substitute.For<IRequirementsWorkflow>();
        SetupBatchResults(requirements);
        var sut = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirements);

        var response = await sut.DispatchAsync(
            BuildMixedBatchEnvelope(method, kind),
            CancellationToken.None);

        Assert.Equal("result", response.Type);
        if (method == RequirementsCommandShapes.CreateBatchMethod)
        {
            await requirements.Received(1).CreateBatchAsync(
                Arg.Is<CreateRequirementsBatchRequest>(request =>
                    MatchesMixedBatchCriteria(request, kind, $"{kind.ToUpperInvariant()}-MCP-REQAC-999-AC001")),
                Arg.Any<CancellationToken>());
            return;
        }

        await requirements.Received(1).UpdateBatchAsync(
            Arg.Is<UpdateRequirementsBatchRequest>(request =>
                MatchesMixedBatchCriteria(request, kind, $"{kind.ToUpperInvariant()}-MCP-REQAC-999-AC002")),
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
        await passthrough.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// TEST-MCP-BUGTRIAGE-019: session-log commands route queryTitle overrides before mutation.
    /// </summary>
    [Fact]
    public async Task Dispatcher_SessionLogCommands_RouteQueryTitleOverrides()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var sessionLog = Substitute.For<ISessionLogWorkflow>();
        sessionLog.UpdateTurnTitleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        sessionLog.UpdateTurnAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<IReadOnlyList<string>?>(), Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        sessionLog.CompleteTurnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        sessionLog.AppendActionsAsync(Arg.Any<IReadOnlyList<ISessionAction>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var sut = new ReplCommandDispatcher(passthrough, sessionLogWorkflow: sessionLog);

        await sut.DispatchAsync(new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-sessionlog-title-update",
                Method = SessionLogCommandShapes.UpdateTurnMethod,
                Params = new Dictionary<string, object?>
                {
                    ["queryTitle"] = "Updated title",
                    ["response"] = "Working",
                },
            },
        }, CancellationToken.None);

        await sut.DispatchAsync(new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-sessionlog-title-append",
                Method = SessionLogCommandShapes.AppendActionsMethod,
                Params = new Dictionary<string, object?>
                {
                    ["queryTitle"] = "Append title",
                    ["actions"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["type"] = "edit",
                            ["description"] = "Changed file",
                            ["status"] = "succeeded",
                        },
                    },
                },
            },
        }, CancellationToken.None);

        await sut.DispatchAsync(new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-sessionlog-title-complete",
                Method = SessionLogCommandShapes.CompleteTurnMethod,
                Params = new Dictionary<string, object?>
                {
                    ["queryTitle"] = "Complete title",
                    ["response"] = "Done",
                },
            },
        }, CancellationToken.None);

        await sessionLog.Received(1).UpdateTurnTitleAsync("Updated title", Arg.Any<CancellationToken>());
        await sessionLog.Received(1).UpdateTurnTitleAsync("Append title", Arg.Any<CancellationToken>());
        await sessionLog.Received(1).UpdateTurnTitleAsync("Complete title", Arg.Any<CancellationToken>());
        await sessionLog.Received(1).UpdateTurnAsync("Working", null, null, Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await sessionLog.Received(1).AppendActionsAsync(Arg.Is<IReadOnlyList<ISessionAction>>(actions => actions != null && actions.Count == 1), Arg.Any<CancellationToken>());
        await sessionLog.Received(1).CompleteTurnAsync("Done", Arg.Any<CancellationToken>());
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
        await passthrough.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A workflow.memory.list request is routed through the typed memory workflow and applies
    /// schema-validated scope/category/keyword filters.
    /// </summary>
    [Fact]
    public async Task Dispatcher_MemoryListRequest_DelegatesToMemoryWorkflow()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var memory = Substitute.For<IMemoryWorkflow>();
        memory.ListAsync(MemoryScope.Global, "agent", "PowerShell", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new MemoryQueryResult { Items = [], TotalCount = 0 }));

        var sut = new ReplCommandDispatcher(passthrough, memoryWorkflow: memory);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-list",
                Method = MemoryCommandShapes.ListMethod,
                Params = new Dictionary<string, object?>
                {
                    ["scope"] = "Global",
                    ["category"] = "agent",
                    ["keyword"] = "PowerShell",
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await memory.Received(1).ListAsync(MemoryScope.Global, "agent", "PowerShell", Arg.Any<CancellationToken>());
        await passthrough.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// workflow.memory.list accepts Effective as an explicit alias for the default
    /// effective Global + Workspace list.
    /// </summary>
    [Fact]
    public async Task Dispatcher_MemoryListRequest_EffectiveScopeForwardsNullScope()
    {
        var memory = Substitute.For<IMemoryWorkflow>();
        memory.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new MemoryQueryResult { TotalCount = 0 }));

        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), memoryWorkflow: memory);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-list-effective",
                Method = MemoryCommandShapes.ListMethod,
                Params = new Dictionary<string, object?>
                {
                    ["scope"] = "Effective",
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await memory.Received(1).ListAsync(null, null, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A workflow.memory.add request is normalized into the typed memory add request and keeps
    /// agent identity metadata intact.
    /// </summary>
    [Fact]
    public async Task Dispatcher_MemoryAddRequest_AcceptsFlatYamlShape()
    {
        var memory = Substitute.For<IMemoryWorkflow>();
        memory.AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new MemoryMutationResult { Success = true }));

        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), memoryWorkflow: memory);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-add",
                Method = MemoryCommandShapes.AddMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "MEMORY-AGENT-001",
                    ["category"] = "agent",
                    ["scope"] = "Workspace",
                    ["text"] = "Use the Codex plugin wrapper for MCP state.",
                    ["updatedBy"] = "Codex",
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await memory.Received(1).AddAsync(
            Arg.Is<MemoryAddRequest>(request => request != null
                && request.Id == "MEMORY-AGENT-001"
                && request.Category == "agent"
                && request.Scope == MemoryScope.Workspace
                && request.Text == "Use the Codex plugin wrapper for MCP state."
                && request.UpdatedBy == "Codex"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// workflow.memory.add requires memory text, so malformed YAML is rejected before transport.
    /// </summary>
    [Fact]
    public async Task Dispatcher_MemoryAddRequest_MissingText_ReturnsSchemaError()
    {
        var memory = Substitute.For<IMemoryWorkflow>();
        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), memoryWorkflow: memory);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-add-invalid",
                Method = MemoryCommandShapes.AddMethod,
                Params = new Dictionary<string, object?>
                {
                    ["category"] = "agent",
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("error", response.Type);
        var err = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("schema_validation_failed", err.Code);
        await memory.DidNotReceiveWithAnyArgs().AddAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// workflow.memory.add rejects explicit ids that do not match the canonical
    /// MEMORY-{CATEGORY}-{NNN} format before invoking the workflow.
    /// </summary>
    [Fact]
    public async Task Dispatcher_MemoryAddRequest_InvalidId_ReturnsSchemaError()
    {
        var memory = Substitute.For<IMemoryWorkflow>();
        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), memoryWorkflow: memory);

        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-add-invalid-id",
                Method = MemoryCommandShapes.AddMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "not-a-memory-id",
                    ["category"] = "agent",
                    ["text"] = "This id must be rejected.",
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("error", response.Type);
        var err = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("schema_validation_failed", err.Code);
        var errors = Assert.IsAssignableFrom<IReadOnlyList<string>>(err.Details!["errors"]);
        Assert.Contains(errors, error => error.Contains("MEMORY-{CATEGORY}-{NNN}", StringComparison.Ordinal));
        await memory.DidNotReceiveWithAnyArgs().AddAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
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
    /// TEST-MCP-BUGTRIAGE-015: sparse TODO updates must preserve omitted collection fields.
    /// </summary>
    [Fact]
    public async Task Dispatcher_TodoUpdateRequest_PreservesOmittedCollectionFields()
    {
        var todo = Substitute.For<ITodoWorkflow>();
        var mutation = CreateMutationResult();
        ITodoUpdateRequest? capturedRequest = null;
        todo.UpdateAsync(
                "BUG-TRIAGE-015",
                Arg.Do<ITodoUpdateRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutation));

        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), todoWorkflow: todo);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-bug-triage-015",
                Method = TodoCommandShapes.UpdateMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "BUG-TRIAGE-015",
                    ["done"] = true,
                    ["doneSummary"] = "Fixed sparse update preservation.",
                    ["remaining"] = "No remaining work.",
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await todo.Received(1).UpdateAsync("BUG-TRIAGE-015", Arg.Any<ITodoUpdateRequest>(), Arg.Any<CancellationToken>());
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Done);
        Assert.Equal("Fixed sparse update preservation.", capturedRequest.DoneSummary);
        Assert.Equal("No remaining work.", capturedRequest.Remaining);
        Assert.Null(capturedRequest.Description);
        Assert.Null(capturedRequest.TechnicalDetails);
        Assert.Null(capturedRequest.ImplementationTasks);
        Assert.Null(capturedRequest.DependsOn);
        Assert.Null(capturedRequest.FunctionalRequirements);
        Assert.Null(capturedRequest.TechnicalRequirements);
    }

    /// <summary>
    /// TEST-MCP-BUGTRIAGE-015: explicit empty TODO update collections must clear fields.
    /// </summary>
    [Fact]
    public async Task Dispatcher_TodoUpdateRequest_PreservesExplicitEmptyCollectionFields()
    {
        var todo = Substitute.For<ITodoWorkflow>();
        var mutation = CreateMutationResult();
        ITodoUpdateRequest? capturedRequest = null;
        todo.UpdateAsync(
                "BUG-TRIAGE-015",
                Arg.Do<ITodoUpdateRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutation));

        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), todoWorkflow: todo);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-bug-triage-015-empty",
                Method = TodoCommandShapes.UpdateMethod,
                Params = new Dictionary<string, object?>
                {
                    ["id"] = "BUG-TRIAGE-015",
                    ["description"] = Array.Empty<object?>(),
                    ["technicalDetails"] = Array.Empty<object?>(),
                    ["implementationTasks"] = Array.Empty<object?>(),
                    ["dependsOn"] = Array.Empty<object?>(),
                    ["functionalRequirements"] = Array.Empty<object?>(),
                    ["technicalRequirements"] = Array.Empty<object?>(),
                },
            },
        };

        var response = await sut.DispatchAsync(envelope, CancellationToken.None);

        Assert.Equal("result", response.Type);
        await todo.Received(1).UpdateAsync("BUG-TRIAGE-015", Arg.Any<ITodoUpdateRequest>(), Arg.Any<CancellationToken>());
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Description);
        Assert.Empty(capturedRequest.Description);
        Assert.NotNull(capturedRequest.TechnicalDetails);
        Assert.Empty(capturedRequest.TechnicalDetails);
        Assert.NotNull(capturedRequest.ImplementationTasks);
        Assert.Empty(capturedRequest.ImplementationTasks);
        Assert.NotNull(capturedRequest.DependsOn);
        Assert.Empty(capturedRequest.DependsOn);
        Assert.NotNull(capturedRequest.FunctionalRequirements);
        Assert.Empty(capturedRequest.FunctionalRequirements);
        Assert.NotNull(capturedRequest.TechnicalRequirements);
        Assert.Empty(capturedRequest.TechnicalRequirements);
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

    private static bool MatchesCreateCriteria(IFrCreateRequest? request)
        => MatchesCreateCriteria(request, "FR-MCP-REQAC-999");

    private static bool MatchesCreateCriteria(IFrCreateRequest? request, string requirementId)
    {
        if (request?.AcceptanceCriteria is not { Count: 1 } criteria)
            return false;

        return criteria[0].Id == $"{requirementId}-AC001" &&
               criteria[0].Text == "Create forwards structured criteria to the workflow." &&
               criteria[0].IsSatisfied == false &&
               criteria[0].Evidence == "dispatcher regression";
    }

    private static bool MatchesUpdateCriteria(IFrUpdateRequest? request)
        => MatchesUpdateCriteria(request, "FR-MCP-REQAC-999");

    private static bool MatchesUpdateCriteria(IFrUpdateRequest? request, string requirementId)
    {
        if (request?.AcceptanceCriteria is not { Count: 1 } criteria)
            return false;

        return criteria[0].Id == $"{requirementId}-AC002" &&
               criteria[0].Text == "Update forwards structured criteria to the workflow." &&
               criteria[0].IsSatisfied == true;
    }

    private static bool MatchesCreateCriteria(ITrCreateRequest? request)
        => MatchesCreateCriteria(request, "TR-MCP-REQAC-999");

    private static bool MatchesCreateCriteria(ITrCreateRequest? request, string requirementId)
    {
        if (request?.AcceptanceCriteria is not { Count: 1 } criteria)
            return false;

        return criteria[0].Id == $"{requirementId}-AC001" &&
               criteria[0].Text == "Create forwards structured criteria to the workflow." &&
               criteria[0].IsSatisfied == false &&
               criteria[0].Evidence == "dispatcher regression";
    }

    private static bool MatchesUpdateCriteria(ITrUpdateRequest? request)
        => MatchesUpdateCriteria(request, "TR-MCP-REQAC-999");

    private static bool MatchesUpdateCriteria(ITrUpdateRequest? request, string requirementId)
    {
        if (request?.AcceptanceCriteria is not { Count: 1 } criteria)
            return false;

        return criteria[0].Id == $"{requirementId}-AC002" &&
               criteria[0].Text == "Update forwards structured criteria to the workflow." &&
               criteria[0].IsSatisfied == true;
    }

    private static bool MatchesCreateCriteria(ITestCreateRequest? request)
        => MatchesCreateCriteria(request, "TEST-MCP-REQAC-999");

    private static bool MatchesCreateCriteria(ITestCreateRequest? request, string requirementId)
    {
        if (request?.AcceptanceCriteria is not { Count: 1 } criteria)
            return false;

        return criteria[0].Id == $"{requirementId}-AC001" &&
               criteria[0].Text == "Create forwards structured criteria to the workflow." &&
               criteria[0].IsSatisfied == false &&
               criteria[0].Evidence == "dispatcher regression";
    }

    private static bool MatchesUpdateCriteria(ITestUpdateRequest? request)
        => MatchesUpdateCriteria(request, "TEST-MCP-REQAC-999");

    private static bool MatchesUpdateCriteria(ITestUpdateRequest? request, string requirementId)
    {
        if (request?.AcceptanceCriteria is not { Count: 1 } criteria)
            return false;

        return criteria[0].Id == $"{requirementId}-AC002" &&
               criteria[0].Text == "Update forwards structured criteria to the workflow." &&
               criteria[0].IsSatisfied == true;
    }

    private static YamlEnvelope BuildCreateRequirementEnvelope(string method, string id)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["title"] = "Preserve criteria",
            ["description"] = "Create preserves structured acceptance criteria.",
            ["priority"] = "high",
            ["area"] = "MCP",
            ["acceptanceCriteria"] = CreateCriteria(id),
        };

        if (method == RequirementsCommandShapes.CreateTrMethod)
        {
            parameters["subarea"] = "REPL";
        }
        else
        {
            parameters["testType"] = "unit";
        }

        return BuildRequestEnvelope($"req-create-{id.ToLowerInvariant()}-ac", method, parameters);
    }

    private static YamlEnvelope BuildUpdateRequirementEnvelope(string method, string id)
        => BuildRequestEnvelope(
            $"req-update-{id.ToLowerInvariant()}-ac",
            method,
            new Dictionary<string, object?>
            {
                ["id"] = id,
                ["acceptanceCriteria"] = UpdateCriteria(id),
            });

    private static YamlEnvelope BuildTypedBatchEnvelope(string method)
    {
        var id = RequirementIdForTypedBatchMethod(method);
        return BuildRequestEnvelope(
            $"req-{method.Split('.').Last().ToLowerInvariant()}-ac",
            method,
            new Dictionary<string, object?>
            {
                ["records"] = new[]
                {
                    BuildTypedBatchRecord(method, id),
                },
            });
    }

    private static YamlEnvelope BuildMixedBatchEnvelope(string method, string kind)
    {
        var id = $"{kind.ToUpperInvariant()}-MCP-REQAC-999";
        return BuildRequestEnvelope(
            $"req-{method.Split('.').Last().ToLowerInvariant()}-{kind}-ac",
            method,
            new Dictionary<string, object?>
            {
                ["records"] = new[]
                {
                    BuildMixedBatchRecord(method, kind, id),
                },
            });
    }

    private static YamlEnvelope BuildRequestEnvelope(
        string requestId,
        string method,
        Dictionary<string, object?> parameters)
        => new()
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = requestId,
                Method = method,
                Params = parameters,
            },
        };

    private static Dictionary<string, object?> BuildTypedBatchRecord(string method, string id)
    {
        var record = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["acceptanceCriteria"] = method.Contains("create", StringComparison.OrdinalIgnoreCase)
                ? CreateCriteria(id)
                : UpdateCriteria(id),
        };

        if (!method.Contains("create", StringComparison.OrdinalIgnoreCase))
        {
            return record;
        }

        record["title"] = "Preserve criteria";
        record["description"] = "Batch create preserves structured acceptance criteria.";
        if (method == RequirementsCommandShapes.CreateTestBatchMethod)
        {
            record["condition"] = "Created TEST criteria survive dispatch.";
        }
        else
        {
            record["body"] = "Created requirement criteria survive dispatch.";
        }

        return record;
    }

    private static Dictionary<string, object?> BuildMixedBatchRecord(string method, string kind, string id)
    {
        var record = new Dictionary<string, object?>
        {
            ["kind"] = kind,
            ["id"] = id,
            ["acceptanceCriteria"] = method == RequirementsCommandShapes.CreateBatchMethod
                ? CreateCriteria(id)
                : UpdateCriteria(id),
        };

        if (method != RequirementsCommandShapes.CreateBatchMethod)
        {
            return record;
        }

        record["title"] = "Preserve criteria";
        record["description"] = "Mixed batch create preserves structured acceptance criteria.";
        if (kind == "test")
        {
            record["condition"] = "Created TEST criteria survive mixed dispatch.";
        }
        else
        {
            record["body"] = "Created requirement criteria survive mixed dispatch.";
        }

        return record;
    }

    private static object[] CreateCriteria(string requirementId)
        =>
        [
            new Dictionary<string, object?>
            {
                ["id"] = $"{requirementId}-AC001",
                ["text"] = "Create forwards structured criteria to the workflow.",
                ["isSatisfied"] = false,
                ["evidence"] = "dispatcher regression",
            },
        ];

    private static object[] UpdateCriteria(string requirementId)
        =>
        [
            new Dictionary<string, object?>
            {
                ["id"] = $"{requirementId}-AC002",
                ["text"] = "Update forwards structured criteria to the workflow.",
                ["isSatisfied"] = true,
            },
        ];

    private static void SetupBatchResults(IRequirementsWorkflow requirements)
    {
        var result = Task.FromResult(new RequirementsBatchResult
        {
            Success = true,
            Operation = "batch",
            Kind = "mixed",
            Total = 1,
        });

        requirements.CreateFrBatchAsync(Arg.Any<CreateFrBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
        requirements.UpdateFrBatchAsync(Arg.Any<UpdateFrBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
        requirements.CreateTrBatchAsync(Arg.Any<CreateTrBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
        requirements.UpdateTrBatchAsync(Arg.Any<UpdateTrBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
        requirements.CreateTestBatchAsync(Arg.Any<CreateTestBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
        requirements.UpdateTestBatchAsync(Arg.Any<UpdateTestBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
        requirements.CreateBatchAsync(Arg.Any<CreateRequirementsBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
        requirements.UpdateBatchAsync(Arg.Any<UpdateRequirementsBatchRequest>(), Arg.Any<CancellationToken>()).Returns(result);
    }

    private static async Task AssertTypedBatchReceivedAsync(IRequirementsWorkflow requirements, string method)
    {
        switch (method)
        {
            case RequirementsCommandShapes.CreateFrBatchMethod:
                await requirements.Received(1).CreateFrBatchAsync(
                    Arg.Is<CreateFrBatchRequest>(request =>
                        MatchesTypedBatchCriteria(request, "FR-MCP-REQAC-999-AC001")),
                    Arg.Any<CancellationToken>());
                break;
            case RequirementsCommandShapes.UpdateFrBatchMethod:
                await requirements.Received(1).UpdateFrBatchAsync(
                    Arg.Is<UpdateFrBatchRequest>(request =>
                        MatchesTypedBatchCriteria(request, "FR-MCP-REQAC-999-AC002")),
                    Arg.Any<CancellationToken>());
                break;
            case RequirementsCommandShapes.CreateTrBatchMethod:
                await requirements.Received(1).CreateTrBatchAsync(
                    Arg.Is<CreateTrBatchRequest>(request =>
                        MatchesTypedBatchCriteria(request, "TR-MCP-REQAC-999-AC001")),
                    Arg.Any<CancellationToken>());
                break;
            case RequirementsCommandShapes.UpdateTrBatchMethod:
                await requirements.Received(1).UpdateTrBatchAsync(
                    Arg.Is<UpdateTrBatchRequest>(request =>
                        MatchesTypedBatchCriteria(request, "TR-MCP-REQAC-999-AC002")),
                    Arg.Any<CancellationToken>());
                break;
            case RequirementsCommandShapes.CreateTestBatchMethod:
                await requirements.Received(1).CreateTestBatchAsync(
                    Arg.Is<CreateTestBatchRequest>(request =>
                        MatchesTypedBatchCriteria(request, "TEST-MCP-REQAC-999-AC001")),
                    Arg.Any<CancellationToken>());
                break;
            case RequirementsCommandShapes.UpdateTestBatchMethod:
                await requirements.Received(1).UpdateTestBatchAsync(
                    Arg.Is<UpdateTestBatchRequest>(request =>
                        MatchesTypedBatchCriteria(request, "TEST-MCP-REQAC-999-AC002")),
                    Arg.Any<CancellationToken>());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported typed batch method.");
        }
    }

    private static bool MatchesMixedBatchCriteria(
        CreateRequirementsBatchRequest? request,
        string expectedKind,
        string expectedCriterionId)
    {
        if (request?.Records is not { Count: 1 })
            return false;

        var record = request.Records[0];
        return record.Kind == expectedKind &&
               MatchesCriteria(record.AcceptanceCriteria, expectedCriterionId);
    }

    private static bool MatchesMixedBatchCriteria(
        UpdateRequirementsBatchRequest? request,
        string expectedKind,
        string expectedCriterionId)
    {
        if (request?.Records is not { Count: 1 })
            return false;

        var record = request.Records[0];
        return record.Kind == expectedKind &&
               MatchesCriteria(record.AcceptanceCriteria, expectedCriterionId);
    }

    private static bool MatchesTypedBatchCriteria(CreateFrBatchRequest? request, string expectedCriterionId)
        => request?.Records is { Count: 1 } &&
           MatchesCriteria(request.Records[0].AcceptanceCriteria, expectedCriterionId);

    private static bool MatchesTypedBatchCriteria(UpdateFrBatchRequest? request, string expectedCriterionId)
        => request?.Records is { Count: 1 } &&
           MatchesCriteria(request.Records[0].AcceptanceCriteria, expectedCriterionId);

    private static bool MatchesTypedBatchCriteria(CreateTrBatchRequest? request, string expectedCriterionId)
        => request?.Records is { Count: 1 } &&
           MatchesCriteria(request.Records[0].AcceptanceCriteria, expectedCriterionId);

    private static bool MatchesTypedBatchCriteria(UpdateTrBatchRequest? request, string expectedCriterionId)
        => request?.Records is { Count: 1 } &&
           MatchesCriteria(request.Records[0].AcceptanceCriteria, expectedCriterionId);

    private static bool MatchesTypedBatchCriteria(CreateTestBatchRequest? request, string expectedCriterionId)
        => request?.Records is { Count: 1 } &&
           MatchesCriteria(request.Records[0].AcceptanceCriteria, expectedCriterionId);

    private static bool MatchesTypedBatchCriteria(UpdateTestBatchRequest? request, string expectedCriterionId)
        => request?.Records is { Count: 1 } &&
           MatchesCriteria(request.Records[0].AcceptanceCriteria, expectedCriterionId);

    private static bool MatchesCriteria(
        IReadOnlyList<AcceptanceCriterion>? criteria,
        string expectedCriterionId)
    {
        return criteria is { Count: 1 } &&
               criteria[0].Id == expectedCriterionId;
    }

    private static string RequirementIdForTypedBatchMethod(string method)
        => method switch
        {
            RequirementsCommandShapes.CreateFrBatchMethod or RequirementsCommandShapes.UpdateFrBatchMethod
                => "FR-MCP-REQAC-999",
            RequirementsCommandShapes.CreateTrBatchMethod or RequirementsCommandShapes.UpdateTrBatchMethod
                => "TR-MCP-REQAC-999",
            RequirementsCommandShapes.CreateTestBatchMethod or RequirementsCommandShapes.UpdateTestBatchMethod
                => "TEST-MCP-REQAC-999",
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported typed batch method."),
        };
}
