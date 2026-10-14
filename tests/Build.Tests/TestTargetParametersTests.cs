using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-011 (FR-MCP-BUILD-001; defined by PLAN-SESSIONTURN-SESSIONREVIEW-001 P0.1a, built under PLAN-SESSIONTRIAGEFIX-001
/// C5 by operator decision 2026-10-13): the Nuke Test target accepts --test-projects and --test-filter. Build-infrastructure test,
/// accountable through TR only, run directly with dotnet test. The mock gate tests prove every assertion against a reference
/// selector, a reference filter composer, and a synthetic Build.Test.cs text, and show each rejects a faulty one; the NukeTest*
/// binding tests (named by the TR) run the same assertions against the real Build.SelectTestProjectNames, Build.ComposeTestFilter,
/// and the repository's build/Build.Test.cs. Data: a fixed list of solution project names; the binding source checks read
/// build/Build.Test.cs.
/// </summary>
public sealed class TestTargetParametersTests
{
    /// <summary>Solution project names used by every selection assertion: unit, integration, validation, plugin and build projects.</summary>
    internal static readonly string[] SolutionProjects =
    [
        "QBrainAi.Support.Mcp",
        "QBrainAi.Support.Mcp.Tests",
        "QBrainAi.Repl.Core.Tests",
        "QBrainAi.Support.Mcp.IntegrationTests",
        "QBrainAi.Repl.IntegrationTests",
        "QBrainAi.SessionLog.Validation",
        "QBrainAi.PluginIntegration.Tests",
        "Build.Tests",
    ];

    /// <summary>The default unit filter the Test target applied before TR-MCP-NUKE-011, pinned for AC-004.</summary>
    internal const string DefaultCategoryFilter = "Category!=AiReview&Category!=Integration";

    /// <summary>
    /// The default Test project selection chain before TR-MCP-NUKE-011 (build/Build.Test.cs:16-25 at c4fff186), with comments removed
    /// and whitespace collapsed, pinned for AC-004.
    /// </summary>
    internal const string DefaultSelectionChain =
        "var testProjects = Solution.GetAllProjects(\"*\") " +
        ".Where(p => p.Name.EndsWith(\".Tests\") || p.Name.EndsWith(\".Validation\")) " +
        ".Where(p => !p.Name.Contains(\"IntegrationTests\")) " +
        ".Where(p => !p.Name.Contains(\"PluginIntegration\")) " +
        ".Where(p => !p.Name.EndsWith(\".Validation\")) " +
        ".Where(p => !p.Name.Contains(\"Review.Tests\")) " +
        ".Where(p => !p.Name.EndsWith(\"Build.Tests\")) " +
        ".ToArray();";

    /// <summary>
    /// Mock gate for AC-001 and AC-002: the four selection assertions accept the reference selector, and each rejects a selector that
    /// breaks its rule: one that returns every test project (RunsOnlyNamed), one limited to the default selection
    /// (SelectsDefaultExcluded), and one that returns the request unchecked (UnknownRefused, BuildTestsRefused).
    /// </summary>
    [Fact]
    public void SelectionAssertions_AcceptReferenceSelectorAndRejectFaultySelectors()
    {
        AssertRunsOnlyNamed(ReferenceSelect);
        AssertSelectsDefaultExcluded(ReferenceSelect);
        AssertUnknownRefused(ReferenceSelect);
        AssertBuildTestsRefused(ReferenceSelect);

        Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, IReadOnlyList<string>> everyTestProject =
            (solution, _) => solution.Where(name => name.EndsWith(".Tests", StringComparison.Ordinal)).ToArray();
        Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, IReadOnlyList<string>> defaultOnly =
            (solution, requested) => ReferenceSelect(solution, requested).Where(name => !name.Contains("Integration", StringComparison.Ordinal) && !name.EndsWith(".Validation", StringComparison.Ordinal)).ToArray();
        Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, IReadOnlyList<string>> uncheckedSelector =
            (_, requested) => requested.ToArray();
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertRunsOnlyNamed(everyTestProject));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertSelectsDefaultExcluded(defaultOnly));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertUnknownRefused(uncheckedSelector));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertBuildTestsRefused(uncheckedSelector));
    }

    /// <summary>
    /// Mock gate for AC-003 and AC-004: the three filter assertions accept the reference composer, and each rejects a composer that
    /// breaks its rule: one that joins without parentheses (FilterCombined), one that keeps Category!=Integration for a
    /// default-excluded project (DropsIntegration), and one that wraps the category filter when no extra filter is given
    /// (NoParametersFilter).
    /// </summary>
    [Fact]
    public void FilterAssertions_AcceptReferenceComposerAndRejectFaultyComposers()
    {
        AssertFilterCombined(ReferenceCompose);
        AssertDefaultExcludedDropsIntegration(ReferenceCompose);
        AssertNoParametersFilterUnchanged(ReferenceCompose);

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertFilterCombined((selected, extra) =>
            string.IsNullOrWhiteSpace(extra) ? ReferenceCompose(selected, extra) : $"{ReferenceCompose(selected, null)}&{extra}"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertDefaultExcludedDropsIntegration((_, extra) => ReferenceCompose(true, extra)));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertNoParametersFilterUnchanged((selected, _) => $"({ReferenceCompose(selected, null)})"));
    }

    /// <summary>
    /// Mock gate for AC-002 ("before any test runs") and AC-004 (default selection unchanged): the two source assertions accept a
    /// reference Build.Test.cs text, and reject a text whose default chain changed and a text that resolves names after the first
    /// test run.
    /// </summary>
    [Fact]
    public void SourceAssertions_AcceptReferenceSourceAndRejectChangedChainOrLateResolution()
    {
        AssertDefaultChainUnchanged(ReferenceSource);
        AssertNamesResolvedBeforeAnyTestRun(ReferenceSource);

        var changedChain = ReferenceSource.Replace(".Where(p => !p.Name.Contains(\"Review.Tests\"))", string.Empty, StringComparison.Ordinal);
        var lateResolution = "DotNetTest(_ => _);" + ReferenceSource;
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertDefaultChainUnchanged(changedChain));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertNamesResolvedBeforeAnyTestRun(lateResolution));
    }

    /// <summary>TR-MCP-NUKE-011 AC-001: --test-projects runs only the named projects (the real Build.SelectTestProjectNames).</summary>
    [Fact]
    public void NukeTestTestProjectsRunsOnlyNamed() => AssertRunsOnlyNamed(Build.SelectTestProjectNames);

    /// <summary>
    /// TR-MCP-NUKE-011 AC-001: a name may select an integration or validation project that Test leaves out by default (the real
    /// Build.SelectTestProjectNames).
    /// </summary>
    [Fact]
    public void NukeTestTestProjectsSelectsDefaultExcludedIntegrationAndValidationProjects() =>
        AssertSelectsDefaultExcluded(Build.SelectTestProjectNames);

    /// <summary>
    /// TR-MCP-NUKE-011 AC-002: an unknown name fails (the real Build.SelectTestProjectNames), and the target resolves names before
    /// its first dotnet test call (the real build/Build.Test.cs).
    /// </summary>
    [Fact]
    public void NukeTestUnknownProjectFailsBeforeRun()
    {
        AssertUnknownRefused(Build.SelectTestProjectNames);
        AssertNamesResolvedBeforeAnyTestRun(ReadBuildTestSource());
    }

    /// <summary>
    /// TR-MCP-NUKE-011 AC-002: a name selecting Build.Tests fails (the real Build.SelectTestProjectNames), before any test runs (the
    /// real build/Build.Test.cs).
    /// </summary>
    [Fact]
    public void NukeTestTestProjectsBuildTestsRefusedBeforeRun()
    {
        AssertBuildTestsRefused(Build.SelectTestProjectNames);
        AssertNamesResolvedBeforeAnyTestRun(ReadBuildTestSource());
    }

    /// <summary>TR-MCP-NUKE-011 AC-003: --test-filter is appended as (category)&amp;(filter) (the real Build.ComposeTestFilter).</summary>
    [Fact]
    public void NukeTestTestFilterCombinedWithCategory() => AssertFilterCombined(Build.ComposeTestFilter);

    /// <summary>
    /// TR-MCP-NUKE-011 AC-003: a project Test leaves out by default runs without Category!=Integration (the real
    /// Build.ComposeTestFilter).
    /// </summary>
    [Fact]
    public void NukeTestDefaultExcludedProjectCategoryFilterDropsIntegrationTerm() =>
        AssertDefaultExcludedDropsIntegration(Build.ComposeTestFilter);

    /// <summary>
    /// TR-MCP-NUKE-011 AC-004: without the parameters the target is unchanged: the default selection chain in the real
    /// build/Build.Test.cs is the pinned pre-change chain, and the real Build.ComposeTestFilter gives a default project the pinned
    /// pre-change filter (TestTarget_ExcludesPluginIntegrationFromUnitInventory stays green beside it).
    /// </summary>
    [Fact]
    public void NukeTestNoParametersUnchanged()
    {
        AssertDefaultChainUnchanged(ReadBuildTestSource());
        AssertNoParametersFilterUnchanged(Build.ComposeTestFilter);
    }

    /// <summary>AC-001: only the named projects run, by single name and by the comma form.</summary>
    internal static void AssertRunsOnlyNamed(Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, IReadOnlyList<string>> select)
    {
        Assert.Equal(new[] { "QBrainAi.Repl.Core.Tests" }, select(SolutionProjects, ["QBrainAi.Repl.Core.Tests"]));
        Assert.Equal(
            new[] { "QBrainAi.Support.Mcp.Tests", "QBrainAi.Repl.Core.Tests" },
            select(SolutionProjects, ["QBrainAi.Support.Mcp.Tests,QBrainAi.Repl.Core.Tests"]));
    }

    /// <summary>AC-001: a name may select an integration or validation project that Test leaves out by default.</summary>
    internal static void AssertSelectsDefaultExcluded(Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, IReadOnlyList<string>> select)
    {
        Assert.Equal(
            new[] { "QBrainAi.Support.Mcp.IntegrationTests", "QBrainAi.SessionLog.Validation", "QBrainAi.PluginIntegration.Tests" },
            select(SolutionProjects, ["QBrainAi.Support.Mcp.IntegrationTests", "QBrainAi.SessionLog.Validation", "QBrainAi.PluginIntegration.Tests"]));
    }

    /// <summary>AC-002: a name that matches no test or validation project, or names nothing, fails with the name in the message.</summary>
    internal static void AssertUnknownRefused(Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, IReadOnlyList<string>> select)
    {
        var unknown = Assert.Throws<InvalidOperationException>(() => select(SolutionProjects, ["QBrainAi.Nope.Tests"]));
        Assert.Contains("QBrainAi.Nope.Tests", unknown.Message, StringComparison.Ordinal);
        var production = Assert.Throws<InvalidOperationException>(() => select(SolutionProjects, ["QBrainAi.Support.Mcp"]));
        Assert.Contains("QBrainAi.Support.Mcp", production.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => select(SolutionProjects, [" , "]));
    }

    /// <summary>AC-002: a name that selects Build.Tests fails, even beside valid names.</summary>
    internal static void AssertBuildTestsRefused(Func<IReadOnlyCollection<string>, IReadOnlyCollection<string>, IReadOnlyList<string>> select)
    {
        var refused = Assert.Throws<InvalidOperationException>(() => select(SolutionProjects, ["QBrainAi.Repl.Core.Tests", "Build.Tests"]));
        Assert.Contains("Build.Tests", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>AC-003: --test-filter is appended as (category filter)&amp;(filter) for default and default-excluded projects.</summary>
    internal static void AssertFilterCombined(Func<bool, string?, string> compose)
    {
        Assert.Equal($"({DefaultCategoryFilter})&(FullyQualifiedName~Probe)", compose(true, "FullyQualifiedName~Probe"));
        Assert.Equal("(Category!=AiReview)&(FullyQualifiedName~Probe|Name=Other)", compose(false, "FullyQualifiedName~Probe|Name=Other"));
    }

    /// <summary>AC-003: a project Test leaves out by default runs without the Category!=Integration term.</summary>
    internal static void AssertDefaultExcludedDropsIntegration(Func<bool, string?, string> compose)
    {
        Assert.Equal("Category!=AiReview", compose(false, null));
        Assert.DoesNotContain("Category!=Integration", compose(false, "Name=X"), StringComparison.Ordinal);
    }

    /// <summary>AC-004: with no --test-filter a default project gets exactly the filter the target applied before.</summary>
    internal static void AssertNoParametersFilterUnchanged(Func<bool, string?, string> compose)
    {
        Assert.Equal(DefaultCategoryFilter, compose(true, null));
        Assert.Equal(DefaultCategoryFilter, compose(true, string.Empty));
        Assert.Equal(DefaultCategoryFilter, compose(true, "   "));
    }

    /// <summary>AC-004: the default selection chain in the Test target source is the pinned pre-change chain.</summary>
    internal static void AssertDefaultChainUnchanged(string buildTestSource)
    {
        const string startMark = "var testProjects = Solution.GetAllProjects";
        var start = buildTestSource.IndexOf(startMark, StringComparison.Ordinal);
        Assert.True(start >= 0, "default selection chain not found");
        var end = buildTestSource.IndexOf(".ToArray();", start, StringComparison.Ordinal);
        Assert.True(end > start, "default selection chain has no ToArray()");
        var chain = buildTestSource.Substring(start, end - start + ".ToArray();".Length);
        var normalized = Regex.Replace(Regex.Replace(chain, @"//[^\n]*", string.Empty), @"\s+", " ").Trim();
        Assert.Equal(DefaultSelectionChain, normalized);
    }

    /// <summary>AC-002: the target resolves --test-projects names before its first dotnet test call.</summary>
    internal static void AssertNamesResolvedBeforeAnyTestRun(string buildTestSource)
    {
        var resolve = buildTestSource.IndexOf("SelectTestProjectNames(", StringComparison.Ordinal);
        var firstRun = buildTestSource.IndexOf("DotNetTest(", StringComparison.Ordinal);
        Assert.True(resolve >= 0, "the target does not call SelectTestProjectNames");
        Assert.True(firstRun > resolve, "a dotnet test call precedes the --test-projects resolution");
    }

    private const string ReferenceSource =
        "var testProjects = Solution.GetAllProjects(\"*\")\n" +
        "    .Where(p => p.Name.EndsWith(\".Tests\") || p.Name.EndsWith(\".Validation\"))\n" +
        "    .Where(p => !p.Name.Contains(\"IntegrationTests\"))\n" +
        "    .Where(p => !p.Name.Contains(\"PluginIntegration\"))\n" +
        "    .Where(p => !p.Name.EndsWith(\".Validation\"))\n" +
        "    .Where(p => !p.Name.Contains(\"Review.Tests\"))\n" +
        "    // a comment inside the chain\n" +
        "    .Where(p => !p.Name.EndsWith(\"Build.Tests\"))\n" +
        "    .ToArray();\n" +
        "var names = SelectTestProjectNames(all, TestProjects);\n" +
        "DotNetTest(_ => _);\n";

    private static IReadOnlyList<string> ReferenceSelect(IReadOnlyCollection<string> solution, IReadOnlyCollection<string> requested)
    {
        var names = requested.SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray();
        if (names.Length == 0)
            throw new InvalidOperationException("--test-projects names no project.");
        var result = new List<string>();
        foreach (var name in names)
        {
            if (name.EndsWith("Build.Tests", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{name} is infrastructure and no Nuke target runs it.");
            var match = solution.FirstOrDefault(project => string.Equals(project, name, StringComparison.OrdinalIgnoreCase)
                && (project.EndsWith(".Tests", StringComparison.Ordinal) || project.EndsWith(".IntegrationTests", StringComparison.Ordinal) || project.EndsWith(".Validation", StringComparison.Ordinal)));
            result.Add(match ?? throw new InvalidOperationException($"{name} matches no test or validation project."));
        }

        return result;
    }

    private static string ReferenceCompose(bool defaultSelected, string? extra)
    {
        var category = defaultSelected ? DefaultCategoryFilter : "Category!=AiReview";
        return string.IsNullOrWhiteSpace(extra) ? category : $"({category})&({extra.Trim()})";
    }

    /// <summary>Reads the repository's build/Build.Test.cs, found by the .nuke/parameters.json file above the test assembly.</summary>
    private static string ReadBuildTestSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".nuke", "parameters.json")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, "build", "Build.Test.cs"));
    }
}
