using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018 / TR-MCP-MEMORY-BENCH-002: Allowed token accounting sources.
/// </summary>
public static class MemoryBenchTokenSources
{
    /// <summary>Plugin host reported usage.</summary>
    public const string Host = "host";

    /// <summary>Documented estimator fallback.</summary>
    public const string Estimator = "estimator";

    /// <summary>Recorded fixture usage.</summary>
    public const string Recorded = "recorded";

    /// <summary>Closed set required by the result schema.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Host,
        Estimator,
        Recorded,
    };
}

/// <summary>
/// FR-MCP-MEMORY-018: Allowed bench run modes.
/// </summary>
public static class MemoryBenchModes
{
    /// <summary>Deterministic stub agent; no cloud required.</summary>
    public const string Stub = "stub";

    /// <summary>Replay of recorded fixtures.</summary>
    public const string Recorded = "recorded";

    /// <summary>Optional live host; opt-in only.</summary>
    public const string Live = "live";
}

/// <summary>
/// FR-MCP-MEMORY-018-03: One seeded memory row from the prompt pack.
/// </summary>
public sealed class MemoryBenchSeededMemory
{
    /// <summary>Seed title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Seed content. Synthetic BENCH-* tokens only.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Memory type.</summary>
    public string Type { get; set; } = "fact";

    /// <summary>Optional tags.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Optional confidence.</summary>
    public double Confidence { get; set; } = 1.0;
}

/// <summary>
/// FR-MCP-MEMORY-018-03: One pack prompt.
/// </summary>
public sealed class MemoryBenchPrompt
{
    /// <summary>Stable prompt id such as PREF-001.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Prompt class token.</summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>User text used for both paired conditions.</summary>
    [JsonPropertyName("user_text")]
    public string UserText { get; set; } = string.Empty;

    /// <summary>Gold rubric text.</summary>
    [JsonPropertyName("gold_answer_rubric")]
    public string GoldAnswerRubric { get; set; } = string.Empty;

    /// <summary>Scoring mode: exact, contains, or rubric.</summary>
    public string Scoring { get; set; } = "contains";

    /// <summary>Claims the answer must not make.</summary>
    [JsonPropertyName("forbidden_claims")]
    public List<string> ForbiddenClaims { get; set; } = [];

    /// <summary>Workspace-scoped seeds for with_memory.</summary>
    [JsonPropertyName("seeded_memories")]
    public List<MemoryBenchSeededMemory> SeededMemories { get; set; } = [];

    /// <summary>Optional latency budget; unused as sole pass/fail when absent.</summary>
    [JsonPropertyName("latency_budget_ms")]
    public int? LatencyBudgetMs { get; set; }
}

/// <summary>
/// FR-MCP-MEMORY-018-38: Metric lists declared by the pack.
/// </summary>
public sealed class MemoryBenchMetricsSpec
{
    /// <summary>Primary metrics. Must lead with tokens.</summary>
    public List<string> Primary { get; set; } = [];

    /// <summary>Secondary metrics including pass_rate and latency.</summary>
    public List<string> Secondary { get; set; } = [];

    /// <summary>Allowed token_source values.</summary>
    [JsonPropertyName("token_source_enum")]
    public List<string> TokenSourceEnum { get; set; } = [];
}

/// <summary>
/// FR-MCP-MEMORY-018-07: Plugin rollout policy declared by the pack.
/// </summary>
public sealed class MemoryBenchPluginRollout
{
    /// <summary>Plugins required before H7a.</summary>
    [JsonPropertyName("required_first")]
    public List<string> RequiredFirst { get; set; } = [];

    /// <summary>Plugins deferred until H7a AGREE.</summary>
    [JsonPropertyName("after_h7a_agree")]
    public List<string> AfterH7aAgree { get; set; } = [];
}

/// <summary>
/// FR-MCP-MEMORY-018-01: Versioned prompt pack document.
/// </summary>
public sealed class MemoryBenchPackDocument
{
    /// <summary>Pack semver.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Pack id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Known plugin ids.</summary>
    public List<string> Plugins { get; set; } = [];

    /// <summary>Paired conditions.</summary>
    public List<string> Conditions { get; set; } = [];

    /// <summary>Prompt entries.</summary>
    public List<MemoryBenchPrompt> Prompts { get; set; } = [];

    /// <summary>Primary metric name.</summary>
    [JsonPropertyName("primary_metric")]
    public string PrimaryMetric { get; set; } = "tokens_total";

    /// <summary>Required per-cell token fields.</summary>
    [JsonPropertyName("token_fields")]
    public List<string> TokenFields { get; set; } = [];

    /// <summary>Metric classification.</summary>
    public MemoryBenchMetricsSpec Metrics { get; set; } = new();

    /// <summary>Pilot plugin id.</summary>
    [JsonPropertyName("pilot_plugin")]
    public string PilotPlugin { get; set; } = "grok";

