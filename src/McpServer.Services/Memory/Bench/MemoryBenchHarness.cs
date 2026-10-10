using System.Text.Json;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018 / TR-MCP-MEMORY-BENCH-002: Grok-first with/without memory harness.
/// Tokens are the primary metric. Missing token fields hard-fail finalize.
/// </summary>
public sealed class MemoryBenchHarness
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = SnakeCasePolicy.Instance,
        WriteIndented = true,
    };

    /// <summary>Loads the canonical pack and runs the requested plugins.</summary>
    public MemoryBenchRunResult Run(string repositoryRoot, MemoryBenchRunOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var pack = MemoryBenchPackLoader.LoadFromRepo(repositoryRoot);
        return Run(pack, options ?? new MemoryBenchRunOptions());
    }

    /// <summary>Runs the pack under stub/recorded/live mode.</summary>
    public MemoryBenchRunResult Run(MemoryBenchPackDocument pack, MemoryBenchRunOptions options)
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

        var adapters = MemoryBenchAdapterRegistry.Resolve(plugins, options.Adapters);
        var benchWorkspaceId = "bench-" + Guid.NewGuid().ToString("N");
        var result = new MemoryBenchRunResult
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
            foreach (var prompt in pack.Prompts)
            {
                result.Cells.Add(ExecuteCell(adapter, prompt, "without_memory", options, benchWorkspaceId, pack));
                result.Cells.Add(ExecuteCell(adapter, prompt, "with_memory", options, benchWorkspaceId, pack));
            }
        }

        result.PrimaryWorkspaceMutated = false;
        result.SeededMemoryIds = result.Cells
            .SelectMany(cell => cell.Notes.Contains("seed:", StringComparison.Ordinal)
                ? new[] { cell.PromptId }
                : Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        MemoryBenchResultSchema.ValidateBeforeWrite(result);
        MemoryBenchReport.Populate(result);

        if (options.ResultsDirectory is not null)
            WriteArtifacts(result, options);

        if (options.GateEnabled)
            ApplyGate(result);

        return result;
    }

    /// <summary>Finalizes a possibly incomplete run. Missing tokens hard-fail.</summary>
    public static void Finalize(MemoryBenchRunResult result)
        => MemoryBenchResultSchema.ValidateBeforeWrite(result);

    private static MemoryBenchCellResult ExecuteCell(
        IMemoryBenchPluginAdapter adapter,
        MemoryBenchPrompt prompt,
        string condition,
        MemoryBenchRunOptions options,
        string benchWorkspaceId,
        MemoryBenchPackDocument pack)
    {
        var withMemory = condition == "with_memory";
        var toolsAvailable = withMemory && !options.SuppressTools;
        var inject = withMemory && !options.SuppressInjection;
        var effective = withMemory ? prompt.SeededMemories : [];
        var injection = inject ? RenderInjection(effective) : string.Empty;

        var turn = adapter.Execute(new MemoryBenchTurnContext
        {
            Plugin = adapter.PluginId,
            Prompt = prompt,
            Condition = condition,
            Mode = options.Mode,
            Injection = injection,
            EffectiveMemories = effective,
            ToolsAvailable = toolsAvailable,
            BenchWorkspaceId = benchWorkspaceId,
        });

        var systemAndPrompt = pack.Id + " " + prompt.UserText;
        var (estimatedIn, estimatedOut, _) = MemoryBenchTokenEstimator.EstimateTurn(
            systemAndPrompt,
            injection,
            turn.ToolPayloadText,
            turn.Answer);

        var tokensIn = turn.HostTokensIn ?? estimatedIn;
        var tokensOut = turn.HostTokensOut ?? estimatedOut;
        var tokenSource = turn.HostTokensIn is not null
            ? MemoryBenchTokenSources.Host
            : turn.TokenSourceHint;

        var (score, pass, notes) = MemoryBenchScoring.Score(prompt, turn.Answer, condition);
        if (options.GateEnabled && !pass && IsCorrectnessRequired(prompt, withMemory))
            notes += ";gate-fail";

        var cell = new MemoryBenchCellResult
        {
            Plugin = adapter.PluginId,
            PromptId = prompt.Id,
            Condition = condition,
            Transcript = turn.Transcript,
            ToolCalls = turn.ToolCalls.ToList(),
            InjectionPresent = !string.IsNullOrWhiteSpace(injection)
                && turn.Transcript.Contains("REQUIRED MEMORIES", StringComparison.Ordinal),
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            TokensTotal = tokensIn + tokensOut,
            TokenSource = tokenSource,
            EstimatorId = tokenSource == MemoryBenchTokenSources.Estimator ? MemoryBenchTokenEstimator.EstimatorId : null,
            EstimatorVersion = tokenSource == MemoryBenchTokenSources.Estimator ? MemoryBenchTokenEstimator.EstimatorVersion : null,
            LatencyMs = turn.LatencyMs,
            Score = score,
            Pass = pass,
            Notes = notes,
            InjectionTokens = MemoryBenchTokenEstimator.Count(injection),
            ToolTokens = MemoryBenchTokenEstimator.Count(turn.ToolPayloadText),
            Answer = turn.Answer,
        };

        MemoryBenchResultSchema.ValidateCell(cell);
        return cell;
    }

    private static string RenderInjection(IReadOnlyList<MemoryBenchSeededMemory> memories)
    {
        var now = DateTimeOffset.UnixEpoch;
        var items = memories
            .Select((memory, index) => new MemoryItem
            {
                Id = "MEMORY-BENCH-" + (index + 1).ToString("000"),
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

    private static bool IsCorrectnessRequired(MemoryBenchPrompt prompt, bool withMemory)
        => withMemory && prompt.Class is "preference" or "decision" or "fact-paraphrase";

    private static void ApplyGate(MemoryBenchRunResult result)
    {
        var failedRequired = result.Cells.Any(cell =>
            cell.Condition == "with_memory"
            && !cell.Pass
            && (cell.PromptId.StartsWith("PREF-", StringComparison.Ordinal)
                || cell.PromptId.StartsWith("DEC-", StringComparison.Ordinal)
                || cell.PromptId.StartsWith("FACT-", StringComparison.Ordinal)));
        if (failedRequired)
            throw new InvalidOperationException("Memory:Bench:Gate=true failed required with_memory correctness cells.");
    }

    private static void WriteArtifacts(MemoryBenchRunResult result, MemoryBenchRunOptions options)
    {
        Directory.CreateDirectory(options.ResultsDirectory!);
        var stamp = options.UtcStamp ?? DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var jsonPath = Path.Combine(options.ResultsDirectory!, "memory-bench-" + stamp + ".json");
        var mdPath = Path.Combine(options.ResultsDirectory!, "memory-bench-" + stamp + ".md");
        var json = JsonSerializer.Serialize(result, JsonOptions);
        MemoryBenchResultSchema.ValidateJson(json);
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
