using System.Text.Json;
using SharpNinja.AiUnit.Frontier;
using AiUnitRuntime = SharpNinja.AiUnit.Xunit.AiStrategyFixture;

namespace QBrainAi.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC3: aiUnit companion evaluator for plugin Session Log receipts.
/// Deterministic failures (invalid JSON, missing fields, contradictions, deterministicFailure=true)
/// are never overridden to Valid=true. Success-path evaluation invokes SharpNinja.aiUnit.
/// TEST-MCP-PLUGININT-001 AC-004 (PLAN-SESSIONTRIAGEFIX-001 E6 follow-up): the instructions state the receipt contract, and the
/// evaluation waits for the active strategy's TimeoutSeconds plus <see cref="EvaluationMarginSeconds"/>, so aiUnit reports its own
/// timeout first; no model is named (operator rule 2026-10-15).
/// </summary>
public static class AiStrategyFixture
{
    /// <summary>Receipt fields the deterministic check and the evaluator's contract both require as non-empty strings.</summary>
    internal static readonly string[] RequiredFields = ["agent", "cacheFolder", "entrypoint", "status"];

    /// <summary>Seconds added to the strategy's TimeoutSeconds before the fixture stops waiting for aiUnit's answer.</summary>
    internal const int EvaluationMarginSeconds = 30;

    /// <summary>The evaluator's instructions: the receipt contract and the answer it returns.</summary>
    internal const string SystemPrompt =
        "You evaluate one plugin Session Log persistence receipt, given as JSON after these instructions. " +
        "The receipt contract: agent, cacheFolder, entrypoint and status are required non-empty strings, and status must be completed; " +
        "sessionId, requestId and sourceSha identify the persisted turn and must be non-empty strings when present; " +
        "deterministicFailure is optional and false when absent. " +
        "Return only JSON with keys valid (boolean), missingFields (string array naming only required fields that are absent or empty), " +
        "and contradictions (string array naming fields whose values contradict the contract). " +
        "Never set valid=true when deterministicFailure is true, a required field is missing, or the JSON is malformed.";

    /// <summary>TEST-MCP-PLUGININT-001 AC-004: how long the fixture waits for an answer under a strategy timeout of <paramref name="strategyTimeoutSeconds"/>.</summary>
    /// <param name="strategyTimeoutSeconds">The active strategy's TimeoutSeconds.</param>
    /// <returns>The strategy timeout plus <see cref="EvaluationMarginSeconds"/>.</returns>
    internal static TimeSpan EvaluationWait(int strategyTimeoutSeconds) => TimeSpan.FromSeconds(strategyTimeoutSeconds + EvaluationMarginSeconds);

    /// <summary>
    /// TEST-MCP-PLUGININT-001 AC-004: the active strategy's TimeoutSeconds from aiUnit's resolved strategy, which aiUnit 2.1.3 exposes as
    /// object (its own reader, ExtractStrategyTimeoutSeconds, is private and reads the property the same way); null when it has none.
    /// </summary>
    /// <param name="resolved">AiStrategyFixture.Default.Resolved.</param>
    /// <returns>The strategy's TimeoutSeconds, or null.</returns>
    internal static int? StrategyTimeoutSeconds(object? resolved) =>
        resolved?.GetType().GetProperty("TimeoutSeconds")?.GetValue(resolved) is int seconds ? seconds : null;

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
        var strategyTimeoutSeconds = StrategyTimeoutSeconds(AiUnitRuntime.Default.Resolved);
        if (client is null || strategyTimeoutSeconds is null)
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
                .WaitAsync(EvaluationWait(strategyTimeoutSeconds.Value), cancellationToken)
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
