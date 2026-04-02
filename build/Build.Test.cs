using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Run all unit and integration tests.</summary>
    public Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetResultsDirectory(ArtifactsDirectory / "test-results"));
        });
}
