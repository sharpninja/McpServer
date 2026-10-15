// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using Nuke.Common;
using Serilog;

partial class Build
{
    const string PluginCorePesterFolder = "plugins/core/test-fixtures/pester";

    static readonly string[] AdversePesterCounters = ["failures", "errors", "skipped", "ignored", "not-run", "inconclusive", "invalid"];

    /// <summary>
    /// TR-MCP-NUKE-012: Pester test files or folders to run, relative to the repository root, space- or comma-separated; the whole
    /// plugins/core/test-fixtures/pester folder when omitted.
    /// </summary>
    [Parameter("TR-MCP-NUKE-012: Pester test paths relative to the repository root (space- or comma-separated); the whole plugins/core/test-fixtures/pester folder when omitted.")]
    readonly string[] PesterPaths = [];

    /// <summary>
    /// TR-MCP-NUKE-012 (G4): runs the plugin core's Pester suites through Nuke with Pester 5 in the -CI configuration, natively on Windows
    /// and Linux. Every path is resolved before Pester starts (a missing path fails the target), and the run fails on any failure, error,
    /// skip, ignored, not-run, inconclusive or invalid test, or when it records no test. Results go to TestResults/&lt;run id&gt;/pester.
    /// </summary>
    public Target TestPluginCore => _ => _
        .Description("Run the plugin core Pester suites (Pester 5, -CI) through Nuke; fails on any failure, skip, or missing path (TR-MCP-NUKE-012)")
        .Executes(() =>
        {
            var requested = PesterPaths
                .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();
            var runId = SessionLifeUnitGateReports.NormalizeRunId(TestRunId)
                ?? "plugin-core-" + DateTime.Now.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
            var resultPath = (string)(RootDirectory / "TestResults" / runId / "pester" / "results.xml");
            var runner = (string)(RootDirectory / "build" / "Invoke-PluginCorePester.ps1");
            Log.Information("TestPluginCore: Pester on {Paths}, results {Result}", requested.Length == 0 ? PluginCorePesterFolder : string.Join(", ", requested), resultPath);

            RunPluginCorePester(RootDirectory, runner, requested, resultPath, startInfo =>
            {
                using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start pwsh for TestPluginCore.");
                process.WaitForExit();
                return process.ExitCode;
            });
        });

    /// <summary>
    /// TR-MCP-NUKE-012: the target's run. Resolves the requested paths first (a missing path fails before pwsh starts), removes a result
    /// left by an earlier run so it cannot stand in for this one, runs the runner script through <paramref name="run"/>, then fails on an
    /// adverse result and on a nonzero exit.
    /// </summary>
    /// <param name="repositoryRoot">The repository root; the paths resolve against it and pwsh starts in it.</param>
    /// <param name="runnerPath">The runner script, build/Invoke-PluginCorePester.ps1.</param>
    /// <param name="requested">The requested paths, relative to the root; empty means the plugin core's pester folder.</param>
    /// <param name="resultPath">The NUnit XML result path.</param>
    /// <param name="run">Starts the pwsh process and returns its exit code once it has exited.</param>
    internal static void RunPluginCorePester(
        string repositoryRoot,
        string runnerPath,
        IReadOnlyList<string> requested,
        string resultPath,
        Func<ProcessStartInfo, int> run)
    {
        var paths = ResolvePesterPaths(repositoryRoot, requested);
        if (File.Exists(resultPath))
            File.Delete(resultPath);

        var startInfo = new ProcessStartInfo("pwsh") { UseShellExecute = false, WorkingDirectory = repositoryRoot };
        foreach (var argument in ComposePesterRunnerArguments(runnerPath, resultPath, paths))
            startInfo.ArgumentList.Add(argument);
        var exitCode = run(startInfo);

        FailIfPesterResultAdverse(resultPath);
        if (exitCode != 0)
            throw new InvalidOperationException($"TestPluginCore: Pester exited {exitCode} although {resultPath} records a clean run.");
    }

    /// <summary>
    /// TR-MCP-NUKE-012 AC-004: resolves the requested Pester paths against the repository root, failing before any test runs when one
    /// does not exist; with no request, the plugin core's pester folder.
    /// </summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="requested">The requested paths, relative to the root.</param>
    /// <returns>The full paths to run.</returns>
    internal static IReadOnlyList<string> ResolvePesterPaths(string repositoryRoot, IReadOnlyList<string> requested)
    {
        if (requested.Count == 0)
            return [Path.Combine(repositoryRoot, PluginCorePesterFolder)];

        return requested.Select(path =>
        {
            var full = Path.GetFullPath(Path.Combine(repositoryRoot, path));
            return File.Exists(full) || Directory.Exists(full)
                ? full
                : throw new InvalidOperationException($"TestPluginCore: --pester-paths {path} does not exist.");
        }).ToArray();
    }

    /// <summary>
    /// TR-MCP-NUKE-012 AC-002, AC-003: fails unless the NUnit result records at least one test and no failure, error, skip, ignored,
    /// not-run, inconclusive or invalid test; a missing result file fails too.
    /// </summary>
    /// <param name="resultPath">The NUnit XML result Pester wrote.</param>
    internal static void FailIfPesterResultAdverse(string resultPath)
    {
        if (!File.Exists(resultPath))
            throw new InvalidOperationException($"TestPluginCore: Pester wrote no result at {resultPath}.");

        var root = XDocument.Load(resultPath).Root
            ?? throw new InvalidOperationException($"TestPluginCore: {resultPath} has no root element.");
        int Counter(string name) => int.Parse((string?)root.Attribute(name) ?? "0", CultureInfo.InvariantCulture);
        var adverse = AdversePesterCounters.Where(name => Counter(name) > 0).Select(name => $"{name}={Counter(name)}").ToArray();
        if (Counter("total") == 0 || adverse.Length > 0)
        {
            throw new InvalidOperationException(
                $"TestPluginCore: Pester run is not clean (total={Counter("total")}{(adverse.Length > 0 ? ", " + string.Join(", ", adverse) : string.Empty)}).");
        }
    }

    /// <summary>TR-MCP-NUKE-012 AC-001: the pwsh arguments that run the runner script on the resolved paths.</summary>
    /// <param name="runnerPath">The runner script.</param>
    /// <param name="resultPath">The NUnit XML result path.</param>
    /// <param name="paths">The resolved Pester paths.</param>
    /// <returns>The argument list.</returns>
    internal static IReadOnlyList<string> ComposePesterRunnerArguments(string runnerPath, string resultPath, IReadOnlyList<string> paths)
        => ["-NoProfile", "-NonInteractive", "-File", runnerPath, "-ResultPath", resultPath, .. paths];
}
