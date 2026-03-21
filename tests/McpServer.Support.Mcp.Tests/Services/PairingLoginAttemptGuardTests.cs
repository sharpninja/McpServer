using System.Net;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-014: Verifies pairing sign-in throttling and lockout behavior using a controllable clock so brute-force
/// resistance can be validated deterministically without sleeping in tests.
/// </summary>
public sealed class PairingLoginAttemptGuardTests
{
    /// <summary>
    /// FR-MCP-014: Verifies that repeated failed attempts for the same username and remote IP produce a temporary
    /// lockout and that the lockout clears after the reported retry interval elapses.
    /// </summary>
    [Fact]
    public void TryAcquire_AfterThresholdFailures_BlocksUntilRetryAfterExpires()
    {
        var now = DateTimeOffset.Parse("2026-03-21T00:00:00Z");
        var guard = new PairingLoginAttemptGuard(() => now);
        var remoteIp = IPAddress.Loopback;

        for (var i = 0; i < 5; i++)
            guard.RecordFailure("admin", remoteIp);

        Assert.False(guard.TryAcquire("admin", remoteIp, out var retryAfter));
        Assert.True(retryAfter > TimeSpan.Zero);

        now = now.Add(retryAfter).Add(TimeSpan.FromSeconds(1));

        Assert.True(guard.TryAcquire("admin", remoteIp, out var clearedRetryAfter));
        Assert.Equal(TimeSpan.Zero, clearedRetryAfter);
    }

    /// <summary>
    /// FR-MCP-014: Verifies that a successful authentication resets the principal-specific failure history so
    /// earlier mistakes do not cause a later lockout for the same username and remote IP.
    /// </summary>
    [Fact]
    public void RecordSuccess_ClearsPrincipalFailureHistory()
    {
        var now = DateTimeOffset.Parse("2026-03-21T00:00:00Z");
        var guard = new PairingLoginAttemptGuard(() => now);
        var remoteIp = IPAddress.Loopback;

        for (var i = 0; i < 4; i++)
            guard.RecordFailure("admin", remoteIp);

        guard.RecordSuccess("admin", remoteIp);

        for (var i = 0; i < 4; i++)
            guard.RecordFailure("admin", remoteIp);

        Assert.True(guard.TryAcquire("admin", remoteIp, out var retryAfter));
        Assert.Equal(TimeSpan.Zero, retryAfter);
    }

    /// <summary>
    /// FR-MCP-014: Verifies that one remote IP cannot make unlimited failed pairing attempts across many usernames
    /// because the IP-level failure window starts rejecting new attempts until the fixed window expires.
    /// </summary>
    [Fact]
    public void TryAcquire_AfterIpFailureWindowExceeded_ReturnsFalseUntilWindowResets()
    {
        var now = DateTimeOffset.Parse("2026-03-21T00:00:00Z");
        var guard = new PairingLoginAttemptGuard(() => now);
        var remoteIp = IPAddress.Parse("203.0.113.7");

        for (var i = 0; i < 20; i++)
            guard.RecordFailure($"user-{i}", remoteIp);

        Assert.False(guard.TryAcquire("another-user", remoteIp, out var retryAfter));
        Assert.True(retryAfter > TimeSpan.Zero);

        now = now.AddMinutes(5).AddSeconds(1);

        Assert.True(guard.TryAcquire("another-user", remoteIp, out var clearedRetryAfter));
        Assert.Equal(TimeSpan.Zero, clearedRetryAfter);
    }
}