    /// <summary>Rollout policy.</summary>
    [JsonPropertyName("plugin_rollout")]
    public MemoryBenchPluginRollout PluginRollout { get; set; } = new();
}

/// <summary>
/// FR-MCP-MEMORY-018-08: One plugin × prompt × condition cell.
/// </summary>
public sealed class MemoryBenchCellResult
{
    /// <summary>Plugin id.</summary>
    public string Plugin { get; set; } = string.Empty;

    /// <summary>Prompt id.</summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>without_memory or with_memory.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Full transcript including injection when present.</summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>Tool names invoked during the turn.</summary>
    public List<string> ToolCalls { get; set; } = [];

    /// <summary>True when REQUIRED MEMORIES was injected.</summary>
    public bool InjectionPresent { get; set; }

    /// <summary>Prompt/system/injection/tool-result tokens.</summary>
    public int? TokensIn { get; set; }

    /// <summary>Completion tokens.</summary>
    public int? TokensOut { get; set; }

    /// <summary>tokens_in + tokens_out.</summary>
    public int? TokensTotal { get; set; }

    /// <summary>host, estimator, or recorded.</summary>
    public string? TokenSource { get; set; }

    /// <summary>Estimator id when token_source is estimator.</summary>
    public string? EstimatorId { get; set; }

    /// <summary>Estimator version when token_source is estimator.</summary>
    public string? EstimatorVersion { get; set; }

    /// <summary>Secondary latency metric.</summary>
    public int LatencyMs { get; set; }

    /// <summary>Numeric score in [0,1].</summary>
    public double Score { get; set; }

    /// <summary>Correctness/safety gate only.</summary>
    public bool Pass { get; set; }

    /// <summary>Optional notes.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Injection token count charged to tokens_in.</summary>
    public int InjectionTokens { get; set; }

    /// <summary>Tool payload/result token count charged to tokens_in.</summary>
    public int ToolTokens { get; set; }

    /// <summary>Assistant answer text used for scoring.</summary>
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// FR-MCP-MEMORY-018-38: One plugin × condition summary row.
/// </summary>
public sealed class MemoryBenchSummaryRow
{
    /// <summary>Plugin id.</summary>
    public string Plugin { get; set; } = string.Empty;

    /// <summary>Condition.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Prompt count.</summary>
    public int PromptsN { get; set; }

    /// <summary>Sum of tokens_total.</summary>
    public int TokensTotalSum { get; set; }

    /// <summary>Mean tokens_total.</summary>
    public double TokensTotalMean { get; set; }

    /// <summary>Median tokens_total.</summary>
    public double TokensTotalMedian { get; set; }

    /// <summary>Mean tokens_in.</summary>
    public double TokensInMean { get; set; }

    /// <summary>Mean tokens_out.</summary>
    public double TokensOutMean { get; set; }

    /// <summary>Correctness/safety pass rate.</summary>
    public double PassRate { get; set; }
}

/// <summary>
/// FR-MCP-MEMORY-018-39: Per-prompt token delta for one plugin.
/// </summary>
public sealed class MemoryBenchTokenDelta
{
    /// <summary>Plugin id.</summary>
    public string Plugin { get; set; } = string.Empty;

    /// <summary>Prompt id.</summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>tokens_total(with_memory) - tokens_total(without_memory).</summary>
    public int TokensTotalDelta { get; set; }
}

/// <summary>
/// FR-MCP-MEMORY-018-09: Completed bench run artifact.
/// </summary>
public sealed class MemoryBenchRunResult
{
    /// <summary>Pack id.</summary>
    public string PackId { get; set; } = string.Empty;

    /// <summary>stub, recorded, or live.</summary>
    public string Mode { get; set; } = MemoryBenchModes.Stub;

    /// <summary>Plugins actually executed.</summary>
    public List<string> Plugins { get; set; } = [];

    /// <summary>Per-cell results.</summary>
    public List<MemoryBenchCellResult> Cells { get; set; } = [];

    /// <summary>Token-first summary rows.</summary>
    public List<MemoryBenchSummaryRow> Summary { get; set; } = [];

    /// <summary>Per-prompt token deltas.</summary>
    public List<MemoryBenchTokenDelta> Deltas { get; set; } = [];

    /// <summary>Headline mean tokens_total for with_memory.</summary>
    public double MacroMeanTokensWithMemory { get; set; }

    /// <summary>Headline mean tokens_total for without_memory.</summary>
    public double MacroMeanTokensWithoutMemory { get; set; }

    /// <summary>Markdown table with token columns first.</summary>
    public string SummaryMarkdown { get; set; } = string.Empty;

    /// <summary>Disposable bench workspace id used for seeds.</summary>
    public string BenchWorkspaceId { get; set; } = string.Empty;

