// SPDX-License-Identifier: Apache-2.0
namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-012 (FR-MCP-BUILD-001; defined by PLAN-SESSIONTURN-SESSIONREVIEW-001 P0.1a, built under PLAN-SESSIONTRIAGEFIX-001 by
/// operator decision 2026-10-14 "Build TestPluginCore first"): TestPluginCore --pester-paths runs the plugin core's Pester suites through
/// Nuke. Build-infrastructure test, accountable through TR only, run directly with dotnet test. The mock gate tests prove each assertion
/// against reference functions and reject faulty ones before the target's functions exist; the binding tests, named per the TR, bind the
/// same assertions to the target's functions and run the target's run (<c>Build.RunPluginCorePester</c>) with the real runner script
/// build/Invoke-PluginCorePester.ps1 through <see cref="BoundedProcess"/> (TR-MCP-NUKE-009). Data: temporary repository roots with
/// fixture Pester files; NUnit XML documents in the format Pester 5.9.1 writes (pinned 2026-10-14 by a probe of one passing, one failing
/// and one skipped It: root test-results with total, errors, failures="1", not-run, inconclusive, ignored, skipped="1" and invalid).
/// </summary>
public sealed class TestPluginCoreTargetTests
{
    internal const string PesterFolder = "plugins/core/test-fixtures/pester";

    /// <summary>
    /// Mock gate for AC-004: the path assertion accepts a resolver that refuses a missing path naming it, resolves an existing one,
    /// and defaults to the pester folder; it rejects a resolver that passes a missing path through.
    /// </summary>
    [Fact]
    public void PathAssertion_AcceptsReferenceResolverAndRejectsOneThatPassesMissingPaths()
    {
        AssertPathResolution(ReferenceResolve);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertPathResolution((root, requested) =>
            requested.Count == 0 ? [Path.Combine(root, PesterFolder)] : requested.Select(p => Path.GetFullPath(Path.Combine(root, p))).ToArray()));
    }

    /// <summary>
    /// Mock gate for AC-002 and AC-003: the result assertion accepts an evaluator that fails on failures, errors, skips, not-run,
    /// inconclusive, ignored, a zero total or a missing file and passes a clean run; it rejects an evaluator that ignores skips.
    /// </summary>
    [Fact]
    public void ResultAssertion_AcceptsReferenceEvaluatorAndRejectsOneThatIgnoresSkips()
    {
        AssertResultEvaluation(ReferenceEvaluate);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertResultEvaluation(path =>
        {
            var counters = ReadCounters(path);
            if (counters["failures"] + counters["errors"] > 0 || counters["total"] == 0)
                throw new InvalidOperationException("Pester reported failures.");
        }));
    }

    /// <summary>
    /// Mock gate for AC-001: the argument assertion accepts a composer that runs the runner script with -NoProfile -NonInteractive -File,
    /// the result path, and every named path last; it rejects a composer that drops the second path.
    /// </summary>
    [Fact]
    public void ArgumentAssertion_AcceptsReferenceComposerAndRejectsOneThatDropsAPath()
    {
        AssertRunnerArguments(ReferenceCompose);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertRunnerArguments((runner, result, paths) => ReferenceCompose(runner, result, paths.Take(1).ToArray())));
    }

    /// <summary>
    /// AC-001 binding: <c>Build.ComposePesterRunnerArguments</c> passes the argument assertion, and the target's run with the real runner
    /// script runs Pester 5 in the -CI configuration on exactly the named files. Data: passing fixture files A, B and C, with A and B
    /// named. Expected: exit 0, a clean result counting 2 tests, and a runner that sets the two settings Pester 5.9.1 applies for -CI
    /// (Pester.psm1:5090-5095: Run.Exit and TestResult.Enabled).
    /// </summary>
    [Fact]
    public void TestPluginCoreRunsPesterCiOnNamedPaths()
    {
        AssertRunnerArguments(Build.ComposePesterRunnerArguments);
        using var fixture = new PesterFixture();
        foreach (var name in new[] { "A", "B", "C" })
            fixture.WriteTests(name, "It 'passes' { 1 | Should -Be 1 }");

        fixture.Run("A", "B");

        Assert.True(fixture.ExitCode == 0, fixture.Output);
        Assert.Equal(2, ReadCounters(fixture.ResultPath)["total"]);
        var runner = File.ReadAllText(RunnerPath);
        Assert.Contains("$config.Run.Exit = $true", runner, StringComparison.Ordinal);
        Assert.Contains("$config.TestResult.Enabled = $true", runner, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-002 binding: <c>Build.FailIfPesterResultAdverse</c> passes the result assertion; the target's run fails on a real failing test
    /// (failures=1, as the probe pinned for one failing It) while Pester itself exits nonzero (Run.Exit); a nonzero exit fails the run
    /// even when the result reads clean; and a clean result left by an earlier run does not stand in for a run that writes none.
    /// Data: fixture A passes and F fails (1 | Should -Be 2); scripted runs that return 1 with and without writing a clean result.
    /// </summary>
    [Fact]
    public void TestPluginCorePesterFailureFailsTarget()
    {
        AssertResultEvaluation(Build.FailIfPesterResultAdverse);
        using var fixture = new PesterFixture();
        fixture.WriteTests("A", "It 'passes' { 1 | Should -Be 1 }");
        fixture.WriteTests("F", "It 'fails' { 1 | Should -Be 2 }");

        var failure = Assert.Throws<InvalidOperationException>(() => fixture.Run("A", "F"));

        Assert.Contains("failures=1", failure.Message, StringComparison.Ordinal);
        Assert.True(fixture.ExitCode is not null and not 0, fixture.Output);

        var clean = WriteResult(fixture.Root, "earlier-clean", total: 3);
        IReadOnlyList<string> named = [PesterFolder + "/A.Tests.ps1"];
        var exitOnly = Assert.Throws<InvalidOperationException>(() => Build.RunPluginCorePester(fixture.Root, RunnerPath, named, fixture.ResultPath, _ =>
        {
            File.Copy(clean, fixture.ResultPath, overwrite: true);
            return 1;
        }));
        Assert.Contains("exited 1", exitOnly.Message, StringComparison.Ordinal);

        File.Copy(clean, fixture.ResultPath, overwrite: true);
        var stale = Assert.Throws<InvalidOperationException>(() => Build.RunPluginCorePester(fixture.Root, RunnerPath, named, fixture.ResultPath, _ => 1));
        Assert.Contains("wrote no result", stale.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-003 binding: the target's run fails on a real skipped test (skipped=1, as the probe pinned for one skipped It) although Pester
    /// exits 0 on a skip, so only the result file catches it. Data: fixture A passes and S is skipped (It -Skip).
    /// </summary>
    [Fact]
    public void TestPluginCorePesterSkipFailsTarget()
    {
        using var fixture = new PesterFixture();
        fixture.WriteTests("A", "It 'passes' { 1 | Should -Be 1 }");
        fixture.WriteTests("S", "It 'skipped' -Skip { 1 | Should -Be 1 }");

        var skip = Assert.Throws<InvalidOperationException>(() => fixture.Run("A", "S"));

        Assert.Contains("skipped=1", skip.Message, StringComparison.Ordinal);
        Assert.True(fixture.ExitCode == 0, fixture.Output);
    }

    /// <summary>
    /// AC-004 binding: <c>Build.ResolvePesterPaths</c> passes the path assertion, and the target's run refuses a request naming a missing
    /// file before pwsh starts. Data: fixture A exists; Nope.Tests.ps1 does not; a scripted run that records whether it was started.
    /// </summary>
    [Fact]
    public void TestPluginCoreMissingPathFailsTarget()
    {
        AssertPathResolution(Build.ResolvePesterPaths);
        using var fixture = new PesterFixture();
        fixture.WriteTests("A", "It 'passes' { 1 | Should -Be 1 }");
        var started = false;

        var missing = Assert.Throws<InvalidOperationException>(() => Build.RunPluginCorePester(
            fixture.Root,
            RunnerPath,
            [PesterFolder + "/A.Tests.ps1", PesterFolder + "/Nope.Tests.ps1"],
            fixture.ResultPath,
            _ =>
            {
                started = true;
                return 0;
            }));

        Assert.Contains("Nope.Tests.ps1", missing.Message, StringComparison.Ordinal);
        Assert.False(started, "pwsh started although a requested path is missing.");
    }

    /// <summary>
    /// AC-005 binding: the target's run works natively on the operating system the suite runs on, Windows or Linux, with no request
    /// (the whole pester folder). The AC's evidence is this test passing on Linux (LAB-OMARCHY) and on Windows (PAYTON-LEGION2).
    /// Data: passing fixture files A and B in the pester folder.
    /// </summary>
    [Fact]
    public void TestPluginCoreRunsNatively()
    {
        Assert.True(OperatingSystem.IsWindows() || OperatingSystem.IsLinux(), $"Unsupported platform {System.Runtime.InteropServices.RuntimeInformation.OSDescription}.");
        using var fixture = new PesterFixture();
        fixture.WriteTests("A", "It 'passes' { 1 | Should -Be 1 }");
        fixture.WriteTests("B", "It 'passes' { 1 | Should -Be 1 }");

        fixture.Run();

        Assert.True(fixture.ExitCode == 0, fixture.Output);
        Assert.Equal(2, ReadCounters(fixture.ResultPath)["total"]);
    }

    /// <summary>AC-004: a missing path fails naming it; an existing path resolves to its full path; no path means the pester folder.</summary>
    internal static void AssertPathResolution(Func<string, IReadOnlyList<string>, IReadOnlyList<string>> resolve)
    {
        var root = Path.Combine(Path.GetTempPath(), "nuke012-" + Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(root, PesterFolder);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "A.Tests.ps1"), "Describe 'A' { It 'a' { 1 | Should -Be 1 } }");
        try
        {
            var missing = Assert.Throws<InvalidOperationException>(() => resolve(root, [PesterFolder + "/Nope.Tests.ps1"]));
            Assert.Contains("Nope.Tests.ps1", missing.Message, StringComparison.Ordinal);
            Assert.Equal(new[] { Path.Combine(folder, "A.Tests.ps1") }, resolve(root, [PesterFolder + "/A.Tests.ps1"]));
            Assert.Equal(new[] { folder }, resolve(root, []));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>AC-002, AC-003: a clean run passes; any failure, error, skip, ignored, not-run, inconclusive, zero total or missing file fails.</summary>
    internal static void AssertResultEvaluation(Action<string> evaluate)
    {
        var dir = Path.Combine(Path.GetTempPath(), "nuke012-xml-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            evaluate(WriteResult(dir, "clean", total: 3));
            foreach (var adverse in new[] { "failures", "errors", "skipped", "ignored", "not-run", "inconclusive" })
                Assert.Throws<InvalidOperationException>(() => evaluate(WriteResult(dir, adverse, total: 3, adverse)));
            Assert.Throws<InvalidOperationException>(() => evaluate(WriteResult(dir, "empty", total: 0)));
            Assert.Throws<InvalidOperationException>(() => evaluate(Path.Combine(dir, "absent.xml")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>AC-001: pwsh runs the runner script non-interactively with the result path, then every named path.</summary>
    internal static void AssertRunnerArguments(Func<string, string, IReadOnlyList<string>, IReadOnlyList<string>> compose)
    {
        var args = compose("/r/build/Invoke-PluginCorePester.ps1", "/r/TestResults/x/pester/results.xml", ["/r/a.Tests.ps1", "/r/b.Tests.ps1"]);
        Assert.Equal(
            new[] { "-NoProfile", "-NonInteractive", "-File", "/r/build/Invoke-PluginCorePester.ps1", "-ResultPath", "/r/TestResults/x/pester/results.xml", "/r/a.Tests.ps1", "/r/b.Tests.ps1" },
            args);
    }

    /// <summary>Writes an NUnit result document in the Pester 5.9.1 format with every counter zero except total and the adverse one.</summary>
    internal static string WriteResult(string dir, string name, int total, string? adverse = null)
    {
        var counters = new[] { "errors", "failures", "not-run", "inconclusive", "ignored", "skipped", "invalid" }
            .Select(c => $"{c}=\"{(c == adverse ? 1 : 0)}\"");
        var path = Path.Combine(dir, name + ".xml");
        File.WriteAllText(path, $"<?xml version=\"1.0\" encoding=\"utf-8\"?><test-results name=\"Pester\" total=\"{total}\" {string.Join(' ', counters)} date=\"2026-10-14\" time=\"10:37:17\" />");
        return path;
    }

    private static Dictionary<string, int> ReadCounters(string path)
    {
        var root = System.Xml.Linq.XDocument.Load(path).Root!;
        return new[] { "total", "errors", "failures", "not-run", "inconclusive", "ignored", "skipped", "invalid" }
            .ToDictionary(n => n, n => int.Parse((string?)root.Attribute(n) ?? "0", System.Globalization.CultureInfo.InvariantCulture));
    }

    private static IReadOnlyList<string> ReferenceResolve(string root, IReadOnlyList<string> requested)
    {
        if (requested.Count == 0)
            return [Path.Combine(root, PesterFolder)];
        return requested.Select(p =>
        {
            var full = Path.GetFullPath(Path.Combine(root, p));
            return File.Exists(full) ? full : throw new InvalidOperationException($"--pester-paths {p} does not exist.");
        }).ToArray();
    }

    private static void ReferenceEvaluate(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException("Pester result file missing.");
        var c = ReadCounters(path);
        if (c["total"] == 0 || c.Where(kv => kv.Key != "total").Any(kv => kv.Value > 0))
            throw new InvalidOperationException("Pester run not clean.");
    }

    private static IReadOnlyList<string> ReferenceCompose(string runner, string result, IReadOnlyList<string> paths)
        => ["-NoProfile", "-NonInteractive", "-File", runner, "-ResultPath", result, .. paths];

    private static string RunnerPath => Path.Combine(FindRepositoryRoot(), "build", "Invoke-PluginCorePester.ps1");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing QBrainAi.sln.");
    }

    /// <summary>
    /// A temporary repository root with fixture Pester files under plugins/core/test-fixtures/pester, run through the target's run with
    /// the real runner script and <see cref="BoundedProcess"/>; records the exit code and output of the last pwsh run. Deleted on dispose.
    /// </summary>
    private sealed class PesterFixture : IDisposable
    {
        internal PesterFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "nuke012-run-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(Root, PesterFolder));
            ResultPath = Path.Combine(Root, "TestResults", "pester", "results.xml");
        }

        internal string Root { get; }

        internal string ResultPath { get; }

        internal int? ExitCode { get; private set; }

        internal string Output { get; private set; } = string.Empty;

        internal void WriteTests(string name, string body) =>
            File.WriteAllText(Path.Combine(Root, PesterFolder, name + ".Tests.ps1"), $"Describe '{name}' {{ {body} }}");

        internal void Run(params string[] names) =>
            Build.RunPluginCorePester(Root, RunnerPath, names.Select(name => $"{PesterFolder}/{name}.Tests.ps1").ToArray(), ResultPath, startInfo =>
            {
                var result = BoundedProcess.Run(startInfo, TimeSpan.FromMinutes(5), CancellationToken.None);
                ExitCode = result.ExitCode;
                Output = result.Stdout + Environment.NewLine + result.Stderr;
                return result.ExitCode;
            });

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
