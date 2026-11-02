using Xunit.Sdk;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-020 AC-1 to AC-5 (BDPv4 mock gate): independent fixed inputs and expected runs for the TestGroups
/// composition and its per-class result check. Each group names one test project and one or more classes; groups for
/// one project merge into one run with one TRX; the filter is the lane's host platform clause and class clause, with no
/// category clause, so cases tagged for the other platform never run (AC-4); malformed, unknown and empty groups and a
/// missing or unsafe run id are refused before any test runs; a named class that ran no case fails (AC-5). The scripted
/// references answer from these rows, and wrong compositions and checkers are rejected by the same assertions inside
/// this passing harness. No checkout, process or environment is read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1, AC-2, AC-3: added the composition and run-id contracts (mock gate ug-r9-mockgate-q-1).</description></item>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-4, AC-5: host-aware rows and the per-class check rows (mock gate ug-r9-mockgate-q-2).</description></item>
/// </list>
/// </remarks>
public sealed class TestGroupExecutionContractTests
{
    /// <summary>Solution projects whose names differ from their file stems, to expose accidental file-name lookups.</summary>
    internal static readonly (string Name, string Path)[] Projects =
    [
        ("QBrainAi.Repl.Core.Tests", "repo/tests/a/core-file.csproj"),
        ("QBrainAi.Support.Mcp.Tests", "repo/tests/b/support-file.csproj"),
        ("Build.Tests", "repo/tests/c/build-file.csproj"),
    ];

    /// <summary>One composed project run: the runner input and its matching log line and TRX name.</summary>
    /// <param name="Name">The solution project name.</param>
    /// <param name="Path">The solution's project file path.</param>
    /// <param name="Filter">The exact filter passed to the test runner.</param>
    /// <param name="LogLine">The exact filter announcement.</param>
    /// <param name="TrxName">The expected TRX file name.</param>
    internal sealed record Step(string Name, string Path, string Filter, string LogLine, string TrxName);

    /// <summary>A fixed group list and either its expected project selections or a refusal message fragment.</summary>
    /// <param name="Name">The row name.</param>
    /// <param name="Groups">The --test-groups values, in order.</param>
    /// <param name="Expected">The expected projects and their classes, in run order, or null when the input is refused.</param>
    /// <param name="Refusal">A fragment the refusal message must hold, or null when the input is accepted.</param>
    internal sealed record Row(string Name, string[] Groups, (string Project, string[] Classes)[]? Expected, string? Refusal);

    /// <summary>Every composition row, accepted and refused; the same rows hold on both hosts.</summary>
    internal static readonly Row[] Rows =
    [
        new("one class", ["QBrainAi.Repl.Core.Tests:OwnedInstanceProviderSettingContractTests"],
            [("QBrainAi.Repl.Core.Tests", ["OwnedInstanceProviderSettingContractTests"])], null),
        new("two classes", ["QBrainAi.Support.Mcp.Tests:WorkspacePathGetFullPathRatchetTests,GitHubCliInvocationPlanContractTests"],
            [("QBrainAi.Support.Mcp.Tests", ["WorkspacePathGetFullPathRatchetTests", "GitHubCliInvocationPlanContractTests"])], null),
        new("two projects keep group order", ["Build.Tests:TestGroupExecutionContractTests", "QBrainAi.Repl.Core.Tests:Alpha"],
            [("Build.Tests", ["TestGroupExecutionContractTests"]), ("QBrainAi.Repl.Core.Tests", ["Alpha"])], null),
        new("one project's groups merge", ["QBrainAi.Repl.Core.Tests:Alpha", "QBrainAi.Support.Mcp.Tests:Gamma", "QBrainAi.Repl.Core.Tests:Beta,Alpha"],
            [("QBrainAi.Repl.Core.Tests", ["Alpha", "Beta"]), ("QBrainAi.Support.Mcp.Tests", ["Gamma"])], null),
        new("spaces trimmed", [" QBrainAi.Repl.Core.Tests : Alpha , Beta "],
            [("QBrainAi.Repl.Core.Tests", ["Alpha", "Beta"])], null),
        new("no groups", [], null, "at least one"),
        new("unknown project", ["QBrainAi.Nope.Tests:Alpha"], null, "QBrainAi.Nope.Tests"),
        new("file stem is not a project name", ["core-file:Alpha"], null, "core-file"),
        new("missing separator", ["QBrainAi.Repl.Core.Tests"], null, "QBrainAi.Repl.Core.Tests"),
        new("blank project", [":Alpha"], null, "project"),
        new("no classes", ["QBrainAi.Repl.Core.Tests:"], null, "class"),
        new("empty class between commas", ["QBrainAi.Repl.Core.Tests:Alpha,,Beta"], null, "class"),
        new("dotted class name", ["QBrainAi.Repl.Core.Tests:Ns.Alpha"], null, "Ns.Alpha"),
        new("filter syntax in a class name", ["QBrainAi.Repl.Core.Tests:Alpha|Beta"], null, "Alpha|Beta"),
    ];

