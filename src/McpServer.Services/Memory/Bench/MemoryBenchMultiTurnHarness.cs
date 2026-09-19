using System.Text;
using System.Text.Json;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-019 / TR-MCP-MEMORY-BENCH-003: Multi-turn Grok-first harness.
/// without_memory may replay establish transcript (expensive baseline).
/// with_memory injects compact memories and must not replay that transcript.
/// </summary>
public sealed class MemoryBenchMultiTurnHarness
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = SnakeCasePolicy.Instance,
        WriteIndented = true,
    };

    /// <summary>Loads the canonical v2 pack and runs the requested plugins.</summary>
    public MemoryBenchMultiTurnRunResult Run(string repositoryRoot, MemoryBenchRunOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var pack = MemoryBenchMultiTurnPackLoader.LoadFromRepo(repositoryRoot);
        return Run(pack, options ?? new MemoryBenchRunOptions());
    }

    /// <summary>Runs the multi-turn pack under stub/recorded/live mode.</summary>
    public MemoryBenchMultiTurnRunResult Run(MemoryBenchMultiTurnPackDocument pack, MemoryBenchRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(options);

        var plugins = options.Plugins.Count == 0 ? [MemoryBenchAdapterRegistry.DefaultPlugin] : options.Plugins.ToList();
        if (plugins.Any(plugin => plugin.Equals("all", StringComparison.OrdinalIgnoreCase)))
        {
            if (!MemoryBenchValueGate.IsH7aAgreed())
                throw new InvalidOperationException("-Plugin all is blocked until H7a AGREE.");
            plugins = pack.Plugins.ToList();
        }

        if (plugins.Any(plugin => !plugin.Equals("grok", StringComparison.OrdinalIgnoreCase))
            && !MemoryBenchValueGate.IsH7aAgreed())
        {
            throw new InvalidOperationException("Non-Grok plugins are opt-in after H7a AGREE.");
        }

        var adapters = MemoryBenchMultiTurnAdapterRegistry.Resolve(plugins, options);
        var benchWorkspaceId = "bench-mt-" + Guid.NewGuid().ToString("N");
        var result = new MemoryBenchMultiTurnRunResult
        {
            PackId = pack.Id,
            Mode = options.Mode,
            Plugins = plugins,
            BenchWorkspaceId = benchWorkspaceId,
            PrimaryWorkspaceId = options.PrimaryWorkspaceId,
            GateEnabled = options.GateEnabled,
        };

        foreach (var plugin in plugins)
        {
            var adapter = adapters[plugin];
            foreach (var job in pack.Jobs)
            {
                result.Jobs.Add(ExecuteJob(adapter, pack, job, "without_memory", options, benchWorkspaceId, result.Turns));
                result.Jobs.Add(ExecuteJob(adapter, pack, job, "with_memory", options, benchWorkspaceId, result.Turns));
            }
        }

        result.PrimaryWorkspaceMutated = false;
        MemoryBenchMultiTurnResultSchema.ValidateBeforeWrite(result);
        MemoryBenchMultiTurnReport.Populate(result);

        if (options.ResultsDirectory is not null)
            WriteArtifacts(result, options);

        if (options.GateEnabled)
            ApplyGate(result);

        return result;
    }

    private static MemoryBenchJobCellResult ExecuteJob(
        IMemoryBenchMultiTurnAdapter adapter,
        MemoryBenchMultiTurnPackDocument pack,
        MemoryBenchJob job,
        string condition,
        MemoryBenchRunOptions options,
        string benchWorkspaceId,
        List<MemoryBenchTurnCellResult> turns)
    {
        var withMemory = condition == "with_memory";
        var stored = new List<MemoryBenchSeededMemory>();
        var history = new StringBuilder();
        var jobTurns = new List<MemoryBenchTurnCellResult>();

        foreach (var turn in job.Turns)
        {
            var replay = !withMemory;
            var priorTranscript = replay ? history.ToString().TrimEnd() : string.Empty;
            var toolsAvailable = withMemory && !options.SuppressTools;
            var inject = withMemory && !options.SuppressInjection && stored.Count > 0 && turn.Role == "query";
            var injection = inject ? RenderInjection(stored) : string.Empty;
            var promptPayload = pack.Id + " " + (string.IsNullOrWhiteSpace(priorTranscript) ? string.Empty : priorTranscript + " ") + turn.UserText;

            var executed = adapter.Execute(new MemoryBenchMultiTurnContext
            {
                Plugin = adapter.PluginId,
                Job = job,
                Turn = turn,
                Condition = condition,
                Mode = options.Mode,
                Injection = injection,
                EffectiveMemories = stored,
                ToolsAvailable = toolsAvailable,
                PriorTranscript = priorTranscript,
                ReplayEstablishTranscript = replay,
                PromptPayload = promptPayload,
                BenchWorkspaceId = benchWorkspaceId,
            });

            var (estimatedIn, estimatedOut, _) = MemoryBenchTokenEstimator.EstimateTurn(
                promptPayload,
                injection,
                executed.ToolPayloadText,
                executed.Answer);

            var tokensIn = executed.HostTokensIn ?? estimatedIn;
            var tokensOut = executed.HostTokensOut ?? estimatedOut;
            var turnTokenSource = executed.HostTokensIn is not null
                ? MemoryBenchTokenSources.Host
                : executed.TokenSourceHint;

            var (score, pass, notes) = MemoryBenchMultiTurnScoring.Score(job, turn, executed.Answer);
            var cell = new MemoryBenchTurnCellResult
            {
                Plugin = adapter.PluginId,
                JobId = job.Id,
                TurnId = turn.Id,
                Role = turn.Role,
                Condition = condition,
                Required = turn.Required,
                Transcript = executed.Transcript,
                PromptPayload = promptPayload + (string.IsNullOrWhiteSpace(injection) ? string.Empty : "\n" + injection),
                ToolCalls = executed.ToolCalls.ToList(),
                InjectionPresent = !string.IsNullOrWhiteSpace(injection)
                    && executed.Transcript.Contains("REQUIRED MEMORIES", StringComparison.Ordinal),
                TokensIn = tokensIn,
                TokensOut = tokensOut,
                TokensTotal = tokensIn + tokensOut,
                TokenSource = turnTokenSource,
                EstimatorId = turnTokenSource == MemoryBenchTokenSources.Estimator ? MemoryBenchTokenEstimator.EstimatorId : null,
                EstimatorVersion = turnTokenSource == MemoryBenchTokenSources.Estimator ? MemoryBenchTokenEstimator.EstimatorVersion : null,
                LatencyMs = executed.LatencyMs,
                Score = score,
                Pass = pass,
                Notes = notes,
                InjectionTokens = MemoryBenchTokenEstimator.Count(injection),
                ToolTokens = MemoryBenchTokenEstimator.Count(executed.ToolPayloadText),
                Answer = executed.Answer,
            };

            if (withMemory && turn.Role == "query")
            {
                var establishTexts = job.Turns
                    .Where(item => item.Role == "establish")
                    .Select(item => item.UserText)
                    .Where(text => text.Length > 40)
                    .ToList();
                if (establishTexts.Any(text => cell.PromptPayload.Contains(text, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException(
                        "with_memory must not replay the full establish transcript in the prompt: "
                        + job.Id + "/" + turn.Id);
                }
            }

            MemoryBenchMultiTurnResultSchema.ValidateTurn(cell);
            jobTurns.Add(cell);
            turns.Add(cell);

            if (replay)
            {
                if (history.Length > 0)
                    history.AppendLine();
                history.AppendLine("user: " + turn.UserText);
                history.AppendLine("assistant: " + executed.Answer);
            }

            if (withMemory && turn.Role == "establish" && turn.WriteMemories)
                stored.AddRange(turn.ExpectedMemories);
        }

        var required = jobTurns.Where(turn => turn.Required).ToList();
        var failed = required.Where(turn => !turn.Pass).Select(turn => turn.TurnId).ToList();
        var sources = jobTurns.Select(turn => turn.TokenSource).Distinct(StringComparer.Ordinal).ToList();
        var tokenSource = sources.Count == 1 ? sources[0] : MemoryBenchTokenSources.Estimator;
        var jobCell = new MemoryBenchJobCellResult
        {
            Plugin = adapter.PluginId,
            JobId = job.Id,
            Class = job.Class,
            Condition = condition,
            TokensIn = jobTurns.Sum(turn => turn.TokensIn),
            TokensOut = jobTurns.Sum(turn => turn.TokensOut),
            TokensTotal = jobTurns.Sum(turn => turn.TokensTotal),
            TokenSource = tokenSource,
            EstimatorId = tokenSource == MemoryBenchTokenSources.Estimator ? MemoryBenchTokenEstimator.EstimatorId : null,
            EstimatorVersion = tokenSource == MemoryBenchTokenSources.Estimator ? MemoryBenchTokenEstimator.EstimatorVersion : null,
            Pass = required.Count > 0 && failed.Count == 0,
            FailedTurnIds = failed,
            TurnsN = jobTurns.Count,
        };
        MemoryBenchMultiTurnResultSchema.ValidateJob(jobCell);
        return jobCell;
    }

    private static string RenderInjection(IReadOnlyList<MemoryBenchSeededMemory> memories)
    {
        var now = DateTimeOffset.UnixEpoch;
        var items = memories
            .Select((memory, index) => new MemoryItem
            {
                Id = "MEMORY-BENCH-MT-" + (index + 1).ToString("000"),
                Category = memory.Type,
                Scope = MemoryScope.Workspace,
                Text = memory.Content,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Title = memory.Title,
                Content = memory.Content,
                Type = memory.Type,
                Tags = memory.Tags,
                Confidence = memory.Confidence,
            })
            .ToList();
        return MemoryRequiredMemoriesRenderer.RenderRequiredMemories(items);
    }

    private static void ApplyGate(MemoryBenchMultiTurnRunResult result)
    {
        var failedRequired = result.Jobs.Any(job =>
            job.Condition == "with_memory"
            && !job.Pass
            && (job.JobId.Contains("-PREF-", StringComparison.Ordinal)
                || job.JobId.Contains("-DEC-", StringComparison.Ordinal)
                || job.JobId.Contains("-FACT-", StringComparison.Ordinal)));
        if (failedRequired)
            throw new InvalidOperationException("Memory:Bench:Gate=true failed required with_memory multi-turn jobs.");
    }

    private static void WriteArtifacts(MemoryBenchMultiTurnRunResult result, MemoryBenchRunOptions options)
    {
        Directory.CreateDirectory(options.ResultsDirectory!);
        var stamp = options.UtcStamp ?? DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var jsonPath = Path.Combine(options.ResultsDirectory!, "memory-bench-multiturn-" + stamp + ".json");
        var mdPath = Path.Combine(options.ResultsDirectory!, "memory-bench-multiturn-" + stamp + ".md");
        var json = JsonSerializer.Serialize(result, JsonOptions);
        MemoryBenchMultiTurnResultSchema.ValidateJson(json);
        File.WriteAllText(jsonPath, json);
        File.WriteAllText(mdPath, result.SummaryMarkdown);
    }

    private sealed class SnakeCasePolicy : JsonNamingPolicy
    {
        public static readonly SnakeCasePolicy Instance = new();

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var chars = new List<char>(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                        chars.Add('_');
                    chars.Add(char.ToLowerInvariant(c));
                }
                else
                {
                    chars.Add(c);
                }
            }

            return new string(chars.ToArray());
        }
    }
}

