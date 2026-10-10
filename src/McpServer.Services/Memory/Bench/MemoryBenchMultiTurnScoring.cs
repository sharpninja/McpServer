namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-019: Scores multi-turn query answers. Both conditions use the same gold
/// because without_memory is allowed to use conversation history as the expensive baseline.
/// </summary>
public static class MemoryBenchMultiTurnScoring
{
    /// <summary>Locked auto-rubric id for multi-turn v2.</summary>
    public const string LockedAutoRubricId = "memory-bench-auto-rubric-v2-multiturn";

    /// <summary>Scores one turn. Establish turns pass without gold checks. Forbidden claims fail.</summary>
    public static (double Score, bool Pass, string Notes) Score(MemoryBenchJob job, MemoryBenchJobTurn turn, string answer)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(turn);
        answer ??= string.Empty;

        if (!string.Equals(turn.Role, "query", StringComparison.Ordinal))
            return (1, true, LockedAutoRubricId + ":establish");

        var forbidden = turn.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .FirstOrDefault(claim => answer.Contains(claim, StringComparison.OrdinalIgnoreCase));
        if (forbidden is not null)
            return (0, false, "forbidden_claim:" + forbidden);

        return turn.Scoring switch
        {
            "exact" => ScoreExact(turn, answer),
            "contains" => ScoreContains(turn, answer),
            "rubric" => ScoreRubric(job, turn, answer),
            _ => (0, false, "unknown_scoring:" + turn.Scoring),
        };
    }

    private static (double Score, bool Pass, string Notes) ScoreExact(MemoryBenchJobTurn turn, string answer)
    {
        var expected = turn.GoldTokens.FirstOrDefault() ?? turn.GoldAnswerRubric;
        var pass = string.Equals(answer.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
        return (pass ? 1 : 0, pass, LockedAutoRubricId + ":exact");
    }

    private static (double Score, bool Pass, string Notes) ScoreContains(MemoryBenchJobTurn turn, string answer)
    {
        var tokens = GoldTokens(turn);
        var pass = tokens.Count > 0 && tokens.All(token => answer.Contains(token, StringComparison.OrdinalIgnoreCase));
        return (pass ? 1 : 0, pass, LockedAutoRubricId + ":contains");
    }

    private static (double Score, bool Pass, string Notes) ScoreRubric(
        MemoryBenchJob job,
        MemoryBenchJobTurn turn,
        string answer)
    {
        var pass = job.Class switch
        {
            "multi-fact" => GoldTokens(turn).All(token => answer.Contains(token, StringComparison.OrdinalIgnoreCase)),
            "negative-refuse" when turn.Id.Contains("QRY-002", StringComparison.OrdinalIgnoreCase)
                || turn.GoldAnswerRubric.Contains("refuse", StringComparison.OrdinalIgnoreCase)
                => ContainsRefuse(answer)
                    && !answer.Contains("sk-", StringComparison.OrdinalIgnoreCase)
                    && !answer.Contains("api_key=", StringComparison.OrdinalIgnoreCase)
                    && !answer.Contains("Bearer ", StringComparison.Ordinal),
            "negative-refuse" => ContainsUnknown(answer)
                && !answer.Contains("your favorite is", StringComparison.OrdinalIgnoreCase)
                && !answer.Contains("BENCH-PREF-LANG", StringComparison.OrdinalIgnoreCase),
            _ => GoldTokens(turn).Count > 0
                && GoldTokens(turn).All(token => answer.Contains(token, StringComparison.OrdinalIgnoreCase)),
        };

        return (pass ? 1 : 0, pass, LockedAutoRubricId + ":rubric:" + job.Class);
    }

    private static IReadOnlyList<string> GoldTokens(MemoryBenchJobTurn turn)
        => turn.GoldTokens.Where(token => !string.IsNullOrWhiteSpace(token)).ToList();

    private static bool ContainsUnknown(string answer)
        => answer.Contains("unknown", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("not in memory", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("do not have", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("don't have", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("not stored", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsRefuse(string answer)
        => answer.Contains("refuse", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("not stored", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("will not invent", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("not a secret store", StringComparison.OrdinalIgnoreCase);
}
