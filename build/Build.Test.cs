using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Run all unit tests, excluding integration test projects and Category=Integration tests.</summary>
    public Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = Solution.GetAllProjects("*")
                .Where(p => p.Name.EndsWith(".Tests") || p.Name.EndsWith(".Validation"))
                .Where(p => !p.Name.Contains("IntegrationTests"))
                .Where(p => !p.Name.EndsWith(".Validation"))
                .Where(p => !p.Name.Contains("Review.Tests"))
                // Build.Tests references the Nuke _build project and is excluded from the Compile glob,
                // so it has no built assembly to run with --no-build here. Keep Test consistent with Compile.
                .Where(p => !p.Name.EndsWith("Build.Tests"))
                .ToArray();

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
                try
                {
                    if (runId is null)
                    {
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .SetFilter("Category!=AiReview&Category!=Integration")
                            .SetResultsDirectory(RootDirectory / "TestResults"));
                    }
                    else
                    {
                        var directory = SessionLifeUnitGateReports.LaneProjectDirectory(RootDirectory, runId, "unit", project.Name);
                        Directory.CreateDirectory(directory);
                        // console;verbosity=detailed streams per-test progress into Nuke logs.
                        // PluginIntegration alone is multi-host + multi-theory (~30s/host) and can
                        // exceed 30+ minutes; TRX-only logging looks like a hang to external watchdogs.
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .SetFilter("Category!=AiReview&Category!=Integration")
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
