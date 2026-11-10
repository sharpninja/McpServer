using Xunit.Sdk;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-2/AC-3 and the retained precondition in plan section 11: slice J's independent inputs for one lane composition,
/// nonempty passing TRX results, pre-provisioning refusal and a Linux daemon with no inherited caller pipes.
/// Mock controls use literal expected responses before production binding; no checkout, process or environment is read.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: every lane project's filter and logged filter line carry the quarantine exclusion Quarantine!=NoMatchingCriterion after the host clause (before the class clause where one applies); a composition that omits it from the filter, or only from the logged line, is rejected (mock gate ug-qr-mockgate-1, before the production composition changes).</remarks>
public sealed class IntegrationLaneExecutionContractTests
{
    /// <summary>The project names deliberately differ from their file stems to expose accidental filename lookups.</summary>
    internal static readonly (string Name, string Path)[] Projects = IntegrationLaneProjectsContract.LaneProjects
        .Select((name, index) => (name, $"repo/tests/project-{index}/different-file-name.csproj")).ToArray();

    /// <summary>A composed project carries the same identity to the runner, log, filter and TRX filename.</summary>
    internal sealed record Step(string Name, string Path, string Filter, string LogLine, string TrxName);

    /// <summary>The Linux worker's launch data must not retain any stdin, stdout or stderr pipe from its caller.</summary>
    internal sealed record Launch(string FileName, string[] Arguments, bool Shell, bool Input, bool Output, bool Error);

    /// <summary>All TRX outcomes are explicit fixtures; missing, empty, skipped, failed and malformed evidence fails.</summary>
    internal static readonly (string Name, string Xml, bool Accepted)[] TrxRows =
    [
        ("one pass", Trx(1, 1, 0, 0), true),
        ("many passes", Trx(17, 17, 0, 0), true),
        ("empty filter", Trx(0, 0, 0, 0), false),
        ("a failure", Trx(2, 1, 1, 0), false),
        ("a skip", Trx(2, 1, 0, 1), false),
        ("missing counters", "<TestRun><ResultSummary /></TestRun>", false),
        ("malformed XML", "<TestRun>", false),
        ("inconsistent counters", Trx(2, 1, 0, 0), false),
    ];

    /// <summary>The scripted composition satisfies both host filters and preserves project identity throughout.</summary>
    [Fact]
    public void ScriptedComposition_SatisfiesContract() => AssertComposition(ScriptedComposition);

