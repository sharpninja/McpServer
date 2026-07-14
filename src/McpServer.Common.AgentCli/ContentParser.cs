using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace McpServer.Common.AgentCli;

/// <summary>TR-CLI-001: Detects content type and attempts deserialization of CLI output.</summary>
public static class ContentParser
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Attempt to parse text as JSON. Returns the deserialized object or null.</summary>
    public static object? TryParseJson(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var trimmed = text.Trim();
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                return document.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Trace.TraceWarning(ex.ToString());
                return null;
            }
        }
        return null;
    }

    /// <summary>Attempt to parse text as JSON and deserialize to <typeparamref name="T"/>.</summary>
    [RequiresUnreferencedCode("Generic CLI output parsing requires runtime serializer metadata for arbitrary caller-supplied types.")]
    public static T? TryParseJson<T>(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var trimmed = text.Trim();
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                return JsonSerializer.Deserialize<T>(trimmed, s_jsonOptions);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Trace.TraceWarning(ex.ToString());
                return default;
            }
        }
        return default;
    }

    /// <summary>Attempt to parse text as simple YAML (flat key: value, lists).</summary>
    public static Dictionary<string, object?>? TryParseYaml(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        var yamlLineCount = 0;
        foreach (var line in lines)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^[a-zA-Z_][a-zA-Z0-9_-]*\s*:"))
                yamlLineCount++;
        }
        if (yamlLineCount == 0)
            return null;

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                i++;
                continue;
            }

            var match = System.Text.RegularExpressions.Regex.Match(line, @"^([a-zA-Z_][a-zA-Z0-9_-]*)\s*:\s*(.*)$");
            if (!match.Success)
            {
                i++;
                continue;
            }

            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value.Trim();

            // Block list
            if (value is "" or "[]")
            {
                var arr = new List<object?>();
                i++;
                while (i < lines.Length && System.Text.RegularExpressions.Regex.IsMatch(lines[i], @"^\s+-\s"))
                {
                    var itemMatch = System.Text.RegularExpressions.Regex.Match(lines[i], @"^\s+-\s*(.+)$");
                    if (itemMatch.Success)
                        arr.Add(ParseScalar(itemMatch.Groups[1].Value.Trim()));
                    i++;
                }
                result[key] = value == "[]" && arr.Count == 0 ? new List<object?>() : arr;
                continue;
            }

            // Inline list: [a, b, c]
            if (value.StartsWith('[') && value.EndsWith(']'))
            {
                var inner = value[1..^1].Trim();
                result[key] = string.IsNullOrEmpty(inner)
                    ? new List<object?>()
                    : inner.Split(',').Select(s => ParseScalar(s.Trim())).ToList();
                i++;
                continue;
            }

            result[key] = ParseScalar(value);
            i++;
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>Detect content type and attempt deserialization.</summary>
    public static (AgentCliContentType ContentType, object? Parsed) DetectAndParse(string text)
    {
        var jsonResult = TryParseJson(text);
        if (jsonResult is not null)
            return (AgentCliContentType.Json, jsonResult);

        var yamlResult = TryParseYaml(text);
        if (yamlResult is not null)
            return (AgentCliContentType.Yaml, yamlResult);

        return (AgentCliContentType.Text, null);
    }

    /// <summary>Detect content type and deserialize as <typeparamref name="T"/>.</summary>
    [RequiresUnreferencedCode("Generic CLI output parsing requires runtime serializer metadata for arbitrary caller-supplied types.")]
    public static (AgentCliContentType ContentType, T? Parsed) DetectAndParse<T>(string text)
    {
        var typed = TryParseJson<T>(text);
        if (typed is not null)
            return (AgentCliContentType.Json, typed);

        // YAML doesn't support generic deserialization in this minimal parser
        var yamlResult = TryParseYaml(text);
        if (yamlResult is not null)
        {
            // Attempt JSON round-trip for typed deserialization
            try
            {
                var json = JsonSerializer.Serialize(yamlResult, s_jsonOptions);
                var result = JsonSerializer.Deserialize<T>(json, s_jsonOptions);
                return (AgentCliContentType.Yaml, result);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Trace.TraceWarning(ex.ToString());
                return (AgentCliContentType.Yaml, default);
            }
        }

        return (AgentCliContentType.Text, default);
    }

    private static object? ParseScalar(string s)
    {
        if ((s.StartsWith('"') && s.EndsWith('"')) || (s.StartsWith('\'') && s.EndsWith('\'')))
            s = s[1..^1];
        if (s is "null" or "~") return null;
        if (s == "true") return true;
        if (s == "false") return false;
        if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var n))
            return n;
        return s;
    }
}
