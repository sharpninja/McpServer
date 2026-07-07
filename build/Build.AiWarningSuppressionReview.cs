using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>
    /// Executes the aiUnit warning suppression governance review.
    /// </summary>
    public Target AiWarningSuppressionReview => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log.Information("Running AiWarningSuppressionReview test via dotnet test so the aiUnit project review attribute triggers the governance review.");

            DotNetTest(s => s
                .SetProjectFile(TestsDirectory / "McpServer.Review.Tests" / "McpServer.Review.Tests.csproj")
                .SetConfiguration(Configuration)
                .SetFilter("FullyQualifiedName~AiReviewTests.WarningSuppressionGovernanceReview")
                .SetNoBuild(true)
            );
        });
}
