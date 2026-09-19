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

            var project = TestsDirectory / "McpServer.Support.Mcp.Tests" / "McpServer.Support.Mcp.Tests.csproj";
            DotNetTest(_ => _
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetFilter("FullyQualifiedName~MemoryBench|FullyQualifiedName~MemoryIntegrationTests")
                .SetResultsDirectory(RootDirectory / "TestResults"));
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
