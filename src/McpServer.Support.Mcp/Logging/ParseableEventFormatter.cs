// TR-PLANNED-013: Formats a single log event as Parseable-compatible JSON (per-event formatter for HTTP sink).

using System.Globalization;
using System.IO;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace McpServer.Support.Mcp.Logging;

/// <summary>
/// TR-PLANNED-013: Formats one log event as a single JSON object for Parseable per
/// https://www.parseable.com/docs/ingest-data/programming-languages/dotnet.
/// Used as the HTTP sink's text formatter so batched payloads use this shape.
/// </summary>
public sealed class ParseableEventFormatter : ITextFormatter
{
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    internal static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = false };

    /// <inheritdoc />
    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        var obj = ToParseableObject(logEvent);
        var json = JsonSerializer.Serialize(obj, s_jsonOptions);
        output.Write(json);
    }

    /// <summary>Builds the Parseable flat string dictionary for one event (shared with batch formatter).</summary>
    internal static Dictionary<string, string> ToParseableObject(LogEvent logEvent)
    {
        var obj = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["timestamp"] = logEvent.Timestamp.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture),
#pragma warning disable CA1308 // Parseable doc: level = logEvent.Level.ToString().ToLower()
            ["level"] = logEvent.Level.ToString().ToLowerInvariant(),
#pragma warning restore CA1308
            ["message"] = logEvent.RenderMessage(CultureInfo.InvariantCulture)
        };
        if (logEvent.Exception != null)
            obj["exception"] = logEvent.Exception.ToString();
        foreach (var prop in logEvent.Properties)
        {
            var s = ToStringValue(prop.Value) ?? string.Empty;
            obj[prop.Key] = s.Trim('"');
        }
        return obj;
    }

    private static string? ToStringValue(LogEventPropertyValue value)
    {
        if (value is ScalarValue scalar)
        {
            var v = scalar.Value;
            if (v == null) return string.Empty;
            if (v is IFormattable f)
                return f.ToString(null, CultureInfo.InvariantCulture);
            return v.ToString() ?? string.Empty;
        }
        return value.ToString();
    }
}
