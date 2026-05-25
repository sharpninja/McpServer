using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Package version for NuGet pack (defaults to GitVersion output)")]
    readonly string PackageVersion;

    /// <summary>Pack public McpServer libraries as NuGet packages.</summary>
    public Target PackNuGet => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var projects = new[]
            {
                SourceDirectory / "McpServer.Client" / "McpServer.Client.csproj",
                SourceDirectory / "McpServer.Cqrs" / "McpServer.Cqrs.csproj",
                SourceDirectory / "McpServer.Cqrs.Mvvm" / "McpServer.Cqrs.Mvvm.csproj",
            };

            foreach (var project in projects)
            {
                var settings = new DotNetPackSettings()
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory / "nupkg");

                if (!string.IsNullOrWhiteSpace(PackageVersion))
                    settings = settings.SetProperty("PackageVersion", PackageVersion);

                DotNetPack(_ => settings);
            }
        });
}