    /// <summary>Raw --test-run-id values and the run id TestGroups accepts, or null when it must refuse.</summary>
    internal static readonly (string Raw, string? Expected)[] RunIdRows =
    [
        ("", null),
        ("   ", null),
        ("ug-r9-binding-q-1", "ug-r9-binding-q-1"),
        ("a/b", null),
        ("a\\b", null),
        ("..", null),
        ("c:d", null),
    ];

    /// <summary>TR-MCP-NUKE-020 AC-5: named classes, a project's TRX, and the class whose absence must be reported, or
    /// null when every named class ran at least one case. A class counts only when a result's test name holds it as a
    /// whole dot-bounded segment, as the filter selects it.</summary>
    internal static readonly (string Name, string[] Classes, string Xml, string? Missing)[] ClassRows =
    [
        ("every class ran", ["Alpha", "Beta"], Trx("Ns.Alpha.Method", "Ns.Beta.Method(value: 1)"), null),
        ("one class ran nothing", ["Alpha", "Beta"], Trx("Ns.Alpha.Method"), "Beta"),
        ("a longer class name is not the class", ["Alpha"], Trx("Ns.AlphaBeta.Method"), "Alpha"),
        ("no results at all", ["Alpha"], Trx(), "Alpha"),
        ("unreadable TRX", ["Alpha"], "<TestRun>", "Alpha"),
    ];

    /// <summary>The scripted reference satisfies every composition row on both hosts, with the project list in either order.</summary>
    [Fact]
    public void ScriptedComposition_SatisfiesContract() => AssertComposition(ScriptedComposition);

