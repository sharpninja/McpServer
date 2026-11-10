using Nuke.Common.Tooling;

/// <summary>
/// TR-MCP-NUKE-019 AC-7: supplies runtime-discovered test case names for one project and optional trait filter.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: added so Test and
/// MigrationIntegrationTests derive ownership inventories from their projects instead of recorded counts.
/// </remarks>
internal interface ITestCaseDiscoverySource
{
    /// <summary>Discovers the test case display names selected by <paramref name="filter"/>.</summary>
    /// <param name="projectPath">The test project file.</param>
    /// <param name="filter">The optional VSTest trait filter; null discovers every case.</param>
    /// <returns>The discovered display names in runner order.</returns>
    IReadOnlyList<string> Discover(string projectPath, string? filter);
}

/// <summary>
/// TR-MCP-NUKE-019 AC-7: composes a real ownership inventory from unfiltered, integration-filtered and
/// quarantine-filtered runtime discovery results.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: replaces the stale recorded-count binding with
/// case identities discovered from each selected project at target run time.
/// </remarks>
internal static class TestProjectCaseDiscovery
{
    /// <summary>Discovers and classifies one project's cases for the ownership decision.</summary>
    /// <param name="projectName">The declared project name.</param>
    /// <param name="projectPath">The test project file.</param>
    /// <param name="role">The role declared by <see cref="TestProjectRoles"/>.</param>
    /// <param name="source">The runtime discovery boundary.</param>
    /// <returns>The project role and every discovered case identity, kind and quarantine state.</returns>
    /// <exception cref="InvalidOperationException">A discovery result is empty, duplicated or not a subset of all cases.</exception>
    internal static TestProjectInventory Discover(
        string projectName,
        string projectPath,
        TestProjectRole role,
        ITestCaseDiscoverySource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentNullException.ThrowIfNull(source);

        var all = Unique(projectName, "all", source.Discover(projectPath, null));
        if (all.Count == 0)
            throw new InvalidOperationException($"Runtime discovery found no test case in '{projectName}'.");

        var integration = Unique(projectName, "Category=Integration", source.Discover(projectPath, "Category=Integration"));
        var quarantined = Unique(projectName, TestQuarantine.Trait + "=" + TestQuarantine.NoMatchingCriterion,
            source.Discover(projectPath, TestQuarantine.Trait + "=" + TestQuarantine.NoMatchingCriterion));
        RequireSubset(projectName, integration, all, "integration");
        RequireSubset(projectName, quarantined, all, "quarantined");

        var integrationSet = integration.ToHashSet(StringComparer.Ordinal);
        var quarantineSet = quarantined.ToHashSet(StringComparer.Ordinal);
        var cases = all.Select(name => new TestCaseEntry(
            name,
            integrationSet.Contains(name) ? TestCaseKind.Integration : NonIntegrationKind(role),
            quarantineSet.Contains(name))).ToArray();
        return new TestProjectInventory(projectName, role, cases);
    }

    /// <summary>Returns the non-integration kind that exposes a missing Integration trait as a foreign case.</summary>
    /// <param name="role">The declared project role.</param>
    /// <returns>Infrastructure for infrastructure projects; unit otherwise.</returns>
    private static TestCaseKind NonIntegrationKind(TestProjectRole role)
        => role == TestProjectRole.Infrastructure ? TestCaseKind.Infrastructure : TestCaseKind.Unit;

    /// <summary>Rejects duplicate discovered identities without changing runner order.</summary>
    /// <param name="projectName">The project being discovered.</param>
    /// <param name="selection">The discovery filter label.</param>
    /// <param name="cases">The runner result.</param>
    /// <returns>The copied identities.</returns>
    private static IReadOnlyList<string> Unique(string projectName, string selection, IReadOnlyList<string> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);
        var duplicates = cases.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() > 1)
            .Select(group => group.Key).ToArray();
        if (duplicates.Length > 0)
            throw new InvalidOperationException($"Runtime discovery for '{projectName}' ({selection}) repeated: {string.Join(", ", duplicates)}.");
        return cases.ToArray();
    }

    /// <summary>Rejects filtered identities that the same project's unfiltered discovery did not return.</summary>
    /// <param name="projectName">The project being discovered.</param>
    /// <param name="filtered">The filtered discovery result.</param>
    /// <param name="all">The unfiltered discovery result.</param>
    /// <param name="selection">The filtered result label.</param>
    private static void RequireSubset(string projectName, IReadOnlyList<string> filtered, IReadOnlyList<string> all, string selection)
    {
        var unknown = filtered.Except(all, StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
            throw new InvalidOperationException($"Runtime {selection} discovery for '{projectName}' returned identities absent from all-case discovery: {string.Join(", ", unknown)}.");
    }
}

