using System.Text.Json;
using McpServer.Support.Mcp.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace McpServer.Support.Mcp.Tests.Logging;

/// <summary>TR-PLANNED-CORE-013, TR-MCP-LOG-003, TEST-MCP-098: Unit tests for ParseableEventFormatter field-cap enforcement and reserved-field preservation using synthetic Serilog log events with deterministic property names.</summary>
public sealed class ParseableEventFormatterTests
{
    private static readonly MessageTemplate s_messageTemplate = new MessageTemplateParser().Parse("Example message");

    /// <summary>TR-PLANNED-CORE-013, TR-MCP-LOG-003, TEST-MCP-098: Verifies a log event with 300 scalar properties plus reserved-name collisions is capped at 250 total emitted fields so Parseable receives a compliant payload and reserved metadata fields keep their canonical values.</summary>
    [Fact]
    public void ToParseableObject_WhenPropertiesExceedMaximumFieldCount_CapsOutputAndPreservesReservedFields()
    {
        var exception = new InvalidOperationException("boom");
        var logEvent = CreateLogEvent(propertyCount: 300, includeReservedNameCollisions: true, exception: exception);

        var result = ParseableEventFormatter.ToParseableObject(logEvent);

        Assert.Equal(ParseableEventFormatter.MaxFieldCount, result.Count);
        Assert.Equal("2026-03-22T15:30:00.000Z", result["timestamp"]);
        Assert.Equal("warning", result["level"]);
        Assert.Equal("Example message", result["message"]);
        Assert.Equal(exception.ToString(), result["exception"]);
        Assert.Equal("value-000", result["field000"]);
        Assert.Equal("value-245", result["field245"]);
        Assert.DoesNotContain("field246", result.Keys);
    }

    /// <summary>TR-PLANNED-CORE-013, TR-MCP-LOG-003, TEST-MCP-098: Verifies formatter JSON output stays parseable and retains all available fields when the synthetic event uses only a small number of non-reserved properties, proving the cap does not truncate normal events.</summary>
    [Fact]
    public void Format_WhenEventIsBelowFieldLimit_WritesAllNonReservedFieldsAsJson()
    {
        var formatter = new ParseableEventFormatter();
        var logEvent = CreateLogEvent(propertyCount: 2, includeReservedNameCollisions: true, exception: null);
        using var writer = new StringWriter();

        formatter.Format(logEvent, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var root = document.RootElement;
        var propertyCount = root.EnumerateObject().Count();

        Assert.Equal(5, propertyCount);
        Assert.Equal("2026-03-22T15:30:00.000Z", root.GetProperty("timestamp").GetString());
        Assert.Equal("warning", root.GetProperty("level").GetString());
        Assert.Equal("Example message", root.GetProperty("message").GetString());
        Assert.Equal("value-000", root.GetProperty("field000").GetString());
        Assert.Equal("value-001", root.GetProperty("field001").GetString());
    }

    private static LogEvent CreateLogEvent(int propertyCount, bool includeReservedNameCollisions, Exception? exception)
    {
        var properties = new List<LogEventProperty>();
        if (includeReservedNameCollisions)
        {
            properties.Add(new LogEventProperty("timestamp", new ScalarValue("overridden-timestamp")));
            properties.Add(new LogEventProperty("level", new ScalarValue("overridden-level")));
            properties.Add(new LogEventProperty("message", new ScalarValue("overridden-message")));
            properties.Add(new LogEventProperty("exception", new ScalarValue("overridden-exception")));
        }

        for (var i = 0; i < propertyCount; i++)
        {
            var key = $"field{i:000}";
            var value = $"value-{i:000}";
            properties.Add(new LogEventProperty(key, new ScalarValue(value)));
        }

        return new LogEvent(
            new DateTimeOffset(2026, 3, 22, 15, 30, 0, TimeSpan.Zero),
            LogEventLevel.Warning,
            exception,
            s_messageTemplate,
            properties);
    }
}
