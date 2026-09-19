using System.Diagnostics;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace MemoryBenchLiveSubscription;

/// <summary>
/// Live Grok adapter for subscription/cloud-agent context. Uses answers produced by
/// the current Grok subject. Does not read XAI_API_KEY and does not use stub fixtures.
/// </summary>
public sealed class MemoryBenchSubscriptionGrokAdapter : IMemoryBenchPluginAdapter
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _answers;

    /// <summary>Creates an adapter from a prompt-id to condition-to-answer map.</summary>
    public MemoryBenchSubscriptionGrokAdapter(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> answers)
    {
        _answers = answers ?? throw new ArgumentNullException(nameof(answers));
    }

    /// <inheritdoc />
    public string PluginId => "grok";

    /// <inheritdoc />
    public string ValidationEntrypoint => "subscription-live";

    /// <summary>Loads live answers JSON written by the subscription Grok subject.</summary>
    public static MemoryBenchSubscriptionGrokAdapter LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var answersNode = document.RootElement.GetProperty("answers");
        var answers = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var prompt in answersNode.EnumerateObject())
        {
            var conditions = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var condition in prompt.Value.EnumerateObject())
                conditions[condition.Name] = condition.Value.GetString() ?? string.Empty;
            answers[prompt.Name] = conditions;
        }

        return new MemoryBenchSubscriptionGrokAdapter(answers);
    }

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(context.Plugin, PluginId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscription live adapter only serves the grok lane.");

        var started = Stopwatch.StartNew();
        if (!_answers.TryGetValue(context.Prompt.Id, out var conditions)
            || !conditions.TryGetValue(context.Condition, out var answer)
            || string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "Missing live subscription answer for " + context.Prompt.Id + "/" + context.Condition);
        }

        // Guard: never silently fall back to MemoryBenchGrokAdapter fixture answers.
        var fixture = MemoryBenchGrokAdapter.Answer(context.Prompt.Id, context.Condition == "with_memory");
        if (string.Equals(answer.Trim(), fixture.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Live subscription answer must not be a MemoryBenchGrokAdapter fixture clone: "
                + context.Prompt.Id + "/" + context.Condition);
        }

        var toolCalls = new List<string>();
        var toolPayload = string.Empty;
        if (context.Condition == "with_memory" && context.ToolsAvailable)
        {
            toolCalls.Add("memory_recall");
            toolPayload = "memory_recall query=" + context.Prompt.UserText + " results="
                + string.Join(';', context.EffectiveMemories.Select(memory => memory.Content));
        }

        var transcript = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(context.Injection))
        {
            transcript.AppendLine(context.Injection.TrimEnd());
            transcript.AppendLine();
        }

        transcript.AppendLine("user: " + context.Prompt.UserText);
        if (toolCalls.Count > 0)
            transcript.AppendLine("tool: memory_recall");
        transcript.AppendLine("assistant: " + answer);
        started.Stop();

        return new MemoryBenchTurnResult
        {
            Answer = answer,
            Transcript = transcript.ToString(),
            ToolCalls = toolCalls,
            ToolPayloadText = toolPayload,
            LatencyMs = (int)Math.Max(1, started.ElapsedMilliseconds),
            TokenSourceHint = MemoryBenchTokenSources.Estimator,
        };
    }
}
