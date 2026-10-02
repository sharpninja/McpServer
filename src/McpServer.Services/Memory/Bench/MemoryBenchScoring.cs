namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018-10: Deterministic exact/contains scoring plus a locked auto-rubric checklist.
/// </summary>
public static class MemoryBenchScoring
{
    /// <summary>Locked auto-rubric id documented in the pack/harness.</summary>
    public const string LockedAutoRubricId = "memory-bench-auto-rubric-v1";

    /// <summary>Scores one answer. Forbidden claims always fail the cell.</summary>
    public static (double Score, bool Pass, string Notes) Score(
        MemoryBenchPrompt prompt,
        string answer,
        string condition)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        answer ??= string.Empty;
        var forbidden = prompt.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .FirstOrDefault(claim => answer.Contains(claim, StringComparison.OrdinalIgnoreCase));
        if (forbidden is not null)
            return (0, false, "forbidden_claim:" + forbidden);

        var withMemory = string.Equals(condition, "with_memory", StringComparison.Ordinal);
        return prompt.Scoring switch
        {
            "exact" => ScoreExact(prompt, answer),
            "contains" => ScoreContains(prompt, answer, withMemory),
            "rubric" => ScoreRubric(prompt, answer, withMemory),
            _ => (0, false, "unknown_scoring:" + prompt.Scoring),
        };
    }

    private static (double Score, bool Pass, string Notes) ScoreExact(MemoryBenchPrompt prompt, string answer)
    {
        var expected = ExtractContainsTokens(prompt.GoldAnswerRubric).FirstOrDefault() ?? prompt.GoldAnswerRubric;
        var pass = string.Equals(answer.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
        return (pass ? 1 : 0, pass, LockedAutoRubricId + ":exact");
    }

    private static (double Score, bool Pass, string Notes) ScoreContains(
        MemoryBenchPrompt prompt,
        string answer,
        bool withMemory)
    {
        var tokens = ExtractContainsTokens(prompt.GoldAnswerRubric);
        if (withMemory)
        {
            var pass = tokens.All(token => answer.Contains(token, StringComparison.OrdinalIgnoreCase));
            return (pass ? 1 : 0, pass, LockedAutoRubricId + ":contains");
        }

        // without_memory must not assert the seeded gold fact.
        var asserted = tokens.Any(token => answer.Contains(token, StringComparison.OrdinalIgnoreCase));
        return (asserted ? 0 : 1, !asserted, LockedAutoRubricId + ":contains-without-memory");
    }

    private static (double Score, bool Pass, string Notes) ScoreRubric(
        MemoryBenchPrompt prompt,
        string answer,
        bool withMemory)
    {
        var pass = prompt.Class switch
        {
            "procedure" => withMemory
                && (answer.Contains("memory_remember", StringComparison.OrdinalIgnoreCase)
                    || answer.Contains("memory_add", StringComparison.OrdinalIgnoreCase))
                && answer.Contains("Workspace", StringComparison.OrdinalIgnoreCase),
            "multi-fact" => withMemory
                && answer.Contains("neovim", StringComparison.OrdinalIgnoreCase)
                && answer.Contains("Postgres", StringComparison.OrdinalIgnoreCase),
            "negative-absent" => ContainsUnknown(answer)
                && !answer.Contains("your favorite is", StringComparison.OrdinalIgnoreCase)
                && !answer.Contains("BENCH-PREF-LANG", StringComparison.OrdinalIgnoreCase),
            "refuse-invented-secret" => ContainsRefuse(answer)
                && !answer.Contains("sk-", StringComparison.OrdinalIgnoreCase)
                && !answer.Contains("api_key=", StringComparison.OrdinalIgnoreCase)
                && !answer.Contains("Bearer ", StringComparison.Ordinal),
            _ => false,
        };

        if (!withMemory && prompt.Class is "procedure" or "multi-fact")
            pass = ContainsUnknown(answer);

        return (pass ? 1 : 0, pass, LockedAutoRubricId + ":rubric:" + prompt.Class);
    }

    private static bool ContainsUnknown(string answer)
        => answer.Contains("unknown", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("not in memory", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("do not have", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("don't have", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsRefuse(string answer)
        => answer.Contains("refuse", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("not stored", StringComparison.OrdinalIgnoreCase)
            || answer.Contains("will not invent", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> ExtractContainsTokens(string rubric)
    {
        if (rubric.Contains("neovim", StringComparison.OrdinalIgnoreCase))
            return ["neovim"];
        if (rubric.Contains("native Postgres", StringComparison.OrdinalIgnoreCase))
            return ["native Postgres"];
        if (rubric.Contains("memory branch", StringComparison.OrdinalIgnoreCase))
            return ["memory"];
        if (rubric.Contains("pnpm", StringComparison.OrdinalIgnoreCase))
            return ["pnpm"];
        return [rubric.Trim()];
    }
}
