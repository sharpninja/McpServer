using System.Globalization;
using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-019: Token-first multi-turn report. Success-gated means are highlighted.
/// Failed jobs remain listed and are excluded from those means.
/// </summary>
public static class MemoryBenchMultiTurnReport
{
    /// <summary>Populates success-gated summary and markdown.</summary>
    public static void Populate(MemoryBenchMultiTurnRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        result.SuccessGated = MemoryBenchSuccessGatedAggregation.Compute(result.Jobs);
        result.SummaryMarkdown = RenderMarkdown(result);
    }

    private static string RenderMarkdown(MemoryBenchMultiTurnRunResult result)
    {
        var gated = result.SuccessGated;
        var builder = new StringBuilder();
        builder.AppendLine("# Memory bench multi-turn token summary");
        builder.AppendLine();
        builder.AppendLine("v2 is the real efficiency bench. v1 (`memory-prompt-pack-v1`) remains smoke/regression.");
        builder.AppendLine("Primary metric: tokens used. A job succeeds only if every required query turn passes.");
        builder.AppendLine(gated.ExclusionNote);
        builder.AppendLine();
        builder.AppendLine("## Success-gated token means (highlighted)");
        builder.AppendLine();
        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"**SUCCESS-GATED mean tokens_total:** with_memory={Format(gated.SuccessGatedMeanTokensWithMemory)} without_memory={Format(gated.SuccessGatedMeanTokensWithoutMemory)}"));
        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"**SUCCESS-GATED median tokens_total:** with_memory={Format(gated.SuccessGatedMedianTokensWithMemory)} without_memory={Format(gated.SuccessGatedMedianTokensWithoutMemory)}"));
        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"**Paired-success mean tokens_total** (jobs that passed both conditions): with_memory={Format(gated.PairedSuccessMeanTokensWithMemory)} without_memory={Format(gated.PairedSuccessMeanTokensWithoutMemory)}"));
        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"with_memory won on tokens among paired successes: {FormatBool(gated.WithMemoryWonOnPairedSuccessTokens)}"));
        builder.AppendLine();
        builder.AppendLine("## Success rates");
        builder.AppendLine();
        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"jobs_n={gated.JobsN} with_memory={gated.SuccessfulJobsWithMemory}/{gated.JobsN} ({gated.SuccessRateWithMemory:0.###}) without_memory={gated.SuccessfulJobsWithoutMemory}/{gated.JobsN} ({gated.SuccessRateWithoutMemory:0.###}) paired_successes={gated.SuccessfulJobsPaired}"));
        builder.AppendLine();
        builder.AppendLine("## Per-job tokens");
        builder.AppendLine();
        builder.AppendLine("| plugin | job_id | condition | pass | tokens_in | tokens_out | tokens_total | token_source | failed_turns |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var job in result.Jobs.OrderBy(item => item.Plugin, StringComparer.Ordinal)
                     .ThenBy(item => item.JobId, StringComparer.Ordinal)
                     .ThenBy(item => item.Condition, StringComparer.Ordinal))
        {
            builder.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {job.Plugin} | {job.JobId} | {job.Condition} | {job.Pass.ToString().ToLowerInvariant()} | {job.TokensIn} | {job.TokensOut} | {job.TokensTotal} | {job.TokenSource} | {string.Join(',', job.FailedTurnIds)} |"));
        }

        builder.AppendLine();
        builder.AppendLine("## Per-turn tokens");
        builder.AppendLine();
        builder.AppendLine("| plugin | job_id | turn_id | role | condition | required | pass | tokens_in | tokens_out | tokens_total | token_source |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var turn in result.Turns)
        {
            builder.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {turn.Plugin} | {turn.JobId} | {turn.TurnId} | {turn.Role} | {turn.Condition} | {turn.Required.ToString().ToLowerInvariant()} | {turn.Pass.ToString().ToLowerInvariant()} | {turn.TokensIn} | {turn.TokensOut} | {turn.TokensTotal} | {turn.TokenSource} |"));
        }

        return builder.ToString();
    }

    private static string Format(double? value)
        => value is null ? "n/a" : value.Value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatBool(bool? value)
        => value is null ? "n/a" : value.Value ? "yes" : "no";
}
