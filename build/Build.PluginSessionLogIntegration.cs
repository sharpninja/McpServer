using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>
    /// TEST-MCP-PLUGININT-001 AC5: run PluginIntegration tests and treat skipped tests as failures.
    /// </summary>
    public Target PluginSessionLogIntegration => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = TestsDirectory / "McpServer.PluginIntegration.Tests" / "McpServer.PluginIntegration.Tests.csproj";
            if (!File.Exists(project))
            {
                throw new FileNotFoundException("Plugin integration test project is missing.", project.ToString());
            }

            PreflightPluginSessionLogAiUnitStrategy(RootDirectory);

            var resultsDir = RootDirectory / "TestResults";
            Directory.CreateDirectory(resultsDir);

            var deterministicTrx = "plugin-sessionlog-deterministic.trx";
            DotNetTest(_ => _
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .SetFilter("PluginInt=Deterministic")
                .SetResultsDirectory(resultsDir)
                .SetLoggers($"trx;LogFileName={deterministicTrx}"));
            FailIfPluginSessionLogSkipped(resultsDir / deterministicTrx);

            var aiTrx = "plugin-sessionlog-ai.trx";
            DotNetTest(_ => _
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .SetFilter("PluginInt=AI")
                .SetResultsDirectory(resultsDir)
                .SetLoggers($"trx;LogFileName={aiTrx}"));
            FailIfPluginSessionLogSkipped(resultsDir / aiTrx);
        });

    /// <summary>
    /// TEST-MCP-PLUGININT-001 AC5: fail closed when aiUnit strategy metadata is missing.
    /// </summary>
    /// <param name="repositoryRoot">McpServer repository root.</param>
    internal static void PreflightPluginSessionLogAiUnitStrategy(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var csproj = Path.Combine(repositoryRoot, "tests", "McpServer.PluginIntegration.Tests", "McpServer.PluginIntegration.Tests.csproj");
        if (!File.Exists(csproj) || !File.ReadAllText(csproj).Contains("SharpNinja.aiUnit", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PluginSessionLogIntegration aiUnit preflight failed: SharpNinja.aiUnit is not referenced.");
        }

        var settingsPath = Path.Combine(repositoryRoot, "tests", "McpServer.PluginIntegration.Tests", "appsettings.aiunit.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException("PluginSessionLogIntegration aiUnit preflight failed: appsettings.aiunit.json is missing.", settingsPath);
        }

        var json = File.ReadAllText(settingsPath);
        if (!json.Contains("\"ActiveStrategy\"", StringComparison.Ordinal)
            || !json.Contains("\"Strategies\"", StringComparison.Ordinal)
            || !json.Contains("grok-build", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PluginSessionLogIntegration aiUnit preflight failed: ActiveStrategy is not available.");
        }

        Log.Information("PluginSessionLogIntegration aiUnit strategy preflight passed.");
    }

    /// <summary>
    /// Fails the PluginSessionLogIntegration target when the TRX counters report skipped tests.
    /// </summary>
    internal static void FailIfPluginSessionLogSkipped(string trxPath)
    {
        if (!File.Exists(trxPath))
        {
            throw new FileNotFoundException("PluginSessionLogIntegration TRX was not written.", trxPath);
        }

        var document = XDocument.Load(trxPath);
        XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;
        var counters = document.Descendants(ns + "Counters").FirstOrDefault();
        var skipped = int.TryParse(counters?.Attribute("skipped")?.Value, out var skippedCount) ? skippedCount : 0;
        var notExecuted = int.TryParse(counters?.Attribute("notExecuted")?.Value, out var notExecutedCount) ? notExecutedCount : 0;
        var total = int.TryParse(counters?.Attribute("total")?.Value, out var totalCount) ? totalCount : 0;
        var executed = int.TryParse(counters?.Attribute("executed")?.Value, out var executedCount) ? executedCount : 0;
        var failed = int.TryParse(counters?.Attribute("failed")?.Value, out var failedCount) ? failedCount : 0;
        if (total == 0 || failed > 0 || skipped > 0 || notExecuted > 0 || executed < total)
        {
            throw new InvalidOperationException(
                $"PluginSessionLogIntegration SkipIsFailure: skipped={skipped} notExecuted={notExecuted} executed={executed} total={total} failed={failed}.");
        }

        Log.Information("PluginSessionLogIntegration SkipIsFailure passed (skipped=0).");
    }
}
