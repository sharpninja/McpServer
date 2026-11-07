using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

/// <summary>TR-MCP-NUKE-020 AC-1 to AC-5: the part of the Nuke build that holds the TestGroups target and its parameters.</summary>
/// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1 to AC-5: documented this partial declaration (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
partial class Build
{
    /// <summary>
    /// TR-MCP-NUKE-020 AC-1: the groups the TestGroups target runs, each <c>&lt;test project&gt;:&lt;Class&gt;[,&lt;Class&gt;...]</c>.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1: added the --test-groups array parameter (operator 2026-11-02).</description></item>
    /// </list>
    /// </remarks>
    [Parameter("TestGroups: the test groups to run, each '<test project>:<Class>[,<Class>...]' (TR-MCP-NUKE-020)", Name = "test-groups")]
    readonly string[] TestGroupSpecs = [];

    /// <summary>
    /// TR-MCP-NUKE-020: runs only the named groups of test classes, whatever their category but never cases tagged for
    /// the other platform (AC-4), one TRX per selected project under
    /// <c>TestResults/&lt;--test-run-id&gt;/groups/&lt;project&gt;</c>, so a mock gate or binding after an edit runs the
    /// changed tests through Nuke (operator 2026-11-02: "Use dedicated targets or an integration target that accepts an
    /// array of tests groups to run."). The composition and TRX check are the lane's (<see cref="TestGroupExecution"/>,
    /// <see cref="IntegrationLaneExecution.CheckTrx"/>); a project whose groups select no case, a failed or skipped case,
    /// or a named class that ran no case (AC-5) fails the target. It provisions nothing and leaves Test and
    /// MigrationIntegrationTests unchanged (AC-3); the full gate set still runs once at phase exit.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1, AC-2, AC-3: added the TestGroups target so mock gates and bindings run only the changed classes through Nuke.</description></item>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-4, AC-5: composes for the current host and checks each named class against its project's TRX.</description></item>
    /// </list>
    /// </remarks>
    public Target TestGroups => _ => _
        .Description("Run only the named test classes (--test-groups '<project>:<Class>[,<Class>...]'), one TRX per project (TR-MCP-NUKE-020)")
        .Executes(() =>
        {
            var runId = TestGroupExecution.RequireRunId(TestRunId);
            // TR-MCP-NUKE-020 AC-4: the host platform clause keeps cases tagged for the other platform out.
            var groupRuns = TestGroupExecution.Compose(
                TestGroupSpecs,
                Solution.GetAllProjects("*").Select(project => (project.Name, project.Path.ToString())).ToArray(),
                IntegrationLaneFilter.CurrentHost());

            // TR-MCP-NUKE-020 AC-2: the log names every filter it runs.
            foreach (var groupRun in groupRuns)
                Log.Information("{TestGroupFilter}", groupRun.Run.LogLine);

            SessionLifeUnitGateReports.WriteInventory(RootDirectory, runId, "groups",
                groupRuns.Select(groupRun => (groupRun.Run.Name, SessionLifeUnitGateReports.RelativePath(RootDirectory, (AbsolutePath)groupRun.Run.Path))));

            var failures = new List<string>();
            foreach (var groupRun in groupRuns)
            {
                var run = groupRun.Run;
                try
                {
                    var directory = SessionLifeUnitGateReports.LaneProjectDirectory(RootDirectory, runId, "groups", run.Name);
                    Directory.CreateDirectory(directory);
                    DotNetTest(_ => _
                        .SetProjectFile((AbsolutePath)run.Path)
                        .SetConfiguration(Configuration)
                        .SetFilter(run.Filter)
                        .SetVerbosity(DotNetVerbosity.minimal)
                        .SetResultsDirectory(directory)
                        .SetLoggers($"trx;LogFileName={run.TrxName}"));
                    SessionLifeUnitGateReports.EnsureCanonicalTrx(directory, run.TrxName);
                    var trx = File.ReadAllText(Path.Combine(directory, run.TrxName));
                    // TR-MCP-NUKE-020 AC-1 and AC-5: a nonempty all-passing result, and a case of every named class.
                    var violation = IntegrationLaneExecution.CheckTrx(run.Name, trx)
                        ?? TestGroupExecution.CheckClasses(run.Name, groupRun.Classes, trx);
                    if (violation is not null) throw new InvalidOperationException(violation);
                }
                catch (Exception ex)
                {
                    failures.Add(run.Name);
                    Log.Error(ex, "Test group project {Project} failed.", run.Name);
                }
            }

            if (failures.Count > 0)
                throw new InvalidOperationException("Test group projects failed: " + string.Join(", ", failures));
        });
}
