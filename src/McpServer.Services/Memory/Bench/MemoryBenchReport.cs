using System.Globalization;
using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018-11 / FR-MCP-MEMORY-018-38 / FR-MCP-MEMORY-018-40:
/// Token-first summaries. Pass rate is a gate, not the headline metric.
/// </summary>
public static class MemoryBenchReport
{
    /// <summary>Builds summary rows, deltas, headline means, and markdown.</summary>
    public static void Populate(MemoryBenchRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        result.Summary = result.Cells
            .GroupBy(cell => (cell.Plugin, cell.Condition), (key, cells) => Summarize(key.Plugin, key.Condition, cells.ToList()))
            .OrderBy(row => row.Plugin, StringComparer.Ordinal)
            .ThenBy(row => row.Condition, StringComparer.Ordinal)
            .ToList();

        result.Deltas = result.Cells
            .GroupBy(cell => (cell.Plugin, cell.PromptId))
            .Select(group =>
            {
                var withMemory = group.First(cell => cell.Condition == "with_memory").TokensTotal ?? 0;
                var withoutMemory = group.First(cell => cell.Condition == "without_memory").TokensTotal ?? 0;
                return new MemoryBenchTokenDelta
                {
                    Plugin = group.Key.Plugin,
                    PromptId = group.Key.PromptId,
                    TokensTotalDelta = withMemory - withoutMemory,
                };
            })
            .ToList();

        result.MacroMeanTokensWithMemory = Mean(result.Cells.Where(cell => cell.Condition == "with_memory").Select(cell => cell.TokensTotal ?? 0));
        result.MacroMeanTokensWithoutMemory = Mean(result.Cells.Where(cell => cell.Condition == "without_memory").Select(cell => cell.TokensTotal ?? 0));

        var passed = result.Cells.Count(cell => cell.Pass);
        result.EfficiencyTokensPerPass = passed == 0
            ? null
            : result.Cells.Sum(cell => cell.TokensTotal ?? 0) / (double)passed;

        result.SummaryMarkdown = RenderMarkdown(result);
    }

    private static MemoryBenchSummaryRow Summarize(string plugin, string condition, IReadOnlyList<MemoryBenchCellResult> cells)
    {
        var totals = cells.Select(cell => cell.TokensTotal ?? 0).OrderBy(value => value).ToArray();
        return new MemoryBenchSummaryRow
        {
            Plugin = plugin,
            Condition = condition,
            PromptsN = cells.Count,
            TokensTotalSum = totals.Sum(),
            TokensTotalMean = Mean(totals),
            TokensTotalMedian = Median(totals),
            TokensInMean = Mean(cells.Select(cell => cell.TokensIn ?? 0)),
            TokensOutMean = Mean(cells.Select(cell => cell.TokensOut ?? 0)),
            PassRate = cells.Count == 0 ? 0 : cells.Count(cell => cell.Pass) / (double)cells.Count,
        };
    }

    private static string RenderMarkdown(MemoryBenchRunResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Memory bench token summary");
        builder.AppendLine();
        builder.AppendLine("Primary metric: tokens used. Pass/fail is correctness/safety only.");
        builder.AppendLine();
        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Headline macro mean tokens_total: with_memory={result.MacroMeanTokensWithMemory:0.###} without_memory={result.MacroMeanTokensWithoutMemory:0.###}"));
        builder.AppendLine();
        builder.AppendLine("| plugin | condition | prompts_n | tokens_total_sum | tokens_total_mean | tokens_total_median | tokens_in_mean | tokens_out_mean | pass_rate |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var row in result.Summary)
        {
            builder.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {row.Plugin} | {row.Condition} | {row.PromptsN} | {row.TokensTotalSum} | {row.TokensTotalMean:0.###} | {row.TokensTotalMedian:0.###} | {row.TokensInMean:0.###} | {row.TokensOutMean:0.###} | {row.PassRate:0.###} |"));
        }

        return builder.ToString();
    }

    private static double Mean(IEnumerable<int> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : list.Average();
    }

    private static double Median(IReadOnlyList<int> sorted)
    {
        if (sorted.Count == 0)
            return 0;
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
