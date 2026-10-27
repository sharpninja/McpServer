using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Publish QBrainAi.Support.Mcp for deployment.</summary>
    public Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = SourceDirectory / "QBrainAi.Support.Mcp" / "QBrainAi.Support.Mcp.csproj";

            DotNetPublish(_ => _
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutput(ArtifactsDirectory / "qbrain-ai"));

            CopyBrainSlotRuntimeConfig(RootDirectory, ArtifactsDirectory / "qbrain-ai");
        });
}
