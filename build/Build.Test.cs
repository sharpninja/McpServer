using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>
    /// TR-MCP-NUKE-011: test or validation projects to run instead of the default unit selection, by name, space- or
    /// comma-separated. A name may select a project the default leaves out (integration, validation); Build.Tests is refused.
    /// </summary>
    [Parameter("TR-MCP-NUKE-011: test or validation project names to run instead of the default unit selection (space- or comma-separated); Build.Tests is refused.")]
    readonly string[] TestProjects = [];

    /// <summary>TR-MCP-NUKE-011: extra dotnet test filter, appended to each project's category filter as (category)&amp;(filter).</summary>
    [Parameter("TR-MCP-NUKE-011: extra dotnet test filter, appended to the category filter as (<category filter>)&(<filter>).")]
    readonly string TestFilter = string.Empty;

    /// <summary>
    /// Run all unit tests, excluding integration test projects and Category=Integration tests; with --test-projects, run only the
    /// named test or validation projects, and with --test-filter, narrow each run (TR-MCP-NUKE-011).
    /// </summary>
    public Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // Session-life unit inventory deliberately excludes integration projects.
            // QBrainAi.PluginIntegration.Tests stays on PluginSessionLogIntegration / P6, not Nuke Test.
            var testProjects = Solution.GetAllProjects("*")
                .Where(p => p.Name.EndsWith(".Tests") || p.Name.EndsWith(".Validation"))
                .Where(p => !p.Name.Contains("IntegrationTests"))
                .Where(p => !p.Name.Contains("PluginIntegration"))
                .Where(p => !p.Name.EndsWith(".Validation"))
                .Where(p => !p.Name.Contains("Review.Tests"))
                // Build.Tests references the Nuke _build project and is excluded from the Compile glob,
                // so it has no built assembly to run with --no-build here. Keep Test consistent with Compile.
                .Where(p => !p.Name.EndsWith("Build.Tests"))
                .ToArray();
            var defaultProjects = testProjects.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

            // TR-MCP-NUKE-011 AC-001, AC-002: --test-projects replaces the default selection; every name is resolved, or refused,
            // before any test runs. A targeted run writes its own lane so it never stands in for the unit inventory.
            var lane = "unit";
            if (TestProjects.Length > 0)
            {
                var solutionProjects = Solution.GetAllProjects("*").ToArray();
                testProjects = SelectTestProjectNames(solutionProjects.Select(p => p.Name).ToArray(), TestProjects)
                    .Select(name => solutionProjects.First(p => string.Equals(p.Name, name, StringComparison.Ordinal)))
                    .ToArray();
                lane = "targeted";
            }

            var runId = SessionLifeUnitGateReports.NormalizeRunId(TestRunId);
            if (runId is not null)
            {
                SessionLifeUnitGateReports.WriteInventory(
                    RootDirectory,
                    runId,
                    lane,
                    testProjects.Select(project => (
                        project.Name,
                        SessionLifeUnitGateReports.RelativePath(RootDirectory, project.Path))));
            }

            var failures = new List<string>();
            foreach (var project in testProjects)
            {
                // TR-MCP-NUKE-011 AC-003, AC-004: the category filter, narrowed by --test-filter when given.
                var filter = ComposeTestFilter(defaultProjects.Contains(project.Name), TestFilter);
                try
                {
                    if (runId is null)
                    {
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .SetFilter(filter)
                            .SetResultsDirectory(RootDirectory / "TestResults"));
                    }
                    else
                    {
                        var directory = SessionLifeUnitGateReports.LaneProjectDirectory(RootDirectory, runId, lane, project.Name);
                        Directory.CreateDirectory(directory);
                        // console;verbosity=detailed streams per-test progress into Nuke logs.
                        // PluginIntegration is excluded from unit inventory (PluginSessionLogIntegration/P6).
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .SetFilter(filter)
                            .SetResultsDirectory(directory)
                            .SetLoggers(
                                $"trx;LogFileName={project.Name}.trx",
                                "console;verbosity=detailed"));
                        SessionLifeUnitGateReports.EnsureCanonicalTrx(directory, $"{project.Name}.trx");
                    }
                }
                catch (Exception ex)
                {
                    if (runId is null)
                        throw;

                    failures.Add(project.Name);
                    Log.Error(ex, "Session-life unit project {Project} failed.", project.Name);
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Session-life unit test projects failed: " + string.Join(", ", failures));
            }
        });

    /// <summary>
    /// TR-MCP-NUKE-011 AC-001, AC-002: resolves the --test-projects names (space- or comma-separated) to test, integration-test, or
    /// validation projects of the solution, in the order given. Throws, before any test runs, for a name that matches none of them,
    /// for a name that selects Build.Tests (infrastructure, run directly, never by a Nuke target), and when no name is given.
    /// </summary>
    /// <param name="solutionProjects">The names of every project in the solution.</param>
    /// <param name="requested">The raw --test-projects values.</param>
    /// <returns>The resolved project names, each once.</returns>
    internal static IReadOnlyList<string> SelectTestProjectNames(IReadOnlyCollection<string> solutionProjects, IReadOnlyCollection<string> requested)
    {
        var names = requested
            .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (names.Length == 0)
            throw new InvalidOperationException("--test-projects names no project.");

        var selected = new List<string>(names.Length);
        foreach (var name in names)
        {
            if (name.EndsWith("Build.Tests", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"--test-projects {name}: Build.Tests is infrastructure and no Nuke target runs it; run it directly with dotnet test.");
            }

            var match = solutionProjects.FirstOrDefault(project =>
                string.Equals(project, name, StringComparison.OrdinalIgnoreCase) && IsTestOrValidationProject(project));
            selected.Add(match ?? throw new InvalidOperationException(
                $"--test-projects {name} matches no test or validation project of the solution."));
        }

        return selected;
    }

    /// <summary>
    /// TR-MCP-NUKE-011 AC-003, AC-004: the dotnet test filter for one project. A project of the default selection keeps
    /// <c>Category!=AiReview&amp;Category!=Integration</c>; a project the default leaves out drops the Integration term, the trait its
    /// classes carry. A --test-filter value is appended as <c>(category)&amp;(filter)</c>.
    /// </summary>
    /// <param name="defaultSelected">Whether the default Test selection includes the project.</param>
    /// <param name="testFilter">The --test-filter value, or empty.</param>
    /// <returns>The filter passed to dotnet test.</returns>
    internal static string ComposeTestFilter(bool defaultSelected, string? testFilter)
    {
        var category = defaultSelected ? "Category!=AiReview&Category!=Integration" : "Category!=AiReview";
        return string.IsNullOrWhiteSpace(testFilter) ? category : $"({category})&({testFilter.Trim()})";
    }

    /// <summary>TR-MCP-NUKE-011: whether a project name is a test, integration-test, or validation project.</summary>
    static bool IsTestOrValidationProject(string name) =>
        name.EndsWith(".Tests", StringComparison.Ordinal)
        || name.EndsWith(".IntegrationTests", StringComparison.Ordinal)
        || name.EndsWith(".Validation", StringComparison.Ordinal);
}
