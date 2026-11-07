using System.Text.Json;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>BenchMemory plugin id. Default is grok. <c>all</c> is explicit and post-H7a.</summary>
    [Parameter("BenchMemory plugin id (default grok; all is post-H7a)")]
    public readonly string Plugin = "grok";

    /// <summary>When true, BenchMemory fails on required correctness/lift misses.</summary>
    [Parameter("Fail BenchMemory when Memory:Bench:Gate=true")]
    public readonly bool MemoryBenchGate;

    /// <summary>
    /// TR-MCP-MEMORY-BENCH-002-07 / AC-TR-MCP-MEMORY-BENCH-002-13:
    /// Stub-mode Grok bench by default. Token means print first.
    /// </summary>
    /// <remarks>2026-11-07, TR-MCP-MEMORY-BENCH-002-07; AC-TR-MCP-MEMORY-BENCH-002-13; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S6 of hostile validation finding R14-F4 moved most MemoryBench classes and MemoryIntegrationTests from QBrainAi.Support.Mcp.Tests into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (named for its root namespace; operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."), so the target runs its unchanged filter over QBrainAi.Support.Mcp.Tests and then over QBrainAi.Support.Mcp.EnvironmentIntegrationTests, both still after the token means and both built by Compile; the Grok default, the H7a block and the gate parameter are unchanged.</remarks>
    public Target BenchMemory => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var plugin = string.IsNullOrWhiteSpace(Plugin) ? "grok" : Plugin.Trim();
            if (plugin.Equals("all", StringComparison.OrdinalIgnoreCase) && !H7aAgreed())
                throw new InvalidOperationException("BenchMemory -Plugin all is blocked until H7a AGREE.");
            if (!plugin.Equals("grok", StringComparison.OrdinalIgnoreCase)
                && !plugin.Equals("all", StringComparison.OrdinalIgnoreCase)
                && !H7aAgreed())
            {
                throw new InvalidOperationException("Non-Grok BenchMemory plugins are optional until H7a AGREE.");
            }

            // TOKEN MEANS (primary metric) must stay the first metrics block before DotNetTest.
            PrintTokenMeansFirst(plugin);

            // TR-MCP-NUKE-019 AC-7: since slice S6 the MemoryBench and MemoryIntegrationTests classes live in the unit-owned
            // QBrainAi.Support.Mcp.Tests and the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests; the same
            // filter runs over both, in that order.
            string[] projectNames = ["QBrainAi.Support.Mcp.Tests", "QBrainAi.Support.Mcp.EnvironmentIntegrationTests"];
            foreach (var projectName in projectNames)
            {
                var project = TestsDirectory / projectName / (projectName + ".csproj");
                DotNetTest(_ => _
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .SetFilter("FullyQualifiedName~MemoryBench|FullyQualifiedName~MemoryIntegrationTests")
                    .SetResultsDirectory(RootDirectory / "TestResults"));
            }
        });

    private bool H7aAgreed()
    {
        var path = RootDirectory / "docs" / "benchmarks" / "h7a-value-gate.json";
        if (!File.Exists(path))
            return false;
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("agree", out var agree) && agree.GetBoolean();
    }

    private void PrintTokenMeansFirst(string plugin)
    {
        Log.Information("TOKEN MEANS (primary metric) plugin={Plugin} condition=with_memory|without_memory", plugin);
        Log.Information("tokens_in / tokens_out / tokens_total are required cell fields; pass/fail is correctness/safety only.");
        var fixture = RootDirectory / "docs" / "benchmarks" / "fixtures" / "grok-stub-token-summary.md";
        if (File.Exists(fixture))
            Log.Information("{Summary}", File.ReadAllText(fixture));
    }
}
