using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-DB-006 / TEST-MCP-DB-006 (BUG-TRIAGE-079): the KeyedAsyncLock serializes concurrent work on
/// the same key (so a burst of session-log turn deletes on one session cannot poison the SQL Server
/// pool) while letting different keys run concurrently. The pair discriminates a correct keyed lock
/// from a no-op (fails SameKey) and from a global lock (fails DifferentKeys). Deterministic: uses
/// Task.WhenAny timing rather than blocking waits.
/// </summary>
public sealed class KeyedAsyncLockTests
{
    /// <summary>A second acquire of the same key blocks until the first is released.</summary>
    [Fact]
    public async Task AcquireAsync_SameKey_BlocksUntilReleased()
    {
        var ct = TestContext.Current.CancellationToken;
        var sut = new KeyedAsyncLock();

        var first = await sut.AcquireAsync("session-A", ct).ConfigureAwait(true);
        var second = sut.AcquireAsync("session-A", ct);

        var early = await Task.WhenAny(second, Task.Delay(250, ct)).ConfigureAwait(true);
        Assert.NotSame(second, early);

        await first.DisposeAsync().ConfigureAwait(true);

        var afterRelease = await Task.WhenAny(second, Task.Delay(2000, ct)).ConfigureAwait(true);
        Assert.Same(second, afterRelease);
        await (await second.ConfigureAwait(true)).DisposeAsync().ConfigureAwait(true);
    }

    /// <summary>A second acquire of a different key completes while the first key is still held.</summary>
    [Fact]
    public async Task AcquireAsync_DifferentKeys_DoNotBlockEachOther()
    {
        var ct = TestContext.Current.CancellationToken;
        var sut = new KeyedAsyncLock();

        await using (await sut.AcquireAsync("session-A", ct).ConfigureAwait(true))
        {
            var acquireB = sut.AcquireAsync("session-B", ct);
            var completed = await Task.WhenAny(acquireB, Task.Delay(2000, ct)).ConfigureAwait(true);
            Assert.Same(acquireB, completed);
            await (await acquireB.ConfigureAwait(true)).DisposeAsync().ConfigureAwait(true);
        }
    }
}
