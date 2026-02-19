using FWH.Support.Mcp.Models;
using FWH.Support.Mcp.Options;
using FWH.Support.Mcp.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace FWH.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013: Unit tests for InteractionLogSubmissionChannel.</summary>
public sealed class InteractionLogSubmissionChannelTests
{
    /// <summary>TryEnqueue accepts entry and TryDequeueAsync returns it.</summary>
    [Fact]
    public async Task TryEnqueue_ThenTryDequeue_ReturnsEntry()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions { QueueCapacity = 10 });
        var channel = new InteractionLogSubmissionChannel(options);

        var entry = new InteractionLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Method = "GET",
            Path = "/mcp/context/sources",
            StatusCode = 200,
            DurationMs = 1.5,
            RequestId = "req-1"
        };

        var enqueued = channel.TryEnqueue(entry);
        Assert.True(enqueued);

        var (success, dequeued) = await channel.TryDequeueAsync().ConfigureAwait(true);
        Assert.True(success);
        Assert.NotNull(dequeued);
        Assert.Equal("GET", dequeued.Method);
        Assert.Equal("/mcp/context/sources", dequeued.Path);
        Assert.Equal(200, dequeued.StatusCode);
        Assert.Equal("req-1", dequeued.RequestId);
    }

    /// <summary>TryDequeueAsync with empty channel returns (false, null) after WaitToReadAsync when cancelled.</summary>
    [Fact]
    public async Task TryDequeueAsync_WhenEmpty_CanBeCancelled()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions { QueueCapacity = 10 });
        var channel = new InteractionLogSubmissionChannel(options);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var (success, entry) = await channel.TryDequeueAsync(cts.Token).ConfigureAwait(true);
        Assert.False(success);
        Assert.Null(entry);
    }
}
