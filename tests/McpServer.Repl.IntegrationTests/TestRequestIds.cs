using System.Globalization;
using System.Threading;

namespace McpServer.Repl.IntegrationTests;

internal static class TestRequestIds
{
    private static int _nextId;

    public static string Next(string suffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suffix);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
        var sequence = Interlocked.Increment(ref _nextId);
        return $"req-{timestamp}Z-{suffix}-{sequence:x8}";
    }
}
