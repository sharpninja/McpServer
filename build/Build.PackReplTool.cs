using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Build and pack McpServer.Repl.Host as a NuGet global tool.</summary>
    public Target PackReplTool => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = SourceDirectory / "McpServer.Repl.Host" / "McpServer.Repl.Host.csproj";

            DotNetPack(_ => _
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(LocalPackagesDirectory)
                .EnableNoBuild());
        });
}
