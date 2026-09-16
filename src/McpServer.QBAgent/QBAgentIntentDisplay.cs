using System.Text.Json;

namespace McpServer.QBAgent;

/// <summary>Strips Arbiter intent JSON from terminal output unless show-intent is on.</summary>
internal static class QBAgentIntentDisplay
{
    internal static string FormatForDisplay(string? output, bool showIntent)
    {
        var intent = TryReadIntent(output) ?? "missing";
        if (showIntent)
        {
            var body = string.IsNullOrWhiteSpace(output) ? string.Empty : output;
            return string.IsNullOrWhiteSpace(body)
                ? $"intent: {intent}"
                : $"intent: {intent}{Environment.NewLine}{body}";
        }

        if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

        return TryReadContent(output) ?? output;
    }

    internal static string? TryReadIntent(string? output)
    {
        var json = ExtractObject(output);
        if (json is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            if (!document.RootElement.TryGetProperty("intent", out var intent)
                || intent.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(intent.GetString()))
            {
                return null;
            }

            return intent.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string? TryReadContent(string output)
    {
        var json = ExtractObject(output);
        if (json is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            if (!document.RootElement.TryGetProperty("intent", out var intentEl)
                || intentEl.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(intentEl.GetString()))
            {
                return null;
            }

            if (document.RootElement.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
            {
                return content.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractObject(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        var trimmed = output.Trim();
        var marker = trimmed.IndexOf("\"intent\"", StringComparison.Ordinal);
        if (marker < 0)
            return trimmed.StartsWith('{') ? trimmed : null;
        var open = trimmed.LastIndexOf('{', marker);
        if (open < 0)
            return null;
        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = open; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];
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
                    return trimmed[open..(i + 1)];
            }
        }

        return null;
    }
}
