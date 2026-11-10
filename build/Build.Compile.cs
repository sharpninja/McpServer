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
    /// the build project itself, <c>Build.Tests</c> and <c>NukeBuild.IntegrationTests</c>, which the targets that run them build;
    /// ordered.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, TR-MCP-NUKE-002 AC-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: also leaves out Build.IntegrationTests, the integration-owned project that slice S4 split out of Build.Tests (hostile validation finding R14-F4); it references the running build exactly as Build.Tests does, and the lane builds it (no --no-build).</para>
    /// <para>2026-11-07, TR-MCP-NUKE-002 AC-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the exclusion names NukeBuild.IntegrationTests/NukeBuild.IntegrationTests.csproj, the project Build.IntegrationTests renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the project set is otherwise unchanged.</para>
    /// </remarks>
    internal static string[] GetBuildProjectFiles(string sourceDirectory, string testsDirectory) => Directory
        .EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
        .Concat(Directory.EnumerateFiles(testsDirectory, "*.csproj", SearchOption.AllDirectories))
        .Where(path => !path.EndsWith(Path.Combine("Build.Tests", "Build.Tests.csproj"), StringComparison.OrdinalIgnoreCase))
        .Where(path => !path.EndsWith(Path.Combine("NukeBuild.IntegrationTests", "NukeBuild.IntegrationTests.csproj"), StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
