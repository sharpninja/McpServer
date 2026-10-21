using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests that agent-plugin version resolution reads only the sources it is given (<see cref="AgentPluginVersionSources"/>):
/// the plugin root an environment variable names, then the plugin folder beside the workspace, then the plugin caches
/// under the user profile, then the synced version; and that the sources the unit-test assembly installs are hermetic
/// (no plugin-root variable, no sibling folder, an empty temporary profile).
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-WS-005 / AC-TR-MCP-WS-005-001 (unit tests take every plugin-version input from
/// test-owned data). Found by hostile validation round 3 (finding R3-F1: the assembly fixture replaced only the
/// profile, so a plugin-root variable reached 19 unit tests); fix chosen by the operator on 2026-10-20 ("Structural
/// fix, round 4 (Recommended)"). Test data: a temporary root per test holding test-owned plugin folders with fixed
/// versions (environment root 4.0.0, sibling root 3.0.0, profile cache 2.0.0) and a stand-in for the machine (a
/// plugin-root variable at 9.0.0, the folder beside the workspace at 8.0.0, a profile cache at 7.0.0); no real
/// environment variable, machine folder or profile is read. Expected versions follow the criterion's reading order.
/// Negative controls run deliberately wrong resolutions that read one machine input instead of the given source, and
/// hermeticity controls reject sources that name a plugin root, compute a sibling, or point at a populated or
/// non-temporary profile.
/// </remarks>
public sealed class AgentPluginVersionSourcesTests : IDisposable
{
    private const string Variable = "CLAUDE_PLUGIN_ROOT";

    private static readonly string[] PluginRootVariables =
        ["CODEX_PLUGIN_ROOT", "CLAUDE_PLUGIN_ROOT", "COPILOT_PLUGIN_ROOT", "CLINE_PLUGIN_ROOT", "GROK_PLUGIN_ROOT"];

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"mcp-plugin-sources-{Guid.NewGuid():N}");

    private readonly string _plugin = $"mcpserver-sources-test-{Guid.NewGuid():N}";

    /// <summary>A resolution under test: resolves a plugin version for a workspace from the given sources.</summary>
    private delegate string ResolveFunction(string workspacePath, string pluginName, string variable, AgentPluginVersionSources sources);

    /// <summary>Creates the test-owned plugin folders and the machine stand-in under a temporary root.</summary>
    public AgentPluginVersionSourcesTests()
    {
        Directory.CreateDirectory(Workspace);
        WriteVersion(EnvironmentRoot, "4.0.0");
        WriteVersion(GivenSibling, "3.0.0");
        WriteVersion(Path.Combine(GivenProfile, ".claude", "plugins", "cache", _plugin, "2.0.0"), "2.0.0");
        Directory.CreateDirectory(EmptyProfile);
        WriteVersion(MachineEnvironmentRoot, "9.0.0");
        WriteVersion(Path.Combine(_root, _plugin), "8.0.0");
        WriteVersion(Path.Combine(MachineProfile, ".claude", "plugins", "cache", _plugin, "7.0.0"), "7.0.0");
    }

    /// <summary>Deletes the temporary root.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private string Workspace => Path.Combine(_root, "workspace");

    private string EnvironmentRoot => Path.Combine(_root, "given-environment-root");

    private string GivenSibling => Path.Combine(_root, "elsewhere", _plugin);

    private string GivenProfile => Path.Combine(_root, "given-profile");

    private string EmptyProfile => Path.Combine(_root, "empty-profile");

    private string MachineEnvironmentRoot => Path.Combine(_root, "machine-environment-root");

    private string MachineProfile => Path.Combine(_root, "machine-profile");

    /// <summary>The stand-in for the machine: what a resolution that ignores its sources would read instead.</summary>
    private AgentPluginVersionSources Machine => new(
        name => name == Variable ? MachineEnvironmentRoot : null,
        (workspace, plugin) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(workspace))!, plugin),
        MachineProfile);

    private static void WriteVersion(string directory, string version)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, ".version"), version);
    }

    /// <summary>The scripted cases: the given sources and the version the criterion requires.</summary>
    private (AgentPluginVersionSources Sources, string Version) Case(string name) => name switch
    {
        "environment-root-first" => (new(v => v == Variable ? EnvironmentRoot : null, (_, _) => GivenSibling, GivenProfile), "4.0.0"),
        "sibling-root-next" => (new(_ => null, (_, _) => GivenSibling, GivenProfile), "3.0.0"),
        "profile-cache-next" => (new(_ => null, (_, _) => null, GivenProfile), "2.0.0"),
        "nothing-falls-back" => (new(_ => null, (_, _) => null, EmptyProfile), MarkerFileService.SyncedAgentPluginVersion),
        "no-profile" => (new(_ => null, (_, _) => null, null), MarkerFileService.SyncedAgentPluginVersion),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown case."),
    };

    /// <summary>The scripted case names, for the theories.</summary>
    public static TheoryData<string> CaseNames =>
        new() { "environment-root-first", "sibling-root-next", "profile-cache-next", "nothing-falls-back", "no-profile" };

    /// <summary>Asserts one resolution result against the version a case requires.</summary>
    private static void AssertVersion(string expected, string actual) => Assert.Equal(expected, actual);

    private void RunAndAssert(string name, ResolveFunction resolve)
    {
        var (sources, version) = Case(name);
        AssertVersion(version, resolve(Workspace, _plugin, Variable, sources));
    }

    /// <summary>
    /// A model of resolution that reads the plugin roots in the criterion's order from the sources it is handed:
    /// the environment root, the sibling root, then the Claude plugin cache versions under the profile, newest first.
    /// </summary>
    private static string ModelResolve(string workspacePath, string pluginName, string variable, AgentPluginVersionSources sources)
    {
        foreach (var root in Roots())
        {
            var version = MarkerFileService.TryReadAgentPluginVersion(root, MarkerFileService.ReadExistingFileText);
            if (!string.IsNullOrWhiteSpace(version))
                return version;
        }

        return MarkerFileService.SyncedAgentPluginVersion;

        IEnumerable<string> Roots()
        {
            if (sources.EnvironmentVariable(variable) is { Length: > 0 } environmentRoot)
                yield return environmentRoot;
            if (sources.SiblingRoot(workspacePath, pluginName) is { Length: > 0 } siblingRoot)
                yield return siblingRoot;
            if (!string.IsNullOrWhiteSpace(sources.UserProfile))
            {
                var cache = Path.Combine(sources.UserProfile, ".claude", "plugins", "cache", pluginName);
                if (Directory.Exists(cache))
                {
                    foreach (var candidate in Directory.EnumerateDirectories(cache).Order(StringComparer.Ordinal).Reverse())
                        yield return candidate;
                }
            }
        }
    }

    /// <summary>Mock gate positive control: each case's required version passes the version assertion.</summary>
    /// <param name="name">Scripted case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_RequiredVersion_PassesTheVersionAssertion(string name) =>
        AssertVersion(Case(name).Version, Case(name).Version);

    /// <summary>Mock gate positive control: the model that reads only its given sources yields every required version.</summary>
    /// <param name="name">Scripted case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_SourceOnlyModel_PassesTheVersionAssertion(string name) =>
        RunAndAssert(name, ModelResolve);

    /// <summary>
    /// Production binding: <c>MarkerFileService.ResolveAgentPluginVersion</c> with the file-system walk reads only the
    /// sources it is given and yields every required version, while the machine stand-in holds a
    /// different version at each input.
    /// </summary>
    /// <param name="name">Scripted case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_ProductionResolution_YieldsTheRequiredVersion(string name) =>
        RunAndAssert(name, (w, p, v, s) => MarkerFileService.ResolveAgentPluginVersion(w, p, v, s, DirectoryTreeWalker.FileSystem));

    /// <summary>
    /// Production binding: the marker's agent-plugin contract takes every version from the sources it is given: the
    /// agent whose variable resolves gets that root's version, and every other agent gets the given sibling's version.
    /// </summary>
    [Fact]
    public void BuildDefaultAgentPlugins_GivenSources_ResolvesEveryAgentFromThem()
    {
        var (sources, _) = Case("environment-root-first");

        var plugins = MarkerFileService.BuildDefaultAgentPlugins(Workspace, sources);

        Assert.Equal(5, plugins.Agents.Count);
        Assert.Equal("4.0.0", plugins.Agents["Claude"].PluginVersion);
        Assert.All(plugins.Agents.Where(pair => pair.Key != "Claude"), pair => Assert.Equal("3.0.0", pair.Value.PluginVersion));
    }

    /// <summary>
    /// Production binding: the sources the unit-test assembly installs, which every default marker build in this
    /// assembly reads, pass the hermeticity assertion.
    /// </summary>
    [Fact]
    public void AssemblySources_AreHermetic()
    {
        var live = MarkerFileService.AgentPluginVersionSourcesOverride;

        Assert.NotNull(live);
        AssertHermetic(live);
    }

    /// <summary>
    /// Mock gate negative controls: a resolution that reads the machine's plugin-root variable, one that computes the
    /// folder beside the workspace, one that scans the machine's profile, and one that ignores every given source are
    /// each rejected by the version assertion on the case that exposes it, inside a passing harness.
    /// </summary>
    [Fact]
    public void MachineReadingNegativeControls_AreRejectedInsidePassingHarness()
    {
        var machine = Machine;
        ResolveFunction readsMachineEnvironment = (w, p, v, s) => ModelResolve(w, p, v, s with { EnvironmentVariable = machine.EnvironmentVariable });
        ResolveFunction computesSibling = (w, p, v, s) => ModelResolve(w, p, v, s with { SiblingRoot = machine.SiblingRoot });
        ResolveFunction readsMachineProfile = (w, p, v, s) => ModelResolve(w, p, v, s with { UserProfile = machine.UserProfile });
        ResolveFunction ignoresSources = (w, p, v, _) => ModelResolve(w, p, v, machine);

        Assert.NotNull(Record.Exception(() => RunAndAssert("environment-root-first", readsMachineEnvironment)));
        Assert.NotNull(Record.Exception(() => RunAndAssert("profile-cache-next", computesSibling)));
        Assert.NotNull(Record.Exception(() => RunAndAssert("nothing-falls-back", readsMachineProfile)));
        Assert.NotNull(Record.Exception(() => RunAndAssert("no-profile", readsMachineProfile)));
        foreach (var name in new[] { "environment-root-first", "sibling-root-next", "profile-cache-next", "nothing-falls-back", "no-profile" })
            Assert.NotNull(Record.Exception(() => RunAndAssert(name, ignoresSources)));

        // Each wrong resolution is wrong only where its machine input is reached: when an earlier given source already
        // yields a version, it returns the required one, so the rejections above come from the input it reads.
        Assert.Null(Record.Exception(() => RunAndAssert("environment-root-first", computesSibling)));
        Assert.Null(Record.Exception(() => RunAndAssert("environment-root-first", readsMachineProfile)));
    }

    /// <summary>
    /// Asserts that sources are hermetic for unit tests: no plugin-root variable (nor any other name) resolves, no
    /// sibling folder is offered for any workspace, and the profile is an existing, empty directory under the
    /// system temporary folder.
    /// </summary>
    internal static void AssertHermetic(AgentPluginVersionSources sources)
    {
        foreach (var name in PluginRootVariables.Append("ANY_OTHER_VARIABLE"))
            Assert.Null(sources.EnvironmentVariable(name));
        Assert.Null(sources.SiblingRoot(Path.Combine(Path.GetTempPath(), "workspace"), "mcpserver-claude-code-plugin"));
        Assert.Null(sources.SiblingRoot(@"C:\test", "mcpserver-codex-plugin"));
        Assert.False(string.IsNullOrWhiteSpace(sources.UserProfile));
        Assert.True(Directory.Exists(sources.UserProfile));
        Assert.Empty(Directory.EnumerateFileSystemEntries(sources.UserProfile!));
        Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), Path.GetFullPath(sources.UserProfile!), StringComparison.Ordinal);
    }

    /// <summary>Mock gate positive control: scripted hermetic sources pass the hermeticity assertion.</summary>
    [Fact]
    public void ScriptedHermeticSources_PassTheHermeticityAssertion() =>
        AssertHermetic(new AgentPluginVersionSources(_ => null, (_, _) => null, EmptyProfile));

    /// <summary>
    /// Mock gate negative controls: sources that resolve a plugin-root variable, compute the folder beside the
    /// workspace, point at a populated profile, or point at no profile are each rejected by the hermeticity
    /// assertion, inside a passing harness.
    /// </summary>
    [Fact]
    public void LeakySourcesNegativeControls_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertHermetic(new AgentPluginVersionSources(Machine.EnvironmentVariable, (_, _) => null, EmptyProfile))));
        Assert.NotNull(Record.Exception(() => AssertHermetic(new AgentPluginVersionSources(_ => null, Machine.SiblingRoot, EmptyProfile))));
        Assert.NotNull(Record.Exception(() => AssertHermetic(new AgentPluginVersionSources(_ => null, (_, _) => null, GivenProfile))));
        Assert.NotNull(Record.Exception(() => AssertHermetic(new AgentPluginVersionSources(_ => null, (_, _) => null, null))));
    }
}
