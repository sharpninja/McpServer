using System.Text.Json;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-AGENT-001: Parses strict JSON handoff drafts. Never reparses compatibility output.</summary>
public interface IHandoffTodoDraftParser
{
    /// <summary>Parse extractor output as a single JSON object.</summary>
    HandoffParseResult Parse(string? responseText);
}

/// <summary>TR-HANDOFF-AGENT-001: Strict parse result.</summary>
public sealed class HandoffParseResult
{
    /// <summary>Whether a draft object was parsed.</summary>
    public bool Success { get; init; }

    /// <summary>Parsed draft when successful.</summary>
    public HandoffTodoDraft? Draft { get; init; }

    /// <summary>Parse diagnostics.</summary>
    public IReadOnlyList<HandoffDiagnostic> Diagnostics { get; init; } = [];
}

/// <inheritdoc />
public sealed class HandoffTodoDraftParser : IHandoffTodoDraftParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    /// <inheritdoc />
    public HandoffParseResult Parse(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return Fail("extract_malformed", null, "The extractor returned no JSON.");

        var trimmed = responseText.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.Contains("```", StringComparison.Ordinal))
            return Fail("extract_malformed", null, "Compatibility or fenced output is not accepted. The extractor must return a single JSON object.");

        if (trimmed[0] != '{')
            return Fail("extract_malformed", null, "Extractor output is not a JSON object.");

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(trimmed, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false,
            });
        }
        catch (JsonException)
        {
            return Fail("extract_malformed", null, "Extractor output is not valid JSON.");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return Fail("extract_malformed", null, "Extractor output is not a JSON object.");

            HandoffTodoDraft? draft;
            try
            {
                draft = JsonSerializer.Deserialize<HandoffTodoDraft>(trimmed, JsonOptions);
            }
            catch (JsonException)
            {
                return Fail("extract_malformed", null, "Extractor output does not match the handoff draft contract.");
            }

            if (draft is null)
                return Fail("extract_malformed", null, "Extractor output does not match the handoff draft contract.");

            var diagnostics = new List<HandoffDiagnostic>();
            RejectUnknownFields(document.RootElement, diagnostics);
            RequireString(document.RootElement, "id", diagnostics);
            RequireString(document.RootElement, "title", diagnostics);
            RequireString(document.RootElement, "section", diagnostics);
            RequireString(document.RootElement, "priority", diagnostics);
            if (!document.RootElement.TryGetProperty("confidence", out var confidence) ||
                confidence.ValueKind is not JsonValueKind.Number)
            {
                diagnostics.Add(new HandoffDiagnostic
                {
                    Code = "extract_missing_field",
                    Severity = HandoffDiagnosticSeverity.Error,
                    Field = "confidence",
                    Message = "The extractor omitted confidence.",
                });
            }

            if (document.RootElement.TryGetProperty("unknownSourceNotes", out var notes) &&
                notes.ValueKind == JsonValueKind.Array)
            {
                draft.UnknownSourceNotes = notes.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToArray();
            }

            return new HandoffParseResult
            {
                Success = diagnostics.TrueForAll(d => d.Severity != HandoffDiagnosticSeverity.Error),
                Draft = draft,
                Diagnostics = diagnostics,
            };
        }
    }

    private static readonly HashSet<string> KnownDraftFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id",
        "title",
        "section",
        "priority",
        "estimate",
        "description",
        "technicalDetails",
        "implementationTasks",
        "dependsOn",
        "functionalRequirements",
        "technicalRequirements",
        "confidence",
        "unknownSourceNotes",
    };

    private static void RejectUnknownFields(JsonElement root, List<HandoffDiagnostic> diagnostics)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (KnownDraftFields.Contains(property.Name))
                continue;

            diagnostics.Add(new HandoffDiagnostic
            {
                Code = "extract_unknown_field",
                Severity = HandoffDiagnosticSeverity.Error,
                Field = property.Name,
                Message = $"The extractor returned unknown field '{property.Name}'.",
            });
        }
    }

    private static void RequireString(JsonElement root, string name, List<HandoffDiagnostic> diagnostics)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            diagnostics.Add(new HandoffDiagnostic
            {
                Code = "extract_missing_field",
                Severity = HandoffDiagnosticSeverity.Error,
                Field = name,
                Message = $"The extractor omitted {name}.",
            });
        }
    }

    private static HandoffParseResult Fail(string code, string? field, string message)
        => new()
        {
            Success = false,
            Diagnostics =
            [
                new HandoffDiagnostic
                {
                    Code = code,
                    Severity = HandoffDiagnosticSeverity.Error,
                    Field = field,
                    Message = message,
                },
            ],
        };
}
