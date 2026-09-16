using System.Text.Json;
using SharpNinja.AiUnit.Frontier;
using AiUnitRuntime = SharpNinja.AiUnit.Xunit.AiStrategyFixture;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC3: aiUnit companion evaluator for plugin Session Log receipts.
/// Deterministic failures (invalid JSON, missing fields, contradictions, deterministicFailure=true)
/// are never overridden to Valid=true. Success-path evaluation invokes SharpNinja.aiUnit.
/// </summary>
public static class AiStrategyFixture
{
    private static readonly string[] RequiredFields = ["agent", "cacheFolder", "entrypoint", "status"];

    private const string SystemPrompt =
        "Return only JSON with keys valid (boolean), missingFields (string array), and contradictions (string array). " +
        "Never set valid=true when deterministicFailure is true, required workflow fields are missing, or JSON is malformed.";

    /// <summary>
    /// Evaluates a redacted persistence receipt and returns strict JSON fields valid, missingFields, and contradictions.
    /// </summary>
    /// <param name="redactedReceipt">Redacted persistence receipt or session artifact.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>Semantic completeness result.</returns>
    public static async Task<AiStrategyEvaluation> EvaluateAsync(string redactedReceipt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redactedReceipt);
        cancellationToken.ThrowIfCancellationRequested();

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(redactedReceipt);
            if (root.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return InvalidJson();
            }
        }
        catch (JsonException)
        {
            return InvalidJson();
        }

        var missing = new List<string>();
        foreach (var field in RequiredFields)
        {
            if (!root.TryGetProperty(field, out var property)
                || property.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(property.GetString()))
            {
                missing.Add(field);
            }
        }

        var contradictions = new List<string>();
        if (root.TryGetProperty("status", out var status)
            && status.ValueKind == JsonValueKind.String
            && !string.Equals(status.GetString(), "completed", StringComparison.OrdinalIgnoreCase))
        {
            contradictions.Add("status");
        }

        var deterministicFailure = root.TryGetProperty("deterministicFailure", out var flag)
            && flag.ValueKind == JsonValueKind.True;

        if (missing.Count > 0 || contradictions.Count > 0 || deterministicFailure)
        {
            return new AiStrategyEvaluation
            {
                Valid = false,
                MissingFields = missing,
                Contradictions = contradictions,
            };
        }

        var client = AiUnitRuntime.Default.Client;
        if (client is null)
        {
            return new AiStrategyEvaluation
            {
                Valid = false,
                MissingFields = ["aiUnit-client"],
                Contradictions = ["unavailable"],
            };
        }

        try
        {
            var response = await client.SendAsync(new FrontierRequest(SystemPrompt, redactedReceipt))
                .WaitAsync(TimeSpan.FromSeconds(60), cancellationToken)
                .ConfigureAwait(false);
            if (response.Error is not null || string.IsNullOrWhiteSpace(response.Text))
            {
                return new AiStrategyEvaluation
                {
                    Valid = false,
                    MissingFields = ["aiUnit-response"],
                    Contradictions = ["unavailable"],
                };
            }

            try
            {
                return ParseAiUnitPayload(ExtractJsonObject(response.Text));
            }
            catch (JsonException)
            {
                return new AiStrategyEvaluation
                {
                    Valid = false,
                    MissingFields = ["aiUnit-json"],
                    Contradictions = ["malformed"],
                };
            }
        }
        catch (TimeoutException)
        {
            return new AiStrategyEvaluation
            {
                Valid = false,
                MissingFields = ["aiUnit-response"],
                Contradictions = ["timeout"],
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AiStrategyEvaluation
            {
                Valid = false,
                MissingFields = ["aiUnit-response"],
                Contradictions = ["timeout"],
            };
        }
    }

    /// <summary>
    /// Parses the required aiUnit JSON object. <c>valid</c> must be the JSON boolean true;
    /// empty missing/contradiction arrays do not override a false valid flag.
    /// </summary>
    /// <param name="json">JSON object text from the model.</param>
    /// <returns>Parsed evaluation.</returns>
    public static AiStrategyEvaluation ParseAiUnitPayload(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var payload = JsonSerializer.Deserialize<JsonElement>(json);
        var aiMissing = ReadStringArray(payload, "missingFields");
        var aiContradictions = ReadStringArray(payload, "contradictions");
        var explicitFalse = payload.TryGetProperty("valid", out var validElement)
            && validElement.ValueKind == JsonValueKind.False;
        return new AiStrategyEvaluation
        {
            Valid = !explicitFalse && aiMissing.Count == 0 && aiContradictions.Count == 0,
            MissingFields = aiMissing,
            Contradictions = aiContradictions,
        };
    }

    private static AiStrategyEvaluation InvalidJson()
    {
        return new AiStrategyEvaluation
        {
            Valid = false,
            MissingFields = ["json"],
            Contradictions = ["invalid-json"],
        };
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text[start..(end + 1)];
        }

        return text;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToArray();
    }
}
