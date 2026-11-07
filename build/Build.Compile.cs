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
    /// <paramref name="sourceDirectory"/> and <paramref name="testsDirectory"/> except the two test projects that reference
    /// the build project itself, <c>Build.Tests</c> and <c>Build.IntegrationTests</c>, which the targets that run them build;
    /// ordered.
    /// </summary>
    /// <remarks>2026-11-07, TR-MCP-NUKE-002 AC-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: also leaves out Build.IntegrationTests, the integration-owned project that slice S4 split out of Build.Tests (hostile validation finding R14-F4); it references the running build exactly as Build.Tests does, and the lane builds it (no --no-build).</remarks>
    internal static string[] GetBuildProjectFiles(string sourceDirectory, string testsDirectory) => Directory
        .EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
        .Concat(Directory.EnumerateFiles(testsDirectory, "*.csproj", SearchOption.AllDirectories))
        .Where(path => !path.EndsWith(Path.Combine("Build.Tests", "Build.Tests.csproj"), StringComparison.OrdinalIgnoreCase))
        .Where(path => !path.EndsWith(Path.Combine("Build.IntegrationTests", "Build.IntegrationTests.csproj"), StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
