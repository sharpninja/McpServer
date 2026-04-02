using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Package version for NuGet pack (defaults to GitVersion output)")]
    readonly string PackageVersion;

    /// <summary>Pack McpServer.Client as a NuGet package.</summary>
    public Target PackNuGet => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = SourceDirectory / "McpServer.Client" / "McpServer.Client.csproj";

            var settings = new DotNetPackSettings()
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory / "nupkg");

            if (!string.IsNullOrWhiteSpace(PackageVersion))
                settings = settings.SetProperty("PackageVersion", PackageVersion);

            DotNetPack(_ => settings);
        });
}
