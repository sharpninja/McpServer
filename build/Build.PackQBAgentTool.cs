using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Build and pack McpServer.QBAgent as a NuGet global tool.</summary>
    public Target PackQBAgentTool => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = SourceDirectory / "McpServer.QBAgent" / "McpServer.QBAgent.csproj";
            var packageVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");

            DotNetPack(_ => _
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(LocalPackagesDirectory)
                .SetProperty("PackageVersion", packageVersion)
                .SetProperty("Version", packageVersion)
                .SetProperty("InformationalVersion", packageVersion));
        });
}
