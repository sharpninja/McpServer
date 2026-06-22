using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>
    /// Executes the test marked [AiCodeReview] via dotnet test. The aiUnit attribute triggers
    /// the actual review using the configured strategy; the test writes aggregated MD to docs/reviews.
    /// </summary>
    public Target AiCodeReview => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log.Information("Running AiCodeReview test via dotnet test so the [AiCodeReview] attribute triggers the library review.");

            DotNetTest(s => s
                .SetProjectFile(TestsDirectory / "McpServer.Review.Tests" / "McpServer.Review.Tests.csproj")
                .SetConfiguration(Configuration)
                .SetFilter("FullyQualifiedName~AiReviewTests.CodeReview")
                .SetNoBuild(true)
            );
        });
}