    /// <summary>
    /// Wrong project keys, filters, log lines and TRX names are independently rejected; TR-MCP-NUKE-019 AC-8: so are a
    /// filter and log line without the quarantine exclusion, and a log line that leaves the exclusion out of a filter
    /// that has it.
    /// </summary>
    /// <param name="fault">The deliberate fault.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: adds the two quarantine faults.</remarks>
    [Theory]
    [InlineData("file stem")]
    [InlineData("filter")]
    [InlineData("log")]
    [InlineData("trx")]
    [InlineData("quarantine exclusion omitted")]
    [InlineData("log omits the quarantine exclusion")]
    public void WrongCompositions_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertComposition((projects, host) => ScriptedComposition(projects, host).Select(step => fault switch
        {
            "file stem" => step with { Name = Path.GetFileNameWithoutExtension(step.Path) },
            "filter" => step with { Filter = "Category=Integration" },
            "log" => step with { LogLine = "wrong filter" },
            "trx" => step with { TrxName = "different-file-name.trx" },
            "quarantine exclusion omitted" => step with
            {
                Filter = WithoutQuarantineExclusion(step.Filter),
                LogLine = $"Integration lane filter for {step.Name}: {WithoutQuarantineExclusion(step.Filter)}",
            },
            "log omits the quarantine exclusion" => step with { LogLine = $"Integration lane filter for {step.Name}: {WithoutQuarantineExclusion(step.Filter)}" },
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        }).ToArray()));

    /// <summary>Every TRX row exercises the shared acceptance assertion, including a zero-match runner success.</summary>
    [Fact]
    public void ScriptedTrxValidation_SatisfiesContractAndRejectsEveryWrongVerdict()
    {
        AssertTrx(ScriptedTrx);
        foreach (var target in TrxRows)
            Assert.ThrowsAny<XunitException>(() => AssertTrx(xml => xml == target.Xml ? !target.Accepted : ScriptedTrx(xml)));
    }

    /// <summary>Mock gate: the worker plan isolates all three standard handles and writes through its own log script.</summary>
    [Fact]
    public void ScriptedLinuxWorker_SatisfiesContractAndRejectsInheritedPipes()
    {
        AssertLaunch(ScriptedLaunch);
        Assert.ThrowsAny<XunitException>(() => AssertLaunch(() => ScriptedLaunch() with { Output = false }));
        Assert.ThrowsAny<XunitException>(() => AssertLaunch(() => ScriptedLaunch() with { Error = false }));
        Assert.ThrowsAny<XunitException>(() => AssertLaunch(() => ScriptedLaunch() with { Input = false }));
        Assert.ThrowsAny<XunitException>(() => AssertLaunch(() => ScriptedLaunch() with { Shell = true }));
        Assert.ThrowsAny<XunitException>(() => AssertLaunch(() => ScriptedLaunch() with { Arguments = ["serve"] }));
    }

    /// <summary>Mock gate: only a lane invocation requires the interactive Windows session, before provisioning runs.</summary>
    [Fact]
    public void ScriptedInvocationPolicy_SatisfiesContractAndRejectsLateRefusal()
    {
        AssertInvocationPolicy((lane, host, interactive, provision) =>
        {
            if (lane && host == IntegrationLaneHost.Windows && !interactive) throw new InvalidOperationException("interactive session");
            provision();
        });
        Assert.ThrowsAny<XunitException>(() => AssertInvocationPolicy((lane, host, interactive, provision) =>
        {
            provision();
            if (lane && host == IntegrationLaneHost.Windows && !interactive) throw new InvalidOperationException("interactive session");
        }));
    }

    /// <summary>Mock gate: a host outside Linux/Windows and a non-x64 portable asset request must name the unsupported input.</summary>
    [Fact]
    public void ScriptedPlatformChecks_SatisfyContractAndRejectWrongAssumptions()
    {
        AssertPlatforms((windows, linux) => windows ? "Windows" : linux ? "Linux" : "unsupported host",
            architecture => architecture == "X64" ? null : "Unsupported architecture: " + architecture);
        Assert.ThrowsAny<XunitException>(() => AssertPlatforms((windows, _) => windows ? "Windows" : "Linux", _ => null));
    }

    /// <summary>BLD-9: a selector contract requires ClassesOf explicitly; an implicit default body is rejected.</summary>
    [Fact]
    public void ScriptedSelectorInterface_RequiresExplicitClassesMethod()
    {
        AssertExplicitClasses(typeof(ExplicitSelectorContract));
        Assert.ThrowsAny<XunitException>(() => AssertExplicitClasses(typeof(WrongDefaultSelectorContract)));
    }

    /// <summary>Checks the actual interface method's abstract flag without reading source text.</summary>
    internal static void AssertExplicitClasses(Type contract) => Assert.True(contract.GetMethod("ClassesOf")?.IsAbstract == true);

    /// <summary>
    /// Asserts the same complete project rows against a supplied lane composer on both supported hosts. TR-MCP-NUKE-019
    /// AC-8: every project's filter and logged filter line hold the quarantine exclusion.
    /// </summary>
    /// <param name="compose">The composer under test.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: the expected filters carry the quarantine exclusion after the host clause, and the filter and the log line are each checked to hold it.</remarks>
    internal static void AssertComposition(Func<IReadOnlyList<(string Name, string Path)>, IntegrationLaneHost, IReadOnlyList<Step>> compose)
    {
        foreach (var host in new[] { IntegrationLaneHost.Linux, IntegrationLaneHost.Windows })
        {
            foreach (var input in new[] { Projects, Projects.Reverse().ToArray() })
            {
                var result = compose(input, host);
                Assert.Equal(Projects.Select(project => project.Name), result.Select(step => step.Name));
                foreach (var step in result)
                {
                    Assert.Equal(Projects.Single(project => project.Name == step.Name).Path, step.Path);
                    var filter = ExpectedFilter(step.Name, host);
                    Assert.Equal(filter, step.Filter);
                    Assert.Equal($"Integration lane filter for {step.Name}: {filter}", step.LogLine);
                    Assert.Contains("&" + IntegrationLaneFilterContract.QuarantineExclusion, step.Filter, StringComparison.Ordinal);
                    Assert.Contains("&" + IntegrationLaneFilterContract.QuarantineExclusion, step.LogLine, StringComparison.Ordinal);
                    Assert.Equal(step.Name + ".trx", step.TrxName);
                }
            }
        }
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-1, AC-2, AC-8: the filter the lane must pass for one project on one host: the host clause, the
    /// quarantine exclusion, and for QBrainAi.Support.Mcp.IntegrationTests the class clause of its two classes.
    /// </summary>
    /// <param name="project">The lane project.</param>
    /// <param name="host">The host.</param>
    /// <returns>The expected filter.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: extracted from the assertion and the scripted composition so both state the same text, now with the quarantine exclusion.</remarks>
    private static string ExpectedFilter(string project, IntegrationLaneHost host)
    {
        var filter = (host == IntegrationLaneHost.Linux ? "Category=Integration&Platform!=Windows" : "Category=Integration&Platform!=Linux")
            + "&" + IntegrationLaneFilterContract.QuarantineExclusion;
        if (project == "QBrainAi.Support.Mcp.IntegrationTests")
            filter += "&(FullyQualifiedName~.RequirementScopeLayerReplIntegrationTests.|FullyQualifiedName~.QuadBrainOllamaEndpointIntegrationTests.)";
        return filter;
    }

    /// <summary>TR-MCP-NUKE-019 AC-8 negative controls: a filter with its quarantine exclusion removed.</summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The filter without the exclusion.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static string WithoutQuarantineExclusion(string filter) =>
        filter.Replace("&" + IntegrationLaneFilterContract.QuarantineExclusion, string.Empty, StringComparison.Ordinal);

    /// <summary>Validates every actual TRX verdict against its independent fixture expectation.</summary>
    internal static void AssertTrx(Func<string, bool> validate)
    {
        foreach (var row in TrxRows) Assert.True(row.Accepted == validate(row.Xml), row.Name);
    }

    /// <summary>Checks exact argument boundaries, the PowerShell-only worker, the log path and isolated standard handles.</summary>
    internal static void AssertLaunch(Func<Launch> create)
    {
        var plan = create();
        Assert.Equal("setsid", plan.FileName);
        Assert.Equal(new[] { "pwsh.exe", "-NoProfile", "-NonInteractive", "-File", "worker.ps1", "-OllamaPath", "tools/ollama", "-LogPath", "owned/logs/ollama.log" }, plan.Arguments);
        Assert.False(plan.Shell);
        Assert.True(plan.Input);
        Assert.True(plan.Output);
        Assert.True(plan.Error);
    }

    /// <summary>Checks callback ordering with simulated hosts, without depending on the current OS or desktop session.</summary>
    internal static void AssertInvocationPolicy(Action<bool, IntegrationLaneHost, bool, Action> invoke)
    {
        var called = false;
        var refusal = Assert.Throws<InvalidOperationException>(() => invoke(true, IntegrationLaneHost.Windows, false, () => called = true));
        Assert.Contains("interactive", refusal.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(called);
        foreach (var row in new[] { (true, IntegrationLaneHost.Windows, true), (true, IntegrationLaneHost.Linux, false), (false, IntegrationLaneHost.Windows, false) })
        {
            called = false;
            invoke(row.Item1, row.Item2, row.Item3, () => called = true);
            Assert.True(called);
        }
    }

    /// <summary>Checks explicit host and portable-architecture inputs; no ambient platform is read.</summary>
    internal static void AssertPlatforms(Func<bool, bool, string> host, Func<string, string?> portable)
    {
        Assert.Equal("Windows", host(true, false));
        Assert.Equal("Linux", host(false, true));
        Assert.Contains("unsupported", host(false, false), StringComparison.OrdinalIgnoreCase);
        Assert.Null(portable("X64"));
        foreach (var architecture in new[] { "Arm64", "X86", "Other" })
        {
            var refusal = portable(architecture);
            Assert.NotNull(refusal);
            Assert.Contains(architecture, refusal, StringComparison.Ordinal);
        }
    }

    /// <summary>Mock gate scripted composition: the contract's rows, in contract order, with the input's paths.</summary>
    /// <param name="projects">The solution's projects.</param>
    /// <param name="host">The host.</param>
    /// <returns>The expected steps.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: builds each filter with the shared expected-filter helper, so it carries the quarantine exclusion.</remarks>
    private static IReadOnlyList<Step> ScriptedComposition(IReadOnlyList<(string Name, string Path)> projects, IntegrationLaneHost host) =>
        Projects.Select(expected =>
        {
            var project = projects.Single(input => input.Name == expected.Name);
            var filter = ExpectedFilter(project.Name, host);
            return new Step(project.Name, project.Path, filter, $"Integration lane filter for {project.Name}: {filter}", project.Name + ".trx");
        }).ToArray();

    private static bool ScriptedTrx(string xml) => TrxRows.Single(row => row.Xml == xml).Accepted;

    private static Launch ScriptedLaunch() => new("setsid",
        ["pwsh.exe", "-NoProfile", "-NonInteractive", "-File", "worker.ps1", "-OllamaPath", "tools/ollama", "-LogPath", "owned/logs/ollama.log"], false, true, true, true);

    private static string Trx(int total, int passed, int failed, int skipped) =>
        $"<TestRun xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010'><ResultSummary><Counters total='{total}' passed='{passed}' failed='{failed}' notExecuted='{skipped}' /></ResultSummary></TestRun>";

    private interface ExplicitSelectorContract
    {
        IReadOnlyList<string> ClassesOf(string project);
    }

    private interface WrongDefaultSelectorContract
    {
        IReadOnlyList<string> ClassesOf(string project) => [];
    }
}
