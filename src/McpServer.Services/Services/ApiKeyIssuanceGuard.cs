using System.Collections.Concurrent;
using System.Net;

namespace McpServer.Support.Mcp.Services;

internal sealed class ApiKeyIssuanceGuard
{
    private const int PermitLimit = 30;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, FixedWindowCounter> _counters = new(StringComparer.Ordinal);

    public bool TryAcquire(IPAddress? remoteIp, out TimeSpan retryAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var key = remoteIp?.ToString() ?? "loopback";
        var counter = _counters.GetOrAdd(key, _ => new FixedWindowCounter(now));

        lock (counter.Gate)
        {
            if ((now - counter.WindowStart) >= Window)
            {
                counter.WindowStart = now;
                counter.Count = 0;
            }

            if (counter.Count >= PermitLimit)
            {
                retryAfter = Window - (now - counter.WindowStart);
                if (retryAfter < TimeSpan.Zero)
                    retryAfter = TimeSpan.Zero;

                return false;
            }

            counter.Count++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

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
}
