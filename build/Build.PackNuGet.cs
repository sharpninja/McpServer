using Nuke.Common;
using Nuke.Common.IO;
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
            var packageVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");
            var packageOutputDirectory = ArtifactsDirectory / "nupkg";
            CleanNuGetPackageOutput(packageOutputDirectory);
            var projects = new[]
            {
                SourceDirectory / "McpServer.Client" / "McpServer.Client.csproj",
                SourceDirectory / "McpServer.Cqrs" / "McpServer.Cqrs.csproj",
                SourceDirectory / "McpServer.Cqrs.Mvvm" / "McpServer.Cqrs.Mvvm.csproj",
                SourceDirectory / "McpServer.Repl.Core" / "McpServer.Repl.Core.csproj",
                SourceDirectory / "McpServer.McpAgent" / "McpServer.McpAgent.csproj",
            };

            foreach (var project in projects)
            {
                var settings = new DotNetPackSettings()
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(packageOutputDirectory)
                    .SetProperty("PackageVersion", packageVersion)
                    .SetProperty("Version", packageVersion)
                    .SetProperty("InformationalVersion", packageVersion);

                DotNetPack(_ => settings);
            }
        });

    /// <summary>Resolve the NuGet package version from an explicit parameter or GitVersion.yml next-version.</summary>
    internal static string ResolveNuGetPackageVersion(string? packageVersion, AbsolutePath gitVersionPath)
    {
        if (!string.IsNullOrWhiteSpace(packageVersion))
            return packageVersion.Trim();

        if (!File.Exists(gitVersionPath.ToString()))
            throw new FileNotFoundException("GitVersion.yml was not found.", gitVersionPath.ToString());

        return ResolveNuGetPackageVersionFromGitVersion(File.ReadAllText(gitVersionPath.ToString()));
    }

    /// <summary>Parse the next-version value used by GitVersion as the local package-version default.</summary>
    internal static string ResolveNuGetPackageVersionFromGitVersion(string gitVersionContent)
    {
        ArgumentNullException.ThrowIfNull(gitVersionContent);

        foreach (var line in gitVersionContent.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("next-version:", StringComparison.Ordinal))
                continue;

            var value = trimmed["next-version:".Length..].Split('#', 2)[0].Trim().Trim('\'', '"');
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        throw new InvalidOperationException("Could not parse next-version from GitVersion.yml.");
    }

    /// <summary>Remove stale NuGet packages before packing the current release version.</summary>
    internal static void CleanNuGetPackageOutput(AbsolutePath packageDirectory)
    {
        Directory.CreateDirectory(packageDirectory.ToString());

        foreach (var package in Directory.GetFiles(packageDirectory.ToString(), "*.nupkg", SearchOption.TopDirectoryOnly))
        {
            File.Delete(package);
        }
    }
}
