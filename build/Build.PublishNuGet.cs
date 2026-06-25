using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Environment variable used to authenticate NuGet package publishing.</summary>
    public const string NuGetApiKeyEnvironmentVariable = "NUGET_API_KEY";

    /// <summary>nuget.org v3 package source used by the publish target.</summary>
    public const string NuGetOrgSource = "https://api.nuget.org/v3/index.json";

    /// <summary>Publish packed public McpServer NuGet packages to nuget.org.</summary>
    public Target PublishNuGet => _ => _
        .DependsOn(PackNuGet)
        .Executes(() =>
        {
            var apiKey = ResolveNuGetApiKey(Environment.GetEnvironmentVariable);
            var packages = GetNuGetPackagesToPublish(ArtifactsDirectory / "nupkg");
            if (packages.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No NuGet packages found under '{ArtifactsDirectory / "nupkg"}'. Run PackNuGet first.");
            }

            foreach (var package in packages)
            {
                Log.Information("Publishing NuGet package {Package} to {Source}", package.Name, NuGetOrgSource);
                DotNetNuGetPush(_ => _
                    .SetTargetPath(package)
                    .SetSource(NuGetOrgSource)
                    .SetApiKey(apiKey)
                    .EnableSkipDuplicate());
            }
        });

    /// <summary>Resolve the NuGet API key from an environment-variable reader.</summary>
    internal static string ResolveNuGetApiKey(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var apiKey = getEnvironmentVariable(NuGetApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("$(", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set the {NuGetApiKeyEnvironmentVariable} environment variable before running PublishNuGet.");
        }

        return apiKey;
    }

    /// <summary>Find publishable NuGet package files in the pack output directory.</summary>
    internal static IReadOnlyList<AbsolutePath> GetNuGetPackagesToPublish(AbsolutePath packageDirectory)
    {
        if (!Directory.Exists(packageDirectory.ToString()))
        {
            return [];
        }

        return Directory.GetFiles(packageDirectory.ToString(), "*.nupkg", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => (AbsolutePath)path)
            .ToArray();
    }
}