/// <summary>
/// TR-MCP-NUKE-019 AC-7: fails a build selection unless runtime-discovered ownership accepts every selected project.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: added to make role/category drift a target failure.
/// </remarks>
internal static class TestProjectOwnershipGate
{
    /// <summary>Throws when any selected project has another role or a foreign discovered case.</summary>
    /// <param name="selection">The target name.</param>
    /// <param name="outcome">The ownership decision over runtime-discovered inventories.</param>
    /// <exception cref="InvalidOperationException">One or more selected projects are rejected.</exception>
    internal static void RequireAccepted(string selection, TestOwnershipOutcome outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selection);
        ArgumentNullException.ThrowIfNull(outcome);
        var rejected = outcome.Projects.Where(item => item.Verdict != TestProjectVerdict.Accepted)
            .Select(item => $"{item.Project} ({item.Verdict})").ToArray();
        if (rejected.Length > 0)
            throw new InvalidOperationException($"{selection} runtime ownership discovery rejected: {string.Join(", ", rejected)}.");
    }
}

/// <summary>
/// TR-MCP-NUKE-019 AC-7: discovers real case identities with <c>dotnet test --list-tests</c> and VSTest filters.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: added as the real target boundary after the scripted
/// discovery contract. Discovery executes no test case. A project that Compile did not build may build on its first
/// discovery; every later discovery uses --no-build and --no-restore.
/// </remarks>
internal sealed class DotNetListTestDiscoverySource(
    string configuration,
    Func<string, bool>? buildOnFirstDiscovery = null) : ITestCaseDiscoverySource
{
    private readonly HashSet<string> builtByDiscovery = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<string> Discover(string projectPath, string? filter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        var fullPath = Path.GetFullPath(projectPath);
        var mustBuild = buildOnFirstDiscovery?.Invoke(fullPath) == true && !builtByDiscovery.Contains(fullPath);
        var arguments = $"test \"{fullPath.Replace("\"", "\\\"")}\" --configuration \"{configuration}\" --list-tests --verbosity minimal";
        if (!mustBuild)
            arguments += " --no-build --no-restore";
        if (!string.IsNullOrWhiteSpace(filter))
            arguments += $" --filter \"{filter.Replace("\"", "\\\"")}\"";

        var process = ProcessTasks.StartProcess("dotnet", arguments, logOutput: false, logInvocation: true);
        process.WaitForExit();
        process.AssertZeroExitCode();
        if (mustBuild)
            builtByDiscovery.Add(fullPath);
        return Parse(process.Output.Select(output => output.Text).ToArray());
    }

    /// <summary>Parses the indented case identities after the VSTest list header.</summary>
    /// <param name="lines">The complete process output.</param>
    /// <returns>The discovered case identities; empty when the filter selects none.</returns>
    /// <exception cref="InvalidOperationException">The successful process output has no list header.</exception>
    internal static IReadOnlyList<string> Parse(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var header = -1;
        for (var index = 0; index < lines.Count; index++)
        {
            if (string.Equals(lines[index].Trim(), "The following Tests are available:", StringComparison.Ordinal))
            {
                header = index;
                break;
            }
        }
        if (header < 0)
            throw new InvalidOperationException("dotnet list-tests output did not contain the test-list header.");

        var cases = new List<string>();
        for (var index = header + 1; index < lines.Count; index++)
        {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
                continue;
            if (trimmed.StartsWith("No test matches the given testcase filter", StringComparison.Ordinal))
                return [];
            if (raw.Length == trimmed.Length)
                break;
            cases.Add(trimmed);
        }
        return cases;
    }
}
