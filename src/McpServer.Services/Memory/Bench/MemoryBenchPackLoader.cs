using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018-01 / TR-MCP-MEMORY-BENCH-002-04: Loads and validates the prompt pack.
/// </summary>
public static class MemoryBenchPackLoader
{
    /// <summary>Canonical pack path relative to the repository root.</summary>
    public const string CanonicalRelativePath = "docs/benchmarks/memory-prompt-pack-v1.yaml";

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
        "fact-paraphrase",
        "procedure",
        "multi-fact",
        "negative-absent",
        "conflict-stale-vs-new",
        "refuse-invented-secret",
    ];

    private static readonly Regex RealSecretPattern = new(
        @"sk-[A-Za-z0-9]{8,}|api_key\s*=\s*[A-Za-z0-9_\-]{12,}|Bearer\s+[A-Za-z0-9\._\-]{12,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Loads the canonical pack from a repository root.</summary>
    public static MemoryBenchPackDocument LoadFromRepo(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        return Load(Path.Combine(repositoryRoot, CanonicalRelativePath));
    }

    /// <summary>Loads and schema-validates a pack file (JSON document with optional # comments).</summary>
    public static MemoryBenchPackDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("Memory prompt pack is missing.", path);

        var raw = File.ReadAllText(path);
        var json = StripHashComments(raw);
        var pack = JsonSerializer.Deserialize<MemoryBenchPackDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Memory prompt pack deserialized to null.");
        Validate(pack, raw);
        return pack;
    }

    /// <summary>Validates pack schema and fixture safety.</summary>
    public static void Validate(MemoryBenchPackDocument pack, string? rawText = null)
    {
        ArgumentNullException.ThrowIfNull(pack);
        if (string.IsNullOrWhiteSpace(pack.Id) || string.IsNullOrWhiteSpace(pack.Version))
            throw new InvalidOperationException("Pack schema requires id and version.");
        if (pack.Prompts.Count < 8)
            throw new InvalidOperationException("Pack must define at least 8 prompts.");
        if (!pack.Conditions.Contains("without_memory", StringComparer.Ordinal)
            || !pack.Conditions.Contains("with_memory", StringComparer.Ordinal))
            throw new InvalidOperationException("Pack must declare paired without_memory and with_memory conditions.");

        var classes = pack.Prompts.Select(prompt => prompt.Class).ToHashSet(StringComparer.Ordinal);
        foreach (var required in RequiredClasses)
        {
            if (!classes.Contains(required))
                throw new InvalidOperationException("Pack is missing required class: " + required);
        }

        foreach (var prompt in pack.Prompts)
            ValidatePrompt(prompt);

        if (pack.TokenFields.Count == 0
            || !pack.TokenFields.Contains("tokens_in", StringComparer.Ordinal)
            || !pack.TokenFields.Contains("tokens_out", StringComparer.Ordinal)
            || !pack.TokenFields.Contains("tokens_total", StringComparer.Ordinal)
            || !pack.TokenFields.Contains("token_source", StringComparer.Ordinal))
            throw new InvalidOperationException("Pack must declare tokens_in/tokens_out/tokens_total/token_source.");

        if (!string.Equals(pack.PilotPlugin, "grok", StringComparison.Ordinal))
            throw new InvalidOperationException("Pack pilot_plugin must be grok.");

        var haystack = rawText ?? JsonSerializer.Serialize(pack);
        if (RealSecretPattern.IsMatch(haystack) && !haystack.Contains("BENCH-", StringComparison.Ordinal))
            throw new InvalidOperationException("Pack fixtures must not contain real secrets.");
        if (RealSecretPattern.IsMatch(haystack)
            && haystack.Contains("sk-live-", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Pack fixtures must not contain live secret material.");
    }

    private static void ValidatePrompt(MemoryBenchPrompt prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt.Id)
            || string.IsNullOrWhiteSpace(prompt.Class)
            || string.IsNullOrWhiteSpace(prompt.UserText)
            || string.IsNullOrWhiteSpace(prompt.GoldAnswerRubric)
            || string.IsNullOrWhiteSpace(prompt.Scoring))
            throw new InvalidOperationException("Prompt entry schema is incomplete: " + prompt.Id);

        if (prompt.Scoring is not ("exact" or "contains" or "rubric"))
            throw new InvalidOperationException("Prompt scoring must be exact|contains|rubric: " + prompt.Id);

        _ = prompt.SeededMemories ?? throw new InvalidOperationException("seeded_memories is required: " + prompt.Id);
        _ = prompt.ForbiddenClaims ?? throw new InvalidOperationException("forbidden_claims is required: " + prompt.Id);
    }

    private static string StripHashComments(string raw)
    {
        var lines = raw.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return string.Join('\n', lines.Where(line => !line.TrimStart().StartsWith('#')));
    }
}
