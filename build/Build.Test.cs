using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Run all unit tests, excluding integration test projects and Category=Integration tests.</summary><remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: after its unchanged name rules select the projects, Test consults the build's role declarations (TestProjectRoles.RequireRole) and refuses, before any test runs, a selected project not declared unit-owned; at the da11faa2 layout the seven selected projects are declared unit-owned, so no case changes (slice S2; this remark shares the summary line so the name-rule lines keep their numbers). 2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: Test runs no quarantined case: every project runs with <see cref="UnitTestFilter.Filter"/>, the earlier category filter followed by the quarantine exclusion, and Test logs <see cref="UnitTestFilter.LogLine"/> for each project before it runs, so the log shows the exclusion; the name rules and the projects are unchanged (after mock gate ug-qr-mockgate-1; this entry also shares the summary line). 2026-11-09, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7; R15-F1: before executing a test, Test discovers all cases and the Integration and quarantine subsets from each selected project, composes the real ownership inventory, and fails on a foreign case instead of trusting recorded counts.</remarks>
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

            // TR-MCP-NUKE-019 AC-7: every project the name rules select must be declared unit-owned by the build.
            TestProjectRoles.RequireRole(TestProjectRole.Unit, testProjects.Select(project => project.Name).ToArray(), nameof(Test));
            var discovery = new DotNetListTestDiscoverySource(Configuration.ToString());
            var discovered = testProjects.Select(project => TestProjectCaseDiscovery.Discover(
                project.Name,
                project.Path.ToString(),
                TestProjectRoles.RoleOf(project.Name),
                discovery)).ToArray();
            var ownership = new TestProjectOwnership().Decide(TestSelectionKind.Unit, discovered);
            TestProjectOwnershipGate.RequireAccepted(nameof(Test), ownership);
            Log.Information("Test runtime ownership discovery accepted {Projects} projects and {Cases} non-quarantined cases.",
                discovered.Length, ownership.Cases.Count);

            var runId = SessionLifeUnitGateReports.NormalizeRunId(TestRunId);
            if (runId is not null)
            {
                SessionLifeUnitGateReports.WriteInventory(
                    RootDirectory,
                    runId,
                    "unit",
                    testProjects.Select(project => (
                        project.Name,
                        SessionLifeUnitGateReports.RelativePath(RootDirectory, project.Path))));
            }

            var failures = new List<string>();
            foreach (var project in testProjects)
            {
                // TR-MCP-NUKE-019 AC-8: the log names the filter, quarantine exclusion included, before the project runs.
                Log.Information("{UnitTestFilter}", UnitTestFilter.LogLine(project.Name));
                try
                {
                    if (runId is null)
                    {
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .SetFilter(UnitTestFilter.Filter)
                            .SetResultsDirectory(RootDirectory / "TestResults"));
                    }
                    else
                    {
                        var directory = SessionLifeUnitGateReports.LaneProjectDirectory(RootDirectory, runId, "unit", project.Name);
                        Directory.CreateDirectory(directory);
                        // console;verbosity=detailed streams per-test progress into Nuke logs.
                        // PluginIntegration is excluded from unit inventory (PluginSessionLogIntegration/P6).
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .SetFilter(UnitTestFilter.Filter)
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
}

/// <summary>
/// TR-MCP-NUKE-019 AC-8: the Test target's selection filter and the filter line it logs for each project: Test runs no
/// Category=AiReview, no Category=Integration and no quarantined case (<see cref="TestQuarantine.Exclusion"/>).
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added after mock gate ug-qr-mockgate-1 (operator 2026-11-07: "Find the AC matching the test behavior.  If none match, quarantine them (keep, but descope them) for later manual investigation."); the filter was a literal in the Test target, which logged no filter line; bound by UnitTestFilterTests to UnitTestFilterContract.</remarks>
internal static class UnitTestFilter
{
    /// <summary>The filter Test passes to the runner for every project it runs.</summary>
    internal const string Filter = "Category!=AiReview&Category!=Integration&" + TestQuarantine.Exclusion;

    /// <summary>The line Test logs before it runs <paramref name="project"/>.</summary>
    /// <param name="project">The test project name.</param>
    /// <returns>The log line naming the project and the whole filter.</returns>
    internal static string LogLine(string project) => $"Unit test filter for {project}: {Filter}";
}
