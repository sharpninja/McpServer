using System.Globalization;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-002: Decimal correlation ID with format <c>{baseId}.{counter}</c>.
/// The <see cref="BaseId"/> is a random 8-digit long that remains stable for the entire call tree.
/// The counter is a thread-safe incrementing integer that advances with each pipeline step or handler call,
/// giving every step in the distributed call tree a unique identifier.
/// </summary>
public sealed class CorrelationId
{
    private readonly long _baseId;
    private int _counter;

    /// <summary>
    /// Creates a new root correlation ID with a random base ID and counter starting at 0.
    /// </summary>
    public CorrelationId()
    {
        _baseId = Random.Shared.NextInt64(10000000, 99999999);
        _counter = 0;
    }

    /// <summary>
    /// Reconstitutes a correlation ID from known components (e.g. from an HTTP header).
    /// </summary>
    /// <param name="baseId">The base ID portion.</param>
    /// <param name="counter">The current counter value.</param>
    public CorrelationId(long baseId, int counter)
    {
        _baseId = baseId;
        _counter = counter;
    }

    /// <summary>The base ID — stable for the entire call tree.</summary>
    public long BaseId => _baseId;

    /// <summary>The current counter value (the step number of the most recent operation).</summary>
    public int Counter => _counter;

    /// <summary>The current correlation ID string in <c>{baseId}.{counter}</c> format.</summary>
    public string Current => $"{_baseId}.{_counter}";

    /// <summary>
    /// Advances the counter to the next step and returns the new correlation ID string.
    /// Thread-safe via <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    /// <returns>The new correlation ID string after incrementing.</returns>
    public string Next() => $"{_baseId}.{Interlocked.Increment(ref _counter)}";

    /// <summary>
    /// Parses a correlation ID string in <c>{baseId}.{counter}</c> format.
    /// </summary>
    /// <param name="value">The string to parse (e.g. <c>"48291735.3"</c>).</param>
    /// <returns>A reconstituted <see cref="CorrelationId"/>.</returns>
    /// <exception cref="FormatException">Thrown if the string is not in the expected format.</exception>
    public static CorrelationId Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var dotIndex = value.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex < 1 || dotIndex >= value.Length - 1)
            throw new FormatException($"Invalid correlation ID format: '{value}'. Expected '{{baseId}}.{{counter}}'.");

        var baseId = long.Parse(value.AsSpan(0, dotIndex), CultureInfo.InvariantCulture);
        var counter = int.Parse(value.AsSpan(dotIndex + 1), CultureInfo.InvariantCulture);
        return new CorrelationId(baseId, counter);
    }

    /// <summary>
    /// Attempts to parse a correlation ID string. Returns <c>null</c> if parsing fails.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>A <see cref="CorrelationId"/> or <c>null</c>.</returns>
    public static CorrelationId? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return Parse(value); }
        catch { return null; }
    }

    /// <summary>HTTP header name for correlation ID propagation.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <inheritdoc />
    public override string ToString() => Current;
}