    /// <summary>Primary workspace id that must remain untouched.</summary>
    public string PrimaryWorkspaceId { get; set; } = string.Empty;

    /// <summary>True when primary workspace memories were mutated.</summary>
    public bool PrimaryWorkspaceMutated { get; set; }

    /// <summary>Gate mode: report-only unless Memory:Bench:Gate=true.</summary>
    public bool GateEnabled { get; set; }

    /// <summary>Optional efficiency ratio labeled secondary.</summary>
    public double? EfficiencyTokensPerPass { get; set; }

    /// <summary>Seed ids created during the run (cleaned up).</summary>
    public List<string> SeededMemoryIds { get; set; } = [];
}

/// <summary>
/// TR-MCP-MEMORY-BENCH-002-05: Harness run options.
/// </summary>
public sealed class MemoryBenchRunOptions
{
    /// <summary>Plugin ids to run. Default is grok only.</summary>
    public IReadOnlyList<string> Plugins { get; init; } = ["grok"];

    /// <summary>stub, recorded, or live.</summary>
    public string Mode { get; init; } = MemoryBenchModes.Stub;

    /// <summary>Suppress REQUIRED MEMORIES injection.</summary>
    public bool SuppressInjection { get; init; }

    /// <summary>Suppress memory_* tool registration.</summary>
    public bool SuppressTools { get; init; }

    /// <summary>When true, CI fails on lift/correctness gate misses.</summary>
    public bool GateEnabled { get; init; }

    /// <summary>Directory for result artifacts. Null skips write.</summary>
    public string? ResultsDirectory { get; init; }

    /// <summary>Optional fixed UTC stamp for artifact names.</summary>
    public string? UtcStamp { get; init; }

    /// <summary>Primary workspace id that isolation must not touch.</summary>
    public string PrimaryWorkspaceId { get; init; } = "primary-operator-workspace";

    /// <summary>Optional adapter overrides keyed by plugin id.</summary>
    public IReadOnlyDictionary<string, IMemoryBenchPluginAdapter>? Adapters { get; init; }

    /// <summary>Optional multi-turn adapter overrides keyed by plugin id.</summary>
    public IReadOnlyDictionary<string, IMemoryBenchMultiTurnAdapter>? MultiTurnAdapters { get; init; }
}

/// <summary>
/// TR-MCP-MEMORY-BENCH-002-02: One adapter turn.
/// </summary>
public sealed class MemoryBenchTurnContext
{
    /// <summary>Active plugin id.</summary>
    public required string Plugin { get; init; }

    /// <summary>Pack prompt.</summary>
    public required MemoryBenchPrompt Prompt { get; init; }

    /// <summary>without_memory or with_memory.</summary>
    public required string Condition { get; init; }

    /// <summary>Run mode.</summary>
    public required string Mode { get; init; }

    /// <summary>Injection block or empty.</summary>
    public string Injection { get; init; } = string.Empty;

    /// <summary>Effective seeded contents visible to the adapter.</summary>
    public IReadOnlyList<MemoryBenchSeededMemory> EffectiveMemories { get; init; } = [];

    /// <summary>True when memory_* tools may be used.</summary>
    public bool ToolsAvailable { get; init; }

    /// <summary>Disposable bench workspace id.</summary>
    public string BenchWorkspaceId { get; init; } = string.Empty;
}

/// <summary>
/// TR-MCP-MEMORY-BENCH-002-02: Adapter turn output before scoring/token finalize.
/// </summary>
public sealed class MemoryBenchTurnResult
{
    /// <summary>Assistant answer.</summary>
    public string Answer { get; init; } = string.Empty;

    /// <summary>Transcript text.</summary>
    public string Transcript { get; init; } = string.Empty;

    /// <summary>Tool names used.</summary>
    public IReadOnlyList<string> ToolCalls { get; init; } = [];

    /// <summary>Tool payload plus result text included in tokens_in.</summary>
    public string ToolPayloadText { get; init; } = string.Empty;

    /// <summary>Optional host-reported tokens_in.</summary>
    public int? HostTokensIn { get; init; }

    /// <summary>Optional host-reported tokens_out.</summary>
    public int? HostTokensOut { get; init; }

    /// <summary>Latency for the turn.</summary>
    public int LatencyMs { get; init; }

    /// <summary>Token source hint from the adapter.</summary>
    public string TokenSourceHint { get; init; } = MemoryBenchTokenSources.Estimator;
}

/// <summary>
/// TR-MCP-MEMORY-BENCH-002-10: Hard-fail when a cell is missing required token fields.
/// </summary>
public sealed class MemoryBenchTokenSchemaException : InvalidOperationException
{
    /// <summary>Creates the hard-fail exception.</summary>
    public MemoryBenchTokenSchemaException(string message)
        : base(message)
    {
    }
}
