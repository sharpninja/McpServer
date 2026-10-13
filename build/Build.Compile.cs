using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Compile every project in the shared build project set (TR-MCP-NUKE-002 AC-002).</summary>
    public Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var buildVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");
            var projectFiles = GetBuildProjectFiles(SourceDirectory, TestsDirectory);

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

    /// <summary>
    /// TR-MCP-NUKE-002 AC-002: the one project set that Compile builds and Restore restores: every <c>*.csproj</c> under
    /// <paramref name="sourceDirectory"/> and <paramref name="testsDirectory"/> except <c>Build.Tests</c>, ordered.
    /// </summary>
    internal static string[] GetBuildProjectFiles(string sourceDirectory, string testsDirectory) => Directory
        .EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
        .Concat(Directory.EnumerateFiles(testsDirectory, "*.csproj", SearchOption.AllDirectories))
        .Where(path => !path.EndsWith(Path.Combine("Build.Tests", "Build.Tests.csproj"), StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
