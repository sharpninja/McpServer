using System.Collections.Concurrent;
using System.Net;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-014: Tracks failed pairing sign-in attempts and enforces temporary lockouts.
/// </summary>
internal sealed class PairingLoginAttemptGuard
{
    private const int FailedAttemptThreshold = 5;
    private const int FailedAttemptIpLimit = 20;
    private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan IpWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BaseLockout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxLockout = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, FixedWindowCounter> _ipFailures = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PrincipalFailureState> _principalFailures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<DateTimeOffset> _utcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="PairingLoginAttemptGuard"/> class.
    /// </summary>
    public PairingLoginAttemptGuard()
        : this(static () => DateTimeOffset.UtcNow)
    {
    }

    internal PairingLoginAttemptGuard(Func<DateTimeOffset> utcNow)
    {
        _utcNow = utcNow;
    }

    /// <summary>
    /// Returns <c>true</c> when a pairing sign-in attempt is currently permitted.
    /// </summary>
    public bool TryAcquire(string? username, IPAddress? remoteIp, out TimeSpan retryAfter)
    {
        var now = _utcNow();
        PurgeExpired(now);

        retryAfter = TimeSpan.Zero;
        var ipKey = BuildIpKey(remoteIp);
        if (_ipFailures.TryGetValue(ipKey, out var ipCounter))
        {
            lock (ipCounter.Gate)
            {
                if ((now - ipCounter.WindowStart) >= IpWindow)
                {
                    ipCounter.WindowStart = now;
                    ipCounter.Count = 0;
                }
                else if (ipCounter.Count >= FailedAttemptIpLimit)
                {
                    retryAfter = IpWindow - (now - ipCounter.WindowStart);
                    if (retryAfter < TimeSpan.Zero)
                        retryAfter = TimeSpan.Zero;

                    return false;
                }
            }
        }

        var principalKey = BuildPrincipalKey(username, remoteIp);
        if (_principalFailures.TryGetValue(principalKey, out var principalState))
        {
            lock (principalState.Gate)
            {
                if ((now - principalState.LastFailureUtc) >= FailedAttemptWindow)
                {
                    principalState.FailureCount = 0;
                    principalState.LockedUntilUtc = null;
                }

                if (principalState.LockedUntilUtc is { } lockedUntilUtc && lockedUntilUtc > now)
                {
                    var principalRetryAfter = lockedUntilUtc - now;
                    if (principalRetryAfter > retryAfter)
                        retryAfter = principalRetryAfter;

                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Records a failed pairing sign-in attempt for the supplied user and remote IP.
    /// </summary>
    public void RecordFailure(string? username, IPAddress? remoteIp)
    {
        var now = _utcNow();
        PurgeExpired(now);

        var ipKey = BuildIpKey(remoteIp);
        var ipCounter = _ipFailures.GetOrAdd(ipKey, _ => new FixedWindowCounter(now));
        lock (ipCounter.Gate)
        {
            if ((now - ipCounter.WindowStart) >= IpWindow)
            {
                ipCounter.WindowStart = now;
                ipCounter.Count = 0;
            }

            ipCounter.Count++;
        }

        var principalKey = BuildPrincipalKey(username, remoteIp);
        var principalState = _principalFailures.GetOrAdd(principalKey, _ => new PrincipalFailureState(now));
        lock (principalState.Gate)
        {
            if ((now - principalState.LastFailureUtc) >= FailedAttemptWindow)
            {
                principalState.FailureCount = 0;
                principalState.LockedUntilUtc = null;
            }

            principalState.FailureCount++;
            principalState.LastFailureUtc = now;

            if (principalState.FailureCount >= FailedAttemptThreshold)
            {
                var multiplier = 1 << Math.Min(principalState.FailureCount - FailedAttemptThreshold, 4);
                var lockoutTicks = Math.Min(BaseLockout.Ticks * multiplier, MaxLockout.Ticks);
                principalState.LockedUntilUtc = now.AddTicks(lockoutTicks);
            }
        }
    }

    /// <summary>
    /// Clears accumulated pairing sign-in failures for the supplied user and remote IP.
    /// </summary>
    public void RecordSuccess(string? username, IPAddress? remoteIp)
    {
        _principalFailures.TryRemove(BuildPrincipalKey(username, remoteIp), out _);
    }

    private void PurgeExpired(DateTimeOffset now)
    {
        foreach (var entry in _ipFailures)
        {
            var remove = false;
            lock (entry.Value.Gate)
            {
                remove = (now - entry.Value.WindowStart) >= IpWindow;
            }

            if (remove)
                _ipFailures.TryRemove(entry.Key, out _);
        }

        foreach (var entry in _principalFailures)
        {
            var remove = false;
            lock (entry.Value.Gate)
            {
                var lastRelevantUtc = entry.Value.LockedUntilUtc is { } lockedUntilUtc && lockedUntilUtc > entry.Value.LastFailureUtc
                    ? lockedUntilUtc
                    : entry.Value.LastFailureUtc;
                remove = (now - lastRelevantUtc) >= FailedAttemptWindow;
            }

            if (remove)
                _principalFailures.TryRemove(entry.Key, out _);
        }
    }

    private static string BuildIpKey(IPAddress? remoteIp) => remoteIp?.ToString() ?? "loopback";

    private static string BuildPrincipalKey(string? username, IPAddress? remoteIp)
        => $"{BuildIpKey(remoteIp)}|{NormalizeUsername(username)}";

    private static string NormalizeUsername(string? username)
        => string.IsNullOrWhiteSpace(username) ? "(empty)" : username.Trim();

    private sealed class FixedWindowCounter
    {
        public FixedWindowCounter(DateTimeOffset windowStart)
        {
            WindowStart = windowStart;
        }

        public object Gate { get; } = new();

        public DateTimeOffset WindowStart { get; set; }

        public int Count { get; set; }
    }

    private sealed class PrincipalFailureState
    {
        public PrincipalFailureState(DateTimeOffset lastFailureUtc)
        {
            LastFailureUtc = lastFailureUtc;
        }

        public object Gate { get; } = new();

        public int FailureCount { get; set; }

        public DateTimeOffset LastFailureUtc { get; set; }

        public DateTimeOffset? LockedUntilUtc { get; set; }
    }
}
