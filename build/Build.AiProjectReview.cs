using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>
    /// Executes the test marked [AiProjectReview] via dotnet test. The aiUnit attribute triggers
    /// the actual review using the configured strategy; the test writes aggregated MD to docs/reviews.
    /// </summary>
    public Target AiProjectReview => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log.Information("Running AiProjectReview test via dotnet test so the [AiProjectReview] attribute triggers the library review.");

            DotNetTest(s => s
                .SetProjectFile(TestsDirectory / "McpServer.Review.Tests" / "McpServer.Review.Tests.csproj")
                .SetConfiguration(Configuration)
                .SetFilter("FullyQualifiedName~AiReviewTests.ProjectReview")
                .SetNoBuild(true)
            );
        });
}
