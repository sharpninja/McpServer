// TR-PLANNED-013: Batch formatter that produces Parseable-compatible JSON (array of flat objects).

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Http;

namespace McpServer.Support.Mcp.Logging;

/// <summary>
/// TR-PLANNED-013: Formats Serilog batches for Parseable ingest API per
/// https://www.parseable.com/docs/ingest-data/programming-languages/dotnet.
/// Uses <see cref="ParseableEventFormatter"/> shape; sink may call either LogEvent or string overload.
/// </summary>
public sealed class ParseableBatchFormatter : IBatchFormatter
{
    /// <inheritdoc />
    public void Format(IEnumerable<LogEvent> logEvents, ITextFormatter formatter, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvents);
        ArgumentNullException.ThrowIfNull(output);

        var list = new List<Dictionary<string, string>>();
        foreach (var logEvent in logEvents)
            list.Add(ParseableEventFormatter.ToParseableObject(logEvent));

        if (list.Count == 0)
            return;

        var json = JsonSerializer.Serialize(list, ParseableEventFormatter.s_jsonOptions);
        output.Write(json);
    }

    /// <inheritdoc />
    /// <remarks>Called when the sink pre-formats each event with the configured text formatter (e.g. <see cref="ParseableEventFormatter"/>).</remarks>
    public void Format(IEnumerable<string> logEvents, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvents);
        ArgumentNullException.ThrowIfNull(output);

        var list = logEvents.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (list.Count == 0)
            return;

        output.Write('[');
        var first = true;
        foreach (var s in list)
        {
            if (!first) output.Write(',');
            output.Write(s);
            first = false;
        }
        output.Write(']');
    }
}
