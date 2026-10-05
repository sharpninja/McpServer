using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Package version for NuGet pack (defaults to GitVersion output)")]
    readonly string PackageVersion = string.Empty;

    /// <summary>
    /// Packs public QBrainAI.* libraries. QBrainAi.Common.AgentCli is embedded in QBrainAI.Repl.Core
    /// and is not packed on its own. SharpNinja.McpServer.* facades stay unpackaged in Phase 1.
    /// </summary>
    public Target PackNuGet => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var packageVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");
            var packageOutputDirectory = ArtifactsDirectory / "nupkg";
            CleanNuGetPackageOutput(packageOutputDirectory);
            var projects = new[]
            {
                SourceDirectory / "QBrainAi.Client" / "QBrainAi.Client.csproj",
                SourceDirectory / "QBrainAi.Cqrs" / "QBrainAi.Cqrs.csproj",
                SourceDirectory / "QBrainAi.Cqrs.Mvvm" / "QBrainAi.Cqrs.Mvvm.csproj",
                SourceDirectory / "QBrainAi.Repl.Core" / "QBrainAi.Repl.Core.csproj",
                SourceDirectory / "QBrainAi.McpAgent" / "QBrainAi.McpAgent.csproj",
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
