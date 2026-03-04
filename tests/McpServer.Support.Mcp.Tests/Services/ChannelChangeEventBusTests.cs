using McpServer.Support.Mcp.Notifications;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Unit tests for ChannelChangeEventBus pub/sub behavior.</summary>
public sealed class ChannelChangeEventBusTests
{
    [Fact]
    public async Task PublishAsync_NoSubscribers_DoesNotThrow()
    {
        var sut = new ChannelChangeEventBus();
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

    [Fact]
    public async Task PublishAsync_MultipleSubscribers_AllReceiveEvent()
    {
        var sut = new ChannelChangeEventBus();
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

    [Fact]
    public async Task SubscribeAsync_Cancellation_StopsEnumeration()
    {
        var sut = new ChannelChangeEventBus();
        using var cts = new CancellationTokenSource();
        var sub = sut.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await sub.MoveNextAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
    }
}
