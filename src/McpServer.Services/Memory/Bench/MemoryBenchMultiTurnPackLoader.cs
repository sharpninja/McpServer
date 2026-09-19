using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-019-01 / TR-MCP-MEMORY-BENCH-003: Loads and validates the multi-turn pack.
/// </summary>
public static class MemoryBenchMultiTurnPackLoader
{
    /// <summary>Canonical v2 pack path relative to the repository root.</summary>
    public const string CanonicalRelativePath = "docs/benchmarks/memory-prompt-pack-v2-multiturn.yaml";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly string[] RequiredClasses =
    [
        "preference",
        "decision",
        "fact",
        "multi-fact",
        "negative-refuse",
    ];

    private static readonly Regex RealSecretPattern = new(
        @"sk-[A-Za-z0-9]{8,}|api_key\s*=\s*[A-Za-z0-9_\-]{12,}|Bearer\s+[A-Za-z0-9\._\-]{12,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Loads the canonical v2 pack from a repository root.</summary>
    public static MemoryBenchMultiTurnPackDocument LoadFromRepo(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        return Load(Path.Combine(repositoryRoot, CanonicalRelativePath));
    }

    /// <summary>Loads and validates a multi-turn pack file (JSON document with optional # comments).</summary>
    public static MemoryBenchMultiTurnPackDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("Memory multi-turn prompt pack is missing.", path);

        var raw = File.ReadAllText(path);
        var json = StripHashComments(raw);
        var pack = JsonSerializer.Deserialize<MemoryBenchMultiTurnPackDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Memory multi-turn prompt pack deserialized to null.");
        Validate(pack, raw);
        return pack;
    }

    /// <summary>Validates pack schema, job coverage, and query-must-not-restate rules.</summary>
    public static void Validate(MemoryBenchMultiTurnPackDocument pack, string? rawText = null)
    {
        ArgumentNullException.ThrowIfNull(pack);
        if (string.IsNullOrWhiteSpace(pack.Id) || string.IsNullOrWhiteSpace(pack.Version))
            throw new InvalidOperationException("Multi-turn pack schema requires id and version.");
        if (!string.Equals(pack.Kind, "multi_turn_jobs", StringComparison.Ordinal))
            throw new InvalidOperationException("Multi-turn pack kind must be multi_turn_jobs.");
        if (pack.Jobs.Count < 4)
            throw new InvalidOperationException("Multi-turn pack must define at least 4 jobs.");
        if (!pack.Conditions.Contains("without_memory", StringComparer.Ordinal)
            || !pack.Conditions.Contains("with_memory", StringComparer.Ordinal))
            throw new InvalidOperationException("Pack must declare paired without_memory and with_memory conditions.");

        var classes = pack.Jobs.Select(job => job.Class).ToHashSet(StringComparer.Ordinal);
        foreach (var required in RequiredClasses)
        {
            if (!classes.Contains(required))
                throw new InvalidOperationException("Multi-turn pack is missing required class: " + required);
        }

        foreach (var job in pack.Jobs)
            ValidateJob(job);

        if (pack.TokenFields.Count == 0
            || !pack.TokenFields.Contains("tokens_in", StringComparer.Ordinal)
            || !pack.TokenFields.Contains("tokens_out", StringComparer.Ordinal)
            || !pack.TokenFields.Contains("tokens_total", StringComparer.Ordinal)
            || !pack.TokenFields.Contains("token_source", StringComparer.Ordinal))
            throw new InvalidOperationException("Pack must declare tokens_in/tokens_out/tokens_total/token_source.");

        if (!string.Equals(pack.PilotPlugin, "grok", StringComparison.Ordinal))
            throw new InvalidOperationException("Pack pilot_plugin must be grok.");

        var haystack = rawText ?? JsonSerializer.Serialize(pack);
        if (RealSecretPattern.IsMatch(haystack)
            && haystack.Contains("sk-live-", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Pack fixtures must not contain live secret material.");
    }

    private static void ValidateJob(MemoryBenchJob job)
    {
        if (string.IsNullOrWhiteSpace(job.Id) || string.IsNullOrWhiteSpace(job.Class))
            throw new InvalidOperationException("Job schema is incomplete: " + job.Id);
        if (job.Turns.Count < 2)
            throw new InvalidOperationException("Job must have at least two turns: " + job.Id);
        if (!job.Turns.Any(turn => turn.Role == "establish"))
            throw new InvalidOperationException("Job is missing an establish turn: " + job.Id);
        if (!job.Turns.Any(turn => turn.Role == "query" && turn.Required))
            throw new InvalidOperationException("Job is missing a required query turn: " + job.Id);

        var sawQuery = false;
        foreach (var turn in job.Turns)
        {
            if (string.IsNullOrWhiteSpace(turn.Id)
                || string.IsNullOrWhiteSpace(turn.Role)
                || string.IsNullOrWhiteSpace(turn.UserText))
                throw new InvalidOperationException("Turn schema is incomplete: " + job.Id + "/" + turn.Id);

            if (turn.Role is not ("establish" or "query"))
                throw new InvalidOperationException("Turn role must be establish|query: " + job.Id + "/" + turn.Id);

            if (turn.Role == "query")
            {
                sawQuery = true;
                if (string.IsNullOrWhiteSpace(turn.GoldAnswerRubric) || string.IsNullOrWhiteSpace(turn.Scoring))
                    throw new InvalidOperationException("Query turn requires scoring and gold rubric: " + job.Id + "/" + turn.Id);
                if (turn.Scoring is not ("exact" or "contains" or "rubric"))
                    throw new InvalidOperationException("Turn scoring must be exact|contains|rubric: " + job.Id + "/" + turn.Id);
                foreach (var banned in turn.QueryMustNotContain.Where(token => !string.IsNullOrWhiteSpace(token)))
                {
                    if (turn.UserText.Contains(banned, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Query turn restates a fact token '" + banned + "': " + job.Id + "/" + turn.Id);
                    }
                }
            }
            else if (sawQuery)
            {
                throw new InvalidOperationException("Establish turns must precede query turns: " + job.Id);
            }
        }
    }

    private static string StripHashComments(string raw)
    {
        var lines = raw.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return string.Join('\n', lines.Where(line => !line.TrimStart().StartsWith('#')));
    }
}
