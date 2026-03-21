using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Tests.TestSupport;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-MCP-EVT-001: Unit tests for ChannelChangeEventBus pub/sub behavior.</summary>
public sealed class ChannelChangeEventBusTests
{
    /// <summary>PublishAsync does not throw when no subscribers are registered for the event bus.</summary>
    [Fact]
    public async Task PublishAsync_NoSubscribers_DoesNotThrow()
    {
        var sut = new ChannelChangeEventBus(new TestLogger<ChannelChangeEventBus>());
        var evt = new ChangeEvent
        {
            Category = ChangeEventCategories.Todo,
            Action = ChangeEventActions.Created,
            EntityId = "TEST-001",
        };

        var ex = await Record.ExceptionAsync(async () =>
            await sut.PublishAsync(evt, CancellationToken.None).ConfigureAwait(true)).ConfigureAwait(true);

        Assert.Null(ex);
    }

    /// <summary>PublishAsync fans a change event out to all active subscribers.</summary>
    [Fact]
    public async Task PublishAsync_MultipleSubscribers_AllReceiveEvent()
    {
        var sut = new ChannelChangeEventBus(new TestLogger<ChannelChangeEventBus>());
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var sub1 = sut.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var sub2 = sut.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var read1Task = sub1.MoveNextAsync().AsTask();
        var read2Task = sub2.MoveNextAsync().AsTask();

        var evt = new ChangeEvent
        {
            Category = ChangeEventCategories.Todo,
            Action = ChangeEventActions.Updated,
            EntityId = "TEST-002",
        };

        await sut.PublishAsync(evt, cts.Token).ConfigureAwait(true);

        Assert.True(await read1Task.ConfigureAwait(true));
        Assert.True(await read2Task.ConfigureAwait(true));
        Assert.Equal("TEST-002", sub1.Current.EntityId);
        Assert.Equal("TEST-002", sub2.Current.EntityId);
    }

    /// <summary>SubscribeAsync stops enumeration when the subscriber cancellation token is cancelled.</summary>
    [Fact]
    public async Task SubscribeAsync_Cancellation_StopsEnumeration()
    {
        var sut = new ChannelChangeEventBus(new TestLogger<ChannelChangeEventBus>());
        using var cts = new CancellationTokenSource();
        var sub = sut.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await sub.MoveNextAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    /// <summary>PublishAsync logs overflow and preserves queued events when a subscriber buffer is full.</summary>
    [Fact]
    public async Task PublishAsync_WhenSubscriberBufferIsFull_LogsWarningAndRejectsNewEvent()
    {
        var logger = new TestLogger<ChannelChangeEventBus>();
        var sut = new ChannelChangeEventBus(logger);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var subscriber = sut.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var firstReadTask = subscriber.MoveNextAsync().AsTask();
        await sut.PublishAsync(
            new ChangeEvent
            {
                Category = ChangeEventCategories.Todo,
                Action = ChangeEventActions.Updated,
                EntityId = "ITEM-0000",
            },
            cts.Token).ConfigureAwait(true);

        Assert.True(await firstReadTask.ConfigureAwait(true));
        Assert.Equal("ITEM-0000", subscriber.Current.EntityId);

        for (var index = 1; index <= 1000; index++)
        {
            await sut.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Todo,
                    Action = ChangeEventActions.Updated,
                    EntityId = $"ITEM-{index:0000}",
                },
                cts.Token).ConfigureAwait(true);
        }

        await sut.PublishAsync(
            new ChangeEvent
            {
                Category = ChangeEventCategories.Todo,
                Action = ChangeEventActions.Updated,
                EntityId = "ITEM-OVERFLOW",
            },
            cts.Token).ConfigureAwait(true);

        var deliveredEntityIds = new List<string> { "ITEM-0000" };
        for (var index = 1; index <= 1000; index++)
        {
            Assert.True(await subscriber.MoveNextAsync().ConfigureAwait(true));
            deliveredEntityIds.Add(subscriber.Current.EntityId!);
        }

        Assert.Equal("ITEM-0000", deliveredEntityIds[0]);
        Assert.Equal("ITEM-1000", deliveredEntityIds[^1]);
        Assert.DoesNotContain("ITEM-OVERFLOW", deliveredEntityIds);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
                entry.Message.Contains("Change event delivery rejected 1 subscriber writes", StringComparison.Ordinal) &&
                entry.Message.Contains("ITEM-OVERFLOW", StringComparison.Ordinal));
    }
}
