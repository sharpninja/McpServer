using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Run all unit tests, excluding integration test projects.</summary>
    public Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = Solution.GetAllProjects("*")
                .Where(p => p.Name.EndsWith(".Tests") || p.Name.EndsWith(".Validation"))
                .Where(p => !p.Name.Contains("IntegrationTests"))
                .Where(p => !p.Name.EndsWith(".Validation"))
                .Where(p => !p.Name.Contains("Review.Tests"));

            foreach (var project in testProjects)
            {
                DotNetTest(_ => _
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .SetFilter("Category!=AiReview")
                    .SetResultsDirectory(RootDirectory / "TestResults"));
            }
        });
}
