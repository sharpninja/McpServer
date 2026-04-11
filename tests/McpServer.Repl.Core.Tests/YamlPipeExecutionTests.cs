// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Pipe execution acceptance
// FR-MCP-REPL-002: REPL Lifecycle Management - Multi-line YAML framing and dispatch
// TR-MCP-REPL-001: YAML Envelope Protocol - Production serializer round-trip
// TR-MCP-REPL-003: Command Loop Lifecycle - Multi-line accumulation and dispatch
// TR-MCP-REPL-004: Command Registry and Dispatcher - Request routing
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes end-to-end

using System.Text;
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

    private static bool DictionaryContainsValue(Dictionary<string, object?>? dict, string key, object expected)
    {
        return dict is not null && dict.TryGetValue(key, out var value) && Equals(value, expected);
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