    /// <summary>Each wrong composition fails the same assertions: dropped groups, shared TRX names, a leaked lane
    /// category clause, a missing or swapped platform clause, file-stem names, accepted refusals, unmerged groups, a log
    /// line without its filter and project order instead of group order.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("dropped group")]
    [InlineData("shared trx")]
    [InlineData("lane category clause leaks")]
    [InlineData("no platform clause")]
    [InlineData("other host's platform clause")]
    [InlineData("file stem name")]
    [InlineData("accepts refused input")]
    [InlineData("unmerged project groups")]
    [InlineData("log without filter")]
    [InlineData("project order instead of group order")]
    public void WrongCompositions_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertComposition((groups, projects, host) => WrongComposition(fault, groups, projects, host)));

    /// <summary>The scripted run-id policy satisfies every row; accepting a blank id, inventing one, or keeping an
    /// unsafe path segment is rejected.</summary>
    [Fact]
    public void ScriptedRunId_SatisfiesContractAndRejectsWrongPolicies()
    {
        AssertRunId(ScriptedRunId);
        Assert.ThrowsAny<XunitException>(() => AssertRunId(raw => string.IsNullOrWhiteSpace(raw) ? raw : ScriptedRunId(raw)));
        Assert.ThrowsAny<XunitException>(() => AssertRunId(raw => string.IsNullOrWhiteSpace(raw) ? "generated-run-id" : ScriptedRunId(raw)));
        Assert.ThrowsAny<XunitException>(() => AssertRunId(raw => raw.Contains('/', StringComparison.Ordinal) ? raw : ScriptedRunId(raw)));
    }

    /// <summary>TR-MCP-NUKE-020 AC-5: the scripted class check satisfies every row; a checker that accepts everything
    /// or that matches a class by prefix is rejected.</summary>
    [Fact]
    public void ScriptedClassCheck_SatisfiesContractAndRejectsWrongCheckers()
    {
        AssertClassCheck(ScriptedClassCheck);
        Assert.ThrowsAny<XunitException>(() => AssertClassCheck((_, _) => null));
        Assert.ThrowsAny<XunitException>(() => AssertClassCheck((classes, xml) =>
        {
            var missing = classes.Where(name => !xml.Contains("." + name, StringComparison.Ordinal)).ToArray();
            return missing.Length == 0 ? null : "missing " + string.Join(", ", missing);
        }));
    }

    /// <summary>Asserts every row against a composer on both hosts, with the solution's project list in both orders:
    /// accepted rows produce exactly the expected runs (one TRX name per project, the host's platform clause first),
    /// refused rows throw before any run with the row's message fragment.</summary>
    /// <param name="compose">The composer under test.</param>
    internal static void AssertComposition(Func<IReadOnlyList<string>, IReadOnlyList<(string Name, string Path)>, IntegrationLaneHost, IReadOnlyList<Step>> compose)
    {
        foreach (var host in new[] { IntegrationLaneHost.Linux, IntegrationLaneHost.Windows })
        {
            foreach (var projects in new[] { Projects, Projects.Reverse().ToArray() })
            {
                foreach (var row in Rows)
                {
                    if (row.Refusal is not null)
                    {
                        var refusal = Assert.Throws<InvalidOperationException>(() => compose(row.Groups, projects, host));
                        Assert.Contains(row.Refusal, refusal.Message, StringComparison.Ordinal);
                        continue;
                    }

                    var expected = row.Expected!.Select(selection => Run(selection.Project, host, selection.Classes)).ToArray();
                    var actual = compose(row.Groups, projects, host);
                    Assert.Equal(expected.Length, actual.Count);
                    Assert.Equal(actual.Count, actual.Select(step => step.TrxName).Distinct(StringComparer.Ordinal).Count());
                    for (var index = 0; index < actual.Count; index++)
                        Assert.Equal(expected[index], actual[index]);
                }
            }
        }
    }

    /// <summary>Asserts the run-id policy: safe ids pass through unchanged; blank and unsafe ids are refused with a
    /// message that names --test-run-id.</summary>
    /// <param name="require">The policy under test.</param>
    internal static void AssertRunId(Func<string, string> require)
    {
        foreach (var row in RunIdRows)
        {
            if (row.Expected is null)
            {
                var refusal = Assert.Throws<InvalidOperationException>(() => require(row.Raw));
                Assert.Contains("--test-run-id", refusal.Message, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(row.Expected, require(row.Raw));
            }
        }
    }

    /// <summary>TR-MCP-NUKE-020 AC-5: asserts a class check: null when every named class ran a case, otherwise a
    /// failure message that names the missing class.</summary>
    /// <param name="check">The checker under test: named classes and TRX content in, null or a failure out.</param>
    internal static void AssertClassCheck(Func<IReadOnlyList<string>, string, string?> check)
    {
        foreach (var row in ClassRows)
        {
            var result = check(row.Classes, row.Xml);
            if (row.Missing is null)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Contains(row.Missing, result, StringComparison.Ordinal);
            }
        }
    }

    private static string PlatformClause(IntegrationLaneHost host) => host == IntegrationLaneHost.Linux ? "Platform!=Windows" : "Platform!=Linux";

    private static Step Run(string project, IntegrationLaneHost host, string[] classes)
    {
        var path = Projects.Single(entry => entry.Name == project).Path;
        var filter = PlatformClause(host) + "&(" + string.Join("|", classes.Select(name => "FullyQualifiedName~." + name + ".")) + ")";
        return new Step(project, path, filter, "Test group filter for " + project + ": " + filter, project + ".trx");
    }

    private static IReadOnlyList<Step> ScriptedComposition(IReadOnlyList<string> groups, IReadOnlyList<(string Name, string Path)> projects, IntegrationLaneHost host)
    {
        var row = Rows.Single(candidate => candidate.Groups.SequenceEqual(groups));
        if (row.Refusal is not null)
            throw new InvalidOperationException("scripted refusal: " + row.Refusal);
        return row.Expected!.Select(selection => Run(selection.Project, host, selection.Classes)
            with { Path = projects.Single(project => project.Name == selection.Project).Path }).ToArray();
    }

    private static IReadOnlyList<Step> WrongComposition(string fault, IReadOnlyList<string> groups, IReadOnlyList<(string Name, string Path)> projects, IntegrationLaneHost host)
    {
        var row = Rows.Single(candidate => candidate.Groups.SequenceEqual(groups));
        if (row.Refusal is not null)
            return fault == "accepts refused input" ? [] : ScriptedComposition(groups, projects, host);

        var steps = ScriptedComposition(groups, projects, host);
        var other = host == IntegrationLaneHost.Linux ? IntegrationLaneHost.Windows : IntegrationLaneHost.Linux;
        return fault switch
        {
            "dropped group" => steps.Take(1).ToArray(),
            "shared trx" => steps.Select(step => step with { TrxName = "tests.trx" }).ToArray(),
            "lane category clause leaks" => steps.Select(step => step with { Filter = "Category=Integration&" + step.Filter }).ToArray(),
            "no platform clause" => steps.Select(step => step with { Filter = step.Filter[(PlatformClause(host).Length + 1)..] }).ToArray(),
            "other host's platform clause" => steps.Select(step => step with { Filter = PlatformClause(other) + step.Filter[PlatformClause(host).Length..] }).ToArray(),
            "file stem name" => steps.Select(step => step with { Name = System.IO.Path.GetFileNameWithoutExtension(step.Path) }).ToArray(),
            "accepts refused input" => steps,
            "unmerged project groups" => groups.Count == 3 ? [.. steps, steps[0]] : steps,
            "log without filter" => steps.Select(step => step with { LogLine = "Test group filter for " + step.Name }).ToArray(),
            "project order instead of group order" => steps.OrderBy(step => Array.FindIndex(Projects, project => project.Name == step.Name)).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        };
    }

    private static string ScriptedRunId(string raw) =>
        RunIdRows.Single(row => row.Raw == raw).Expected ?? throw new InvalidOperationException("scripted refusal of --test-run-id '" + raw + "'");

    private static string? ScriptedClassCheck(IReadOnlyList<string> classes, string xml)
    {
        var row = ClassRows.Single(candidate => candidate.Xml == xml && candidate.Classes.SequenceEqual(classes));
        return row.Missing is null ? null : "scripted: class " + row.Missing + " ran no case";
    }

    private static string Trx(params string[] testNames) =>
        "<TestRun xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010'><Results>"
        + string.Concat(testNames.Select(name => "<UnitTestResult testName='" + name + "' outcome='Passed' />"))
        + "</Results></TestRun>";
}
