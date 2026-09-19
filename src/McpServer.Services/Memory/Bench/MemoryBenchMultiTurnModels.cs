using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-019 / TR-MCP-MEMORY-BENCH-003: One ordered turn inside a multi-turn job.
/// </summary>
public sealed class MemoryBenchJobTurn
{
    /// <summary>Stable turn id such as EST-001 or QRY-001.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>establish or query.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>User text for this turn. Query turns must not restate gold facts.</summary>
    [JsonPropertyName("user_text")]
    public string UserText { get; set; } = string.Empty;

    /// <summary>When true, this query turn must pass for the job to succeed. Establish defaults false.</summary>
    public bool Required { get; set; }

    /// <summary>Scoring mode for query turns: exact, contains, or rubric.</summary>
    public string Scoring { get; set; } = "contains";

    /// <summary>Gold rubric text.</summary>
    [JsonPropertyName("gold_answer_rubric")]
    public string GoldAnswerRubric { get; set; } = string.Empty;

    /// <summary>Explicit tokens the answer must contain when scoring is contains/rubric.</summary>
    [JsonPropertyName("gold_tokens")]
    public List<string> GoldTokens { get; set; } = [];

    /// <summary>Phrases that must not appear in <see cref="UserText"/> (query must not restate facts).</summary>
    [JsonPropertyName("query_must_not_contain")]
    public List<string> QueryMustNotContain { get; set; } = [];

    /// <summary>Claims the answer must not make.</summary>
    [JsonPropertyName("forbidden_claims")]
    public List<string> ForbiddenClaims { get; set; } = [];

    /// <summary>When true, with_memory establish turns write <see cref="ExpectedMemories"/> via tools.</summary>
    [JsonPropertyName("write_memories")]
    public bool WriteMemories { get; set; }

    /// <summary>Memories created after this establish turn under with_memory.</summary>
    [JsonPropertyName("expected_memories")]
    public List<MemoryBenchSeededMemory> ExpectedMemories { get; set; } = [];
}

/// <summary>
/// FR-MCP-MEMORY-019: One multi-turn job with establish then query turns.
/// </summary>
public sealed class MemoryBenchJob
{
    /// <summary>Stable job id such as JOB-PREF-001.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Job class token (preference, decision, fact, multi-fact, negative-refuse).</summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>Ordered turns. Establish turns precede required query turns.</summary>
    public List<MemoryBenchJobTurn> Turns { get; set; } = [];
}

/// <summary>
/// FR-MCP-MEMORY-019-01: Versioned multi-turn prompt pack.
/// </summary>
public sealed class MemoryBenchMultiTurnPackDocument
{
    /// <summary>Pack semver.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Pack id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Pack kind. Must be multi_turn_jobs.</summary>
    public string Kind { get; set; } = "multi_turn_jobs";

    /// <summary>Documented role of this pack (real_efficiency_bench).</summary>
    [JsonPropertyName("pack_role")]
    public string PackRole { get; set; } = "real_efficiency_bench";

    /// <summary>Documented role of the v1 pack (smoke/regression).</summary>
    [JsonPropertyName("v1_role")]
    public string V1Role { get; set; } = "smoke/regression";

    /// <summary>Human description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Known plugin ids.</summary>
    public List<string> Plugins { get; set; } = [];

    /// <summary>Paired conditions.</summary>
    public List<string> Conditions { get; set; } = [];

    /// <summary>Jobs with ordered turns.</summary>
    public List<MemoryBenchJob> Jobs { get; set; } = [];

    /// <summary>Primary metric name.</summary>
    [JsonPropertyName("primary_metric")]
    public string PrimaryMetric { get; set; } = "tokens_total";

    /// <summary>Required per-turn and per-job token fields.</summary>
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
/// TR-MCP-MEMORY-BENCH-003: Adapter context for one job turn.
/// </summary>
public sealed class MemoryBenchMultiTurnContext
{
    /// <summary>Active plugin id.</summary>
    public required string Plugin { get; init; }

    /// <summary>Parent job.</summary>
    public required MemoryBenchJob Job { get; init; }

    /// <summary>Current turn.</summary>
    public required MemoryBenchJobTurn Turn { get; init; }

    /// <summary>without_memory or with_memory.</summary>
    public required string Condition { get; init; }

    /// <summary>Run mode.</summary>
    public required string Mode { get; init; }

