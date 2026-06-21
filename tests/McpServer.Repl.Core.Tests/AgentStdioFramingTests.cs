// FR-MCP-REPL-005: Agent STDIO framing - NDJSON fast path and document-separator
// response terminator so persistent bridge clients (Node ReplBridge) work without
// plugin changes, and one process can serve many requests.
// TEST-MCP-REPL-005: Mixed NDJSON and YAML envelopes in one process each get a
// response terminated by both a blank line and a '---' separator line.

using System.Text;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Phase 1b framing tests for <see cref="AgentStdioProtocol"/>:
/// (1) a complete single-line JSON envelope dispatches immediately without waiting
/// for a blank-line/<c>---</c> boundary (NDJSON);
/// (2) multiple <c>---</c>-separated YAML envelopes in one process are each answered;
/// (3) every response is terminated by a blank line (legacy shell contract) AND a
/// <c>---</c> separator line (Node ReplBridge contract).
/// </summary>
public class AgentStdioFramingTests
{
    /// <summary>
    /// Two back-to-back single-line JSON request envelopes with NO framing
    /// boundary between them each get their own response. Today both lines
    /// accumulate into one buffer, merge into invalid YAML, and produce a single
    /// invalid_envelope error - the root cause of the broken Node ReplBridge.
    /// </summary>
    [Fact]
    public async Task RunAsync_BackToBackSingleLineJsonEnvelopes_EachDispatched()
    {
        var (sut, passthrough) = BuildSut();

        var input = new StringBuilder()
            .AppendLine("{\"type\":\"request\",\"payload\":{\"requestId\":\"req-json-1\",\"method\":\"client.todo.QueryAsync\",\"params\":{\"keyword\":\"a\"}}}")
            .AppendLine("{\"type\":\"request\",\"payload\":{\"requestId\":\"req-json-2\",\"method\":\"client.todo.QueryAsync\",\"params\":{\"keyword\":\"b\"}}}")
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("requestId: req-json-1", output, StringComparison.Ordinal);
        Assert.Contains("requestId: req-json-2", output, StringComparison.Ordinal);
        Assert.DoesNotContain("invalid_envelope", output, StringComparison.Ordinal);
        await passthrough.Received(2).InvokeAsync(
            "todo", "QueryAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A single-line JSON envelope followed by a multi-line YAML envelope in the
    /// same process are both dispatched - mixed framing is first-class.
    /// </summary>
    [Fact]
    public async Task RunAsync_MixedJsonAndYamlEnvelopes_EachDispatched()
    {
        var (sut, passthrough) = BuildSut();

        var input = new StringBuilder()
            .AppendLine("{\"type\":\"request\",\"payload\":{\"requestId\":\"req-mixed-json\",\"method\":\"client.todo.QueryAsync\"}}")
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-mixed-yaml")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine()
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("requestId: req-mixed-json", output, StringComparison.Ordinal);
        Assert.Contains("requestId: req-mixed-yaml", output, StringComparison.Ordinal);
        await passthrough.Received(2).InvokeAsync(
            "todo", "QueryAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Multiple <c>---</c>-separated YAML envelopes in one process each get a
    /// response (regression guard for the existing multi-document loop).
    /// </summary>
    [Fact]
    public async Task RunAsync_DashSeparatedYamlEnvelopes_EachDispatched()
    {
        var (sut, passthrough) = BuildSut();

        var input = new StringBuilder()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-yaml-1")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine("---")
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-yaml-2")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine()
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.Contains("requestId: req-yaml-1", output, StringComparison.Ordinal);
        Assert.Contains("requestId: req-yaml-2", output, StringComparison.Ordinal);
        await passthrough.Received(2).InvokeAsync(
            "todo", "QueryAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Every response envelope is terminated by a blank line (legacy shell
    /// repl-invoke contract) AND a <c>---</c> document-separator line (the framing
    /// the Node ReplBridge response parser has always waited for).
    /// </summary>
    [Fact]
    public async Task RunAsync_Responses_TerminatedByBlankLineAndDocumentSeparator()
    {
        var (sut, _) = BuildSut();

        var input = new StringBuilder()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-term-1")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine()
            .ToString();

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await sut.RunAsync(reader, writer, CancellationToken.None);

        var lines = writer.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var separatorIndex = Array.FindIndex(lines, line => line.TrimEnd() == "---");
        Assert.True(separatorIndex > 0, "response must be terminated by a '---' separator line");
        Assert.Contains(lines.Take(separatorIndex), line => line.Length == 0);
    }

    private static (AgentStdioProtocol Sut, IGenericClientPassthrough Passthrough) BuildSut()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new { ok = true }));
        var sut = new AgentStdioProtocol(new YamlSerializer(), new ReplCommandDispatcher(passthrough));
        return (sut, passthrough);
    }
}
