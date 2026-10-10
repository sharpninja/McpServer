using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using McpServer.QBAgent;
using Microsoft.Extensions.AI;
using Xunit;

namespace McpServer.QBAgent.Tests;

/// <summary>TEST-MCP-QBPROGRESS-001: role SSE events print before the stream ends.</summary>
public sealed class QBAgentRoleStreamChatClientTests
{
    /// <summary>Creativity started is reported as soon as that SSE event arrives, before [DONE].</summary>
    [Fact]
    public async Task ReadSseAsync_ReportsRoleStartedBeforeDone()
    {
        var pipe = new Pipe();
        var progress = new List<string>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Action<string> onProgress = line =>
        {
            progress.Add(line);
            if (line.Contains("Creativity started", StringComparison.Ordinal))
                started.TrySetResult();
        };

        var consume = ConsumeAsync(pipe.Reader.AsStream(), onProgress, TestContext.Current.CancellationToken);
        await pipe.Writer.WriteAsync(
            Encoding.UTF8.GetBytes("event: quadbrain.role\ndata: {\"role\":\"Creativity\",\"phase\":\"started\"}\n\n"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await pipe.Writer.FlushAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(consume.IsCompleted);

        await pipe.Writer.WriteAsync(
            Encoding.UTF8.GetBytes(
                "event: quadbrain.role\ndata: {\"role\":\"Creativity\",\"phase\":\"completed\",\"output\":\"ideas\"}\n\n" +
                "data: {\"choices\":[{\"delta\":{\"content\":\"final\"}}]}\n\n" +
                "data: [DONE]\n\n"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await pipe.Writer.CompleteAsync().ConfigureAwait(true);

        var updates = await consume.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains(progress, line => line.Contains("completed", StringComparison.Ordinal) && line.Contains("ideas", StringComparison.Ordinal));
        Assert.Contains("final", string.Join(string.Empty, updates.Select(static u => u.Text)), StringComparison.Ordinal);
    }

    [Fact]
    public void MapMessages_FunctionResult_UsesToolRoleAndCallId()
    {
        var mapped = QBAgentRoleStreamChatClient.MapMessages(
        [
            new ChatMessage(ChatRole.User, "update voice chat"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call_0", "read_file", new Dictionary<string, object?> { ["path"] = "a.yaml" })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call_0", "file body")]),
        ]);
        var json = JsonSerializer.Serialize(mapped);

        Assert.Contains("\"role\":\"tool\"", json, StringComparison.Ordinal);
        Assert.Contains("call_0", json, StringComparison.Ordinal);
        Assert.Contains("file body", json, StringComparison.Ordinal);
        Assert.Contains("read_file", json, StringComparison.Ordinal);
    }

    private static async Task<List<Microsoft.Extensions.AI.ChatResponseUpdate>> ConsumeAsync(
        Stream stream,
        Action<string> progress,
        CancellationToken cancellationToken)
    {
        var updates = new List<Microsoft.Extensions.AI.ChatResponseUpdate>();
        await foreach (var update in QBAgentRoleStreamChatClient.ReadSseAsync(stream, progress, cancellationToken)
            .ConfigureAwait(false))
        {
            updates.Add(update);
        }

        return updates;
    }
}