    /// <summary>Injection block or empty.</summary>
    public string Injection { get; init; } = string.Empty;

    /// <summary>Memories visible after prior establish turns (with_memory only).</summary>
    public IReadOnlyList<MemoryBenchSeededMemory> EffectiveMemories { get; init; } = [];

    /// <summary>True when memory_* tools may be used.</summary>
    public bool ToolsAvailable { get; init; }

    /// <summary>Prior-turn transcript replayed into the prompt. Empty for with_memory.</summary>
    public string PriorTranscript { get; init; } = string.Empty;

    /// <summary>True when the adapter/harness may include conversation history in the prompt.</summary>
    public bool ReplayEstablishTranscript { get; init; }

    /// <summary>System + history + user text actually charged to tokens_in (excluding tools).</summary>
    public string PromptPayload { get; init; } = string.Empty;

    /// <summary>Disposable bench workspace id.</summary>
    public string BenchWorkspaceId { get; init; } = string.Empty;
}

/// <summary>
/// FR-MCP-MEMORY-019: One plugin × job × turn × condition cell.
/// </summary>
public sealed class MemoryBenchTurnCellResult
{
    /// <summary>Plugin id.</summary>
    public string Plugin { get; set; } = string.Empty;

    /// <summary>Job id.</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>Turn id.</summary>
    public string TurnId { get; set; } = string.Empty;

    /// <summary>establish or query.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>without_memory or with_memory.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>True when this turn is required for job success.</summary>
    public bool Required { get; set; }

    /// <summary>Full turn transcript (current turn only).</summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>Prompt text charged to tokens_in excluding tool payloads.</summary>
    public string PromptPayload { get; set; } = string.Empty;

    /// <summary>Tool names invoked during the turn.</summary>
    public List<string> ToolCalls { get; set; } = [];

    /// <summary>True when REQUIRED MEMORIES was injected.</summary>
    public bool InjectionPresent { get; set; }

    /// <summary>Prompt/system/injection/tool-result tokens.</summary>
    public int TokensIn { get; set; }

    /// <summary>Completion tokens.</summary>
    public int TokensOut { get; set; }

    /// <summary>tokens_in + tokens_out.</summary>
    public int TokensTotal { get; set; }

    /// <summary>host, estimator, or recorded.</summary>
    public string TokenSource { get; set; } = MemoryBenchTokenSources.Estimator;

    /// <summary>Estimator id when token_source is estimator.</summary>
    public string? EstimatorId { get; set; }

    /// <summary>Estimator version when token_source is estimator.</summary>
    public string? EstimatorVersion { get; set; }

    /// <summary>Secondary latency metric.</summary>
    public int LatencyMs { get; set; }

    /// <summary>Numeric score in [0,1]. Establish turns default to 1.</summary>
    public double Score { get; set; }

    /// <summary>Correctness/safety gate for required query turns.</summary>
    public bool Pass { get; set; } = true;

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
/// FR-MCP-MEMORY-019: One plugin × job × condition aggregate.
/// </summary>
public sealed class MemoryBenchJobCellResult
{
    /// <summary>Plugin id.</summary>
    public string Plugin { get; set; } = string.Empty;

    /// <summary>Job id.</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>Job class.</summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>without_memory or with_memory.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Sum of turn tokens_in.</summary>
    public int TokensIn { get; set; }

    /// <summary>Sum of turn tokens_out.</summary>
    public int TokensOut { get; set; }

    /// <summary>Sum of turn tokens_total.</summary>
    public int TokensTotal { get; set; }

    /// <summary>host if every turn is host; recorded if every turn is recorded; otherwise estimator.</summary>
    public string TokenSource { get; set; } = MemoryBenchTokenSources.Estimator;

    /// <summary>Estimator id when token_source is estimator.</summary>
    public string? EstimatorId { get; set; }

    /// <summary>Estimator version when token_source is estimator.</summary>
    public string? EstimatorVersion { get; set; }

    /// <summary>True only when every required query turn passed.</summary>
    public bool Pass { get; set; }

    /// <summary>Required query turn ids that failed scoring.</summary>
    public List<string> FailedTurnIds { get; set; } = [];

