using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-019: Grok multi-turn adapter. Stub/recorded fixtures are fail-closed.
/// Live mode still requires XAI_API_KEY on this adapter; subscription live uses a sibling adapter.
/// </summary>
public sealed class MemoryBenchGrokMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "grok";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(context.Plugin, PluginId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MemoryBenchGrokMultiTurnAdapter only serves the grok lane.");

        if (string.Equals(context.Mode, MemoryBenchModes.Live, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("XAI_API_KEY")))
        {
            throw new InvalidOperationException(
                "Live Grok mode requires XAI_API_KEY; CI must use stub or recorded fixtures.");
        }

        var withMemory = string.Equals(context.Condition, "with_memory", StringComparison.Ordinal);
        var answer = Answer(context.Job.Id, context.Turn.Id, withMemory);
        var toolCalls = new List<string>();
        var toolPayload = string.Empty;
        if (withMemory && context.ToolsAvailable && context.Turn.Role == "establish" && context.Turn.WriteMemories)
        {
            toolCalls.Add("memory_remember");
            toolPayload = "memory_remember scope=Workspace content="
                + string.Join(';', context.Turn.ExpectedMemories.Select(memory => memory.Content));
        }
        else if (withMemory && context.ToolsAvailable && context.Turn.Role == "query" && context.EffectiveMemories.Count > 0)
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

        return new MemoryBenchTurnResult
        {
            Answer = answer,
            Transcript = transcript.ToString(),
            ToolCalls = toolCalls,
            ToolPayloadText = toolPayload,
            LatencyMs = 1,
            TokenSourceHint = string.Equals(context.Mode, MemoryBenchModes.Recorded, StringComparison.Ordinal)
                ? MemoryBenchTokenSources.Recorded
                : MemoryBenchTokenSources.Estimator,
        };
    }

    /// <summary>
    /// Recorded Grok answers for the v2 pack. Both conditions answer gold on query turns
    /// because without_memory is allowed to use conversation history.
    /// </summary>
    public static string Answer(string jobId, string turnId, bool withMemory)
    {
        return (jobId, turnId, withMemory) switch
        {
            ("JOB-PREF-001", "EST-001", true) =>
                "Remembered with memory_remember (Workspace): neovim is the editor preference.",
            ("JOB-PREF-001", "EST-001", false) =>
                "Noted in this conversation only: neovim is the editor preference. No memory store is available.",
            ("JOB-PREF-001", "QRY-001", true) =>
                "You prefer neovim for this workspace.",
            ("JOB-PREF-001", "QRY-001", false) =>
                "From earlier in this conversation, you prefer neovim for this workspace.",

            ("JOB-DEC-001", "EST-001", true) =>
                "Remembered with memory_remember (Workspace): native Postgres, not Docker, for local McpServer.",
            ("JOB-DEC-001", "EST-001", false) =>
                "Noted in this conversation only: native Postgres, not Docker, for local McpServer.",
            ("JOB-DEC-001", "QRY-001", true) =>
                "We decided to use native Postgres, not Docker, for local McpServer.",
            ("JOB-DEC-001", "QRY-001", false) =>
                "From earlier in this conversation, we decided on native Postgres, not Docker.",

            ("JOB-FACT-001", "EST-001", true) =>
                "Remembered with memory_remember (Workspace): the work lives on the memory branch.",
            ("JOB-FACT-001", "EST-001", false) =>
                "Noted in this conversation only: the work lives on the memory branch.",
            ("JOB-FACT-001", "QRY-001", true) =>
                "The memory branch carries the agent memory work.",
            ("JOB-FACT-001", "QRY-001", false) =>
                "From earlier in this conversation, the memory branch carries that work.",

            ("JOB-MULTI-001", "EST-001", true) =>
                "Remembered with memory_remember (Workspace): neovim preference.",
            ("JOB-MULTI-001", "EST-001", false) =>
                "Noted in this conversation only: neovim preference.",
            ("JOB-MULTI-001", "EST-002", true) =>
                "Remembered with memory_remember (Workspace): native Postgres decision.",
            ("JOB-MULTI-001", "EST-002", false) =>
                "Noted in this conversation only: native Postgres decision.",
            ("JOB-MULTI-001", "EST-003", true) =>
                "Remembered with memory_remember (Workspace): memory branch fact.",
            ("JOB-MULTI-001", "EST-003", false) =>
                "Noted in this conversation only: memory branch fact.",
            ("JOB-MULTI-001", "QRY-001", true) =>
                "You prefer neovim, we decided on native Postgres, and the memory branch carries the work.",
            ("JOB-MULTI-001", "QRY-001", false) =>
                "From earlier turns: neovim, native Postgres, and the memory branch.",

            ("JOB-NEG-001", "EST-001", true) =>
                "Remembered with memory_remember (Workspace): neovim preference and the no-secrets policy.",
            ("JOB-NEG-001", "EST-001", false) =>
                "Noted in this conversation only: neovim preference and that no language or key was given.",
            ("JOB-NEG-001", "QRY-001", _) =>
                "Unknown / not in memory. I will not invent a favorite unused language.",
            ("JOB-NEG-001", "QRY-002", _) =>
                "I refuse to invent a production API key. That secret is not stored.",

            _ => throw new InvalidOperationException(
                "No recorded Grok multi-turn fixture for " + jobId + "/" + turnId),
        };
    }
}
