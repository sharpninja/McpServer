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
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-5 (appended: a class counts only through an executed result joined to its definition by test id): the class rows now carry TRX test definitions, with rows for a definition without an executed result, a display name that mentions the class, a namespace segment that names it, a custom display name and a not-executed result, and wrong checkers for the earlier production check, display names only and definitions only, because hostile validation finding R9-F1 showed the check accepted a class that ran no case; every helper documented (MEMORY-FACT-008; finding R9-F8) (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
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
    /// null when every named class ran at least one case. A class counts only when an executed result's test, joined to
    /// its definition by test id, belongs to that class (the definition's class name is the class, as the whole name or
    /// its last segment); a definition without an executed result, a display name and a namespace segment never count.</summary>
    internal static readonly (string Name, string[] Classes, string Xml, string? Missing)[] ClassRows =
    [
        ("every class ran", ["Alpha", "Beta"], Trx(("Ns.Alpha", "Ns.Alpha.Method", "Passed"), ("Ns.Beta", "Ns.Beta.Method(value: 1)", "Passed")), null),
        ("one class ran nothing", ["Alpha", "Beta"], Trx(("Ns.Alpha", "Ns.Alpha.Method", "Passed")), "Beta"),
        ("a longer class name is not the class", ["Alpha"], Trx(("Ns.AlphaBeta", "Ns.AlphaBeta.Method", "Passed")), "Alpha"),
        ("no results at all", ["Alpha"], Trx(), "Alpha"),
        ("unreadable TRX", ["Alpha"], "<TestRun>", "Alpha"),
        ("a definition without an executed result does not count", ["Alpha"], Trx(("Ns.Alpha", "Ns.Alpha.Method", null), ("Ns.Beta", "Ns.Beta.Method", "Passed")), "Alpha"),
        ("a display name that mentions the class does not count", ["Alpha"], Trx(("Ns.Beta", "Ns.Beta.Method(path: \"x.Alpha.y\")", "Passed")), "Alpha"),
        ("a namespace segment is not the declaring class", ["Alpha"], Trx(("Ns.Alpha.Tests.Beta", "Ns.Alpha.Tests.Beta.Method", "Passed")), "Alpha"),
        ("a custom display name counts through its test's class", ["Alpha"], Trx(("Ns.Alpha", "Readable custom name", "Passed")), null),
        ("a not-executed result does not count", ["Alpha"], Trx(("Ns.Alpha", "Ns.Alpha.Method", "NotExecuted")), "Alpha"),
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

    /// <summary>TR-MCP-NUKE-020 AC-5: the scripted class check satisfies every row; checkers that accept everything,
    /// match a class by prefix, take the union of display names and definitions without a join (the earlier production
    /// check), read display names only, or read definitions only are rejected.</summary>
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
        Assert.ThrowsAny<XunitException>(() => AssertClassCheck((classes, xml) => TextMatchCheck(classes, xml, displayNames: true, definitions: true)));
        Assert.ThrowsAny<XunitException>(() => AssertClassCheck((classes, xml) => TextMatchCheck(classes, xml, displayNames: true, definitions: false)));
        Assert.ThrowsAny<XunitException>(() => AssertClassCheck((classes, xml) => TextMatchCheck(classes, xml, displayNames: false, definitions: true)));
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

    /// <summary>TR-MCP-NUKE-020 AC-4 scripted reference: the host platform clause the lane uses (TR-MCP-NUKE-019 AC-1).</summary>
    /// <param name="host">The host.</param>
    /// <returns>The platform clause.</returns>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-4: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static string PlatformClause(IntegrationLaneHost host) => host == IntegrationLaneHost.Linux ? "Platform!=Windows" : "Platform!=Linux";

    /// <summary>TR-MCP-NUKE-020 AC-1, AC-2 scripted reference: the run a project's merged groups must produce.</summary>
    /// <param name="project">The project name.</param>
    /// <param name="host">The host.</param>
    /// <param name="classes">The project's named classes, in group order.</param>
    /// <returns>The expected run.</returns>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1, AC-2: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static Step Run(string project, IntegrationLaneHost host, string[] classes)
    {
        var path = Projects.Single(entry => entry.Name == project).Path;
        var filter = PlatformClause(host) + "&(" + string.Join("|", classes.Select(name => "FullyQualifiedName~." + name + ".")) + ")";
        return new Step(project, path, filter, "Test group filter for " + project + ": " + filter, project + ".trx");
    }

    /// <summary>TR-MCP-NUKE-020 AC-1 to AC-4 scripted reference: the runs each composition row states, or its refusal.</summary>
    /// <param name="groups">The --test-groups values.</param>
    /// <param name="projects">The solution's projects.</param>
    /// <param name="host">The host.</param>
    /// <returns>The expected runs.</returns>
    /// <exception cref="InvalidOperationException">The row states a refusal.</exception>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1 to AC-4: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static IReadOnlyList<Step> ScriptedComposition(IReadOnlyList<string> groups, IReadOnlyList<(string Name, string Path)> projects, IntegrationLaneHost host)
    {
        var row = Rows.Single(candidate => candidate.Groups.SequenceEqual(groups));
        if (row.Refusal is not null)
            throw new InvalidOperationException("scripted refusal: " + row.Refusal);
        return row.Expected!.Select(selection => Run(selection.Project, host, selection.Classes)
            with { Path = projects.Single(project => project.Name == selection.Project).Path }).ToArray();
    }

    /// <summary>TR-MCP-NUKE-020 AC-1 to AC-4 negative controls: the scripted runs with one deliberate fault.</summary>
    /// <param name="fault">The fault.</param>
    /// <param name="groups">The --test-groups values.</param>
    /// <param name="projects">The solution's projects.</param>
    /// <param name="host">The host.</param>
    /// <returns>The faulty runs.</returns>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1 to AC-4: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
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

    /// <summary>TR-MCP-NUKE-020 AC-1 scripted reference: the run id each row states, or its refusal.</summary>
    /// <param name="raw">The --test-run-id value.</param>
    /// <returns>The run id.</returns>
    /// <exception cref="InvalidOperationException">The row states a refusal.</exception>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static string ScriptedRunId(string raw) =>
        RunIdRows.Single(row => row.Raw == raw).Expected ?? throw new InvalidOperationException("scripted refusal of --test-run-id '" + raw + "'");

    /// <summary>TR-MCP-NUKE-020 AC-5 scripted reference: the missing class each class row states, looked up from the rows.</summary>
    /// <param name="classes">The named classes.</param>
    /// <param name="xml">The TRX content.</param>
    /// <returns>Null when the row states every class ran; otherwise a failure naming the missing class.</returns>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-5: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static string? ScriptedClassCheck(IReadOnlyList<string> classes, string xml)
    {
        var row = ClassRows.Single(candidate => candidate.Xml == xml && candidate.Classes.SequenceEqual(classes));
        return row.Missing is null ? null : "scripted: class " + row.Missing + " ran no case";
    }

    /// <summary>
    /// TR-MCP-NUKE-020 AC-5 negative controls: a class check that matches the class as a dot-bounded segment of text,
    /// in result display names, in definition class names, or in both without joining a result to its definition. With
    /// both it is the earlier production check.
    /// </summary>
    /// <param name="classes">The named classes.</param>
    /// <param name="xml">The TRX content.</param>
    /// <param name="displayNames">Whether result display names count.</param>
    /// <param name="definitions">Whether definition class names count.</param>
    /// <returns>Null when every class matched; otherwise a failure naming the classes that did not.</returns>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-5: added for hostile validation finding R9-F1 (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</remarks>
    private static string? TextMatchCheck(IReadOnlyList<string> classes, string xml, bool displayNames, bool definitions)
    {
        string[] texts;
        try
        {
            var document = System.Xml.Linq.XDocument.Parse(xml);
            texts = document.Descendants().Where(element => displayNames && element.Name.LocalName == "UnitTestResult")
                .Select(element => (string?)element.Attribute("testName") ?? string.Empty)
                .Concat(document.Descendants().Where(element => definitions && element.Name.LocalName == "TestMethod")
                    .Select(element => ((string?)element.Attribute("className") ?? string.Empty) + "."))
                .ToArray();
        }
        catch (System.Xml.XmlException)
        {
            return "unreadable";
        }

        var missing = classes.Where(name => !texts.Any(text => text.Contains("." + name + ".", StringComparison.Ordinal))).ToArray();
        return missing.Length == 0 ? null : "missing " + string.Join(", ", missing);
    }

    /// <summary>
    /// TR-MCP-NUKE-020 AC-5: a TRX with one test definition per entry (id, display name and a TestMethod with the class
    /// name) and one result per entry whose outcome is given, joined to its definition by test id; an entry with no
    /// outcome has a definition and no result.
    /// </summary>
    /// <param name="tests">Each test's class name, display name and result outcome (null for no result).</param>
    /// <returns>The TRX content.</returns>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-5: now writes definitions joined by test id, for hostile validation finding R9-F1; documented (MEMORY-FACT-008).</remarks>
    private static string Trx(params (string ClassName, string DisplayName, string? Outcome)[] tests) =>
        "<TestRun xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010'><Results>"
        + string.Concat(tests.Select((test, index) => test.Outcome is null
            ? string.Empty
            : "<UnitTestResult testId='t" + index + "' testName='" + System.Security.SecurityElement.Escape(test.DisplayName) + "' outcome='" + test.Outcome + "' />"))
        + "</Results><TestDefinitions>"
        + string.Concat(tests.Select((test, index) =>
            "<UnitTest id='t" + index + "' name='" + System.Security.SecurityElement.Escape(test.DisplayName) + "'><TestMethod className='" + test.ClassName + "' name='Method' /></UnitTest>"))
        + "</TestDefinitions></TestRun>";
}
