using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>
    /// Pack McpServer.QBAgent as a NuGet global tool. <c>dotnet pack</c> on that project
    /// builds only QBAgent and its referenced projects. Do not depend on solution Compile:
    /// that target builds every test project and fails when testhosts lock those outputs.
    /// </summary>
    public Target PackQBAgentTool => _ => _
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
