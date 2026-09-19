using System.Diagnostics;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace MemoryBenchLiveSubscription;

/// <summary>
/// Live Grok multi-turn adapter for subscription/cloud-agent context.
/// Uses answers produced by the current Grok subject. Does not read XAI_API_KEY.
/// </summary>
public sealed class MemoryBenchSubscriptionGrokMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> _answers;

    /// <summary>Creates an adapter from job → turn → condition → answer maps.</summary>
    public MemoryBenchSubscriptionGrokMultiTurnAdapter(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> answers)
    {
        _answers = answers ?? throw new ArgumentNullException(nameof(answers));
    }

    /// <inheritdoc />
    public string PluginId => "grok";

    /// <inheritdoc />
    public string ValidationEntrypoint => "subscription-live";

    /// <summary>Loads live multi-turn answers JSON written by the subscription Grok subject.</summary>
    public static MemoryBenchSubscriptionGrokMultiTurnAdapter LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var answersNode = document.RootElement.GetProperty("answers");
        var answers = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(StringComparer.Ordinal);
        foreach (var job in answersNode.EnumerateObject())
        {
            var turns = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            foreach (var turn in job.Value.EnumerateObject())
            {
                var conditions = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var condition in turn.Value.EnumerateObject())
                    conditions[condition.Name] = condition.Value.GetString() ?? string.Empty;
                turns[turn.Name] = conditions;
            }

            answers[job.Name] = turns;
        }

        return new MemoryBenchSubscriptionGrokMultiTurnAdapter(answers);
    }

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(context.Plugin, PluginId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscription live multi-turn adapter only serves the grok lane.");

        var started = Stopwatch.StartNew();
        if (!_answers.TryGetValue(context.Job.Id, out var turns)
            || !turns.TryGetValue(context.Turn.Id, out var conditions)
            || !conditions.TryGetValue(context.Condition, out var answer)
            || string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "Missing live subscription answer for " + context.Job.Id + "/" + context.Turn.Id + "/" + context.Condition);
        }

        var fixture = MemoryBenchGrokMultiTurnAdapter.Answer(
            context.Job.Id,
            context.Turn.Id,
            context.Condition == "with_memory");
        if (string.Equals(answer.Trim(), fixture.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Live subscription answer must not be a MemoryBenchGrokMultiTurnAdapter fixture clone: "
                + context.Job.Id + "/" + context.Turn.Id + "/" + context.Condition);
        }

        var toolCalls = new List<string>();
        var toolPayload = string.Empty;
        if (context.Condition == "with_memory" && context.ToolsAvailable && context.Turn.Role == "establish" && context.Turn.WriteMemories)
        {
            toolCalls.Add("memory_remember");
            toolPayload = "memory_remember scope=Workspace content="
                + string.Join(';', context.Turn.ExpectedMemories.Select(memory => memory.Content));
        }
        else if (context.Condition == "with_memory" && context.ToolsAvailable && context.Turn.Role == "query" && context.EffectiveMemories.Count > 0)
        {
            toolCalls.Add("memory_recall");
            toolPayload = "memory_recall query=" + context.Turn.UserText + " results="
                + string.Join(';', context.EffectiveMemories.Select(memory => memory.Content));
        }

        var transcript = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(context.Injection))
        {
            transcript.AppendLine(context.Injection.TrimEnd());
            transcript.AppendLine();
        }

        transcript.AppendLine("user: " + context.Turn.UserText);
        foreach (var tool in toolCalls)
            transcript.AppendLine("tool: " + tool);
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
