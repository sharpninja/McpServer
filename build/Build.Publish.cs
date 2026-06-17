using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Publish McpServer.Support.Mcp for deployment.</summary>
    public Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = SourceDirectory / "McpServer.Support.Mcp" / "McpServer.Support.Mcp.csproj";

            DotNetPublish(_ => _
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutput(ArtifactsDirectory / "mcp-server"));

            CopyBrainSlotRuntimeConfig(RootDirectory, ArtifactsDirectory / "mcp-server");
        });
}
