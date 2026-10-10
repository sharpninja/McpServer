using System.Text.Json;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Arbiter protocol envelope: an explicit <c>intent</c> so QuadBrain does not guess from prose.
/// </summary>
public sealed class QuadBrainArbiterEnvelope
{
    /// <summary>User-facing completed answer.</summary>
    public const string IntentFinal = "final";

    /// <summary>More work is required; QuadBrain must loop.</summary>
    public const string IntentContinue = "continue";

    /// <summary>Run the listed tools, then loop.</summary>
    public const string IntentToolCalls = "tool_calls";

    /// <summary>Reject the role evidence; orchestration may vote.</summary>
    public const string IntentReject = "reject";

    /// <summary>Machine intent: final, continue, tool_calls, or reject.</summary>
    public string Intent { get; init; } = IntentContinue;

    /// <summary>User-facing text when <see cref="Intent"/> is final, continue, or reject.</summary>
    public string? Content { get; init; }

    /// <summary>Tool calls when <see cref="Intent"/> is <see cref="IntentToolCalls"/>.</summary>
    public IReadOnlyList<OpenAiToolCall> ToolCalls { get; init; } = [];

    /// <summary>Raw JSON object that parsed successfully.</summary>
    public string? RawJson { get; init; }

    /// <summary>True when <see cref="Intent"/> is <see cref="IntentFinal"/>.</summary>
    public bool IsFinal => string.Equals(Intent, IntentFinal, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the turn is not a user-facing answer yet.</summary>
    public bool IsContinue =>
        string.Equals(Intent, IntentContinue, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Intent, IntentReject, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when <see cref="ToolCalls"/> is non-empty.</summary>
    public bool HasToolCalls => ToolCalls.Count > 0;

    /// <summary>True when the envelope is missing or intent is continue/reject (not a user-facing answer).</summary>
    public static bool RequiresAnotherRound(QuadBrainArbiterEnvelope? envelope)
        => envelope is null || envelope.IsContinue;

    /// <summary>Parses an Arbiter envelope or a legacy <c>{"tool_calls":[...]}</c> object.</summary>
    public static QuadBrainArbiterEnvelope? TryParse(string? output)
    {
        var json = ExtractJsonObject(output);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var root = document.RootElement;
            var toolCalls = ReadToolCalls(root);
            var hasIntent = root.TryGetProperty("intent", out var intentEl)
                            && intentEl.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(intentEl.GetString());
            var hasLegacyTools = toolCalls.Count > 0 && !hasIntent;
            if (!hasIntent && !hasLegacyTools)
                return null;

            var intent = hasIntent
                ? intentEl.GetString()!.Trim()
                : IntentToolCalls;
            var content = root.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String
                ? contentEl.GetString()
                : null;

            return new QuadBrainArbiterEnvelope
            {
                Intent = intent,
                Content = content,
                ToolCalls = toolCalls,
                RawJson = json,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Display text. When <paramref name="showIntent"/> is false, the JSON envelope is omitted
    /// and only <see cref="Content"/> is shown.
    /// </summary>
    public static string FormatForDisplay(string? output, bool showIntent)
    {
        if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

        var envelope = TryParse(output);
        if (envelope is null || showIntent)
            return output;

        if (!string.IsNullOrWhiteSpace(envelope.Content))
            return envelope.Content!;

        if (envelope.HasToolCalls)
            return string.Empty;

        return string.Empty;
    }

    private static List<OpenAiToolCall> ReadToolCalls(JsonElement root)
    {
        var list = new List<OpenAiToolCall>();
        if (!root.TryGetProperty("tool_calls", out var calls) || calls.ValueKind != JsonValueKind.Array)
            return list;

        var index = 0;
        foreach (var call in calls.EnumerateArray())
        {
            if (call.ValueKind != JsonValueKind.Object
                || !call.TryGetProperty("name", out var name)
                || name.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var arguments = call.TryGetProperty("arguments", out var args)
                ? args.GetRawText()
                : "{}";
            list.Add(new OpenAiToolCall
            {
                Id = $"call_{index}",
                Function = new OpenAiFunctionCall
                {
                    Name = name.GetString() ?? string.Empty,
                    Arguments = arguments,
                },
            });
            index++;
        }

        return list;
    }

    private static string? ExtractJsonObject(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        var trimmed = output.Trim();
        var fence = trimmed.IndexOf("```", StringComparison.Ordinal);
        if (fence >= 0)
        {
            var start = trimmed.IndexOf('\n', fence);
            var end = start >= 0 ? trimmed.IndexOf("```", start + 1, StringComparison.Ordinal) : -1;
            if (start >= 0 && end > start)
                trimmed = trimmed[(start + 1)..end].Trim();
        }

        var intent = trimmed.IndexOf("\"intent\"", StringComparison.Ordinal);
        var tools = trimmed.IndexOf("\"tool_calls\"", StringComparison.Ordinal);
        var marker = intent >= 0 && (tools < 0 || intent < tools) ? intent : tools;
        if (marker < 0)
        {
            if (trimmed.Length > 0 && trimmed[0] == '{')
                return SliceBalancedObject(trimmed, 0);
            return null;
        }

        var open = trimmed.LastIndexOf('{', marker);
        return open < 0 ? null : SliceBalancedObject(trimmed, open);
    }

    private static string? SliceBalancedObject(string text, int openIndex)
    {
        if (openIndex < 0 || openIndex >= text.Length || text[openIndex] != '{')
            return null;

        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = openIndex; i < text.Length; i++)
        {
            var ch = text[i];
            if (inString)
            {
                if (escape)
                    escape = false;
                else if (ch == '\\')
                    escape = true;
                else if (ch == '"')
                    inString = false;
                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
                depth++;
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                    return text[openIndex..(i + 1)];
            }
        }

        return null;
    }
}
