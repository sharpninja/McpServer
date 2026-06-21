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

            var settings = new DotNetPackSettings()
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(LocalPackagesDirectory);

            if (!string.IsNullOrWhiteSpace(PackageVersion))
            {
                settings = settings
                    .SetProperty("PackageVersion", PackageVersion)
                    .SetProperty("Version", PackageVersion)
                    .SetProperty("InformationalVersion", PackageVersion);
            }
            else
            {
                settings = settings.EnableNoBuild();
            }

            DotNetPack(_ => settings);
        });
}