    /// <summary>Turn count in this job × condition.</summary>
    public int TurnsN { get; set; }
}

/// <summary>
/// FR-MCP-MEMORY-019: Success-gated token comparison. Failed jobs stay in the artifact.
/// </summary>
public sealed class MemoryBenchSuccessGatedSummary
{
    /// <summary>Job count per condition (same jobs under both conditions).</summary>
    public int JobsN { get; set; }

    /// <summary>Jobs that passed under with_memory.</summary>
    public int SuccessfulJobsWithMemory { get; set; }

    /// <summary>Jobs that passed under without_memory.</summary>
    public int SuccessfulJobsWithoutMemory { get; set; }

    /// <summary>Jobs that passed under both conditions (paired comparison set).</summary>
    public int SuccessfulJobsPaired { get; set; }

    /// <summary>with_memory success rate.</summary>
    public double SuccessRateWithMemory { get; set; }

    /// <summary>without_memory success rate.</summary>
    public double SuccessRateWithoutMemory { get; set; }

    /// <summary>Mean tokens_total over successful with_memory jobs only.</summary>
    public double? SuccessGatedMeanTokensWithMemory { get; set; }

    /// <summary>Median tokens_total over successful with_memory jobs only.</summary>
    public double? SuccessGatedMedianTokensWithMemory { get; set; }

    /// <summary>Mean tokens_total over successful without_memory jobs only.</summary>
    public double? SuccessGatedMeanTokensWithoutMemory { get; set; }

    /// <summary>Median tokens_total over successful without_memory jobs only.</summary>
    public double? SuccessGatedMedianTokensWithoutMemory { get; set; }

    /// <summary>Mean tokens_total(with_memory) over jobs that passed both conditions.</summary>
    public double? PairedSuccessMeanTokensWithMemory { get; set; }

    /// <summary>Mean tokens_total(without_memory) over jobs that passed both conditions.</summary>
    public double? PairedSuccessMeanTokensWithoutMemory { get; set; }

    /// <summary>True when paired-success with_memory mean tokens is lower than without_memory.</summary>
    public bool? WithMemoryWonOnPairedSuccessTokens { get; set; }

    /// <summary>Documents that failed jobs are excluded from success-gated means.</summary>
    public string ExclusionNote { get; set; } =
        "Failed jobs appear in the artifact with pass=false and are excluded from the success-gated token means. Do not claim an efficiency win from failed without_memory runs.";
}

/// <summary>
/// FR-MCP-MEMORY-019: Completed multi-turn bench run.
/// </summary>
public sealed class MemoryBenchMultiTurnRunResult
{
    /// <summary>Pack id.</summary>
    public string PackId { get; set; } = string.Empty;

    /// <summary>stub, recorded, or live.</summary>
    public string Mode { get; set; } = MemoryBenchModes.Stub;

    /// <summary>Plugins actually executed.</summary>
    public List<string> Plugins { get; set; } = [];

    /// <summary>Per-turn results.</summary>
    public List<MemoryBenchTurnCellResult> Turns { get; set; } = [];

    /// <summary>Per-job aggregates including failed jobs (pass=false).</summary>
    public List<MemoryBenchJobCellResult> Jobs { get; set; } = [];

    /// <summary>Success-gated token comparison.</summary>
    public MemoryBenchSuccessGatedSummary SuccessGated { get; set; } = new();

    /// <summary>Markdown with token tables first and success-gated means highlighted.</summary>
    public string SummaryMarkdown { get; set; } = string.Empty;

    /// <summary>Disposable bench workspace id.</summary>
    public string BenchWorkspaceId { get; set; } = string.Empty;

    /// <summary>Primary workspace id that must remain untouched.</summary>
    public string PrimaryWorkspaceId { get; set; } = string.Empty;

    /// <summary>True when primary workspace memories were mutated.</summary>
    public bool PrimaryWorkspaceMutated { get; set; }

    /// <summary>Gate mode.</summary>
    public bool GateEnabled { get; set; }
}

/// <summary>
/// TR-MCP-MEMORY-BENCH-003: Multi-turn adapter contract.
/// </summary>
public interface IMemoryBenchMultiTurnAdapter
{
    /// <summary>Plugin id such as grok.</summary>
    string PluginId { get; }

    /// <summary>Supported validation entrypoint.</summary>
    string ValidationEntrypoint { get; }

    /// <summary>Executes one job turn. Stub/recorded must not require cloud.</summary>
    MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context);
}