/// <summary>
/// FR-MCP-MEMORY-019-07: Resolves multi-turn adapters. Grok is default; others after H7a.
/// </summary>
public static class MemoryBenchMultiTurnAdapterRegistry
{
    /// <summary>Creates the default Grok multi-turn adapter.</summary>
    public static IMemoryBenchMultiTurnAdapter CreateGrok() => new MemoryBenchGrokMultiTurnAdapter();

    /// <summary>Creates the recorded/stub multi-turn adapter for a known plugin id.</summary>
    public static IMemoryBenchMultiTurnAdapter Create(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return pluginId.Trim().ToLowerInvariant() switch
        {
            "grok" => CreateGrok(),
            "claude-code" => new MemoryBenchClaudeCodeMultiTurnAdapter(),
            "claude-cowork" => new MemoryBenchClaudeCoworkMultiTurnAdapter(),
            "cline" => new MemoryBenchClineMultiTurnAdapter(),
            "cline-v2" => new MemoryBenchClineV2MultiTurnAdapter(),
            "codex" => new MemoryBenchCodexMultiTurnAdapter(),
            "copilot" => new MemoryBenchCopilotMultiTurnAdapter(),
            "opencode" => new MemoryBenchOpencodeMultiTurnAdapter(),
            _ => throw new InvalidOperationException($"Unknown MemoryBench multi-turn plugin '{pluginId}'."),
        };
    }

    /// <summary>Resolves adapters for the requested plugins.</summary>
    public static IReadOnlyDictionary<string, IMemoryBenchMultiTurnAdapter> Resolve(
        IReadOnlyList<string> plugins,
        MemoryBenchRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        var map = new Dictionary<string, IMemoryBenchMultiTurnAdapter>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in plugins)
        {
            if (options.MultiTurnAdapters is not null
                && options.MultiTurnAdapters.TryGetValue(plugin, out var over))
            {
                map[plugin] = over;
                continue;
            }

            if (string.Equals(plugin, MemoryBenchAdapterRegistry.DefaultPlugin, StringComparison.OrdinalIgnoreCase))
            {
                map[plugin] = CreateGrok();
                continue;
            }

            if (!MemoryBenchValueGate.IsH7aAgreed())
            {
                throw new InvalidOperationException(
                    $"Plugin '{plugin}' is deferred until H7a AGREE. Default/required multi-turn adapter is grok.");
            }

            map[plugin] = Create(plugin);
        }

        return map;
    }
}
