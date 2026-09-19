namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-019: Macro mean/median tokens_total only over successful jobs.
/// Failed jobs remain in the artifact and must not drive an efficiency claim.
/// </summary>
public static class MemoryBenchSuccessGatedAggregation
{
    /// <summary>Computes success rates and success-gated token means/medians.</summary>
    public static MemoryBenchSuccessGatedSummary Compute(IReadOnlyList<MemoryBenchJobCellResult> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var withMemory = jobs.Where(job => job.Condition == "with_memory").ToList();
        var withoutMemory = jobs.Where(job => job.Condition == "without_memory").ToList();
        var successfulWith = withMemory.Where(job => job.Pass).ToList();
        var successfulWithout = withoutMemory.Where(job => job.Pass).ToList();

        var pairedIds = successfulWith.Select(job => job.Plugin + "\u001f" + job.JobId)
            .Intersect(successfulWithout.Select(job => job.Plugin + "\u001f" + job.JobId), StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var pairedWith = successfulWith
            .Where(job => pairedIds.Contains(job.Plugin + "\u001f" + job.JobId))
            .ToList();
        var pairedWithout = successfulWithout
            .Where(job => pairedIds.Contains(job.Plugin + "\u001f" + job.JobId))
            .ToList();

        var pairedWithMean = MeanOrNull(pairedWith.Select(job => job.TokensTotal));
        var pairedWithoutMean = MeanOrNull(pairedWithout.Select(job => job.TokensTotal));
        bool? won = pairedWithMean is null || pairedWithoutMean is null
            ? null
            : pairedWithMean < pairedWithoutMean;

        return new MemoryBenchSuccessGatedSummary
        {
            JobsN = Math.Max(withMemory.Count, withoutMemory.Count),
            SuccessfulJobsWithMemory = successfulWith.Count,
            SuccessfulJobsWithoutMemory = successfulWithout.Count,
            SuccessfulJobsPaired = pairedIds.Count,
            SuccessRateWithMemory = Rate(successfulWith.Count, withMemory.Count),
            SuccessRateWithoutMemory = Rate(successfulWithout.Count, withoutMemory.Count),
            SuccessGatedMeanTokensWithMemory = MeanOrNull(successfulWith.Select(job => job.TokensTotal)),
            SuccessGatedMedianTokensWithMemory = MedianOrNull(successfulWith.Select(job => job.TokensTotal)),
            SuccessGatedMeanTokensWithoutMemory = MeanOrNull(successfulWithout.Select(job => job.TokensTotal)),
            SuccessGatedMedianTokensWithoutMemory = MedianOrNull(successfulWithout.Select(job => job.TokensTotal)),
            PairedSuccessMeanTokensWithMemory = pairedWithMean,
            PairedSuccessMeanTokensWithoutMemory = pairedWithoutMean,
            WithMemoryWonOnPairedSuccessTokens = won,
        };
    }

    private static double Rate(int passed, int total)
        => total == 0 ? 0 : passed / (double)total;

    private static double? MeanOrNull(IEnumerable<int> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : list.Average();
    }

    private static double? MedianOrNull(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        if (sorted.Length == 0)
            return null;
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
