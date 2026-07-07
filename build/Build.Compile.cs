using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Compile the solution.</summary>
    public Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var buildVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");
            var projectFiles = Directory
                .EnumerateFiles(SourceDirectory.ToString(), "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(TestsDirectory.ToString(), "*.csproj", SearchOption.AllDirectories))
                .Where(path => !path.EndsWith(Path.Combine("Build.Tests", "Build.Tests.csproj"), StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(Path.Combine("AgentPluginCore", "AgentPluginCore.Tests.csproj"), StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var projectFile in projectFiles)
            {
                DotNetBuild(_ => _
                    .SetProjectFile(projectFile)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .SetProperty("PackageVersion", buildVersion)
                    .SetProperty("Version", buildVersion)
                    .SetProperty("InformationalVersion", buildVersion));
            }
        });
}
