using System.Text.RegularExpressions;
using NukeBuild.Tests;
using Xunit;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// TEST-MCP-AIUNIT-002: Captures trim-analysis warnings currently hidden by
/// repository-wide trim warning suppression.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-AIUNIT-002; TR-MCP-NUKE-010 AC-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). Its artifact cleanup TryDeleteArtifacts moved unchanged into the linked TrimArtifactCleanup, which TrimArtifactCleanupTests in Build.Tests also calls, so the infrastructure case no longer reaches into this class; RunTrimAnalysisAsync calls it there, the using of System.Diagnostics went with it, and nothing else changed but the namespace.</remarks>
public sealed partial class TrimAnalysisWarningInventoryTests
{
    private static readonly TrimTarget[] TrimTargets =
    [
        new(
            "QBrainAi.Cqrs",
            TrimTargetKind.Library,
            Path.Combine("src", "QBrainAi.Cqrs", "QBrainAi.Cqrs.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)),
        new(
            "QBrainAi.Cqrs.Mvvm",
            TrimTargetKind.Library,
            Path.Combine("src", "QBrainAi.Cqrs.Mvvm", "QBrainAi.Cqrs.Mvvm.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["IL2026"] = 8,
                ["IL2067"] = 6,
                ["IL2070"] = 6,
                ["IL2075"] = 12,
            }),
        new(
            "QBrainAi.McpAgent",
            TrimTargetKind.Library,
            Path.Combine("src", "QBrainAi.McpAgent", "QBrainAi.McpAgent.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)),
        new(
            "QBrainAi.Repl.Core",
            TrimTargetKind.Library,
            Path.Combine("src", "QBrainAi.Repl.Core", "QBrainAi.Repl.Core.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)),
        new(
            "QBrainAi.Launcher",
            TrimTargetKind.Executable,
            Path.Combine("src", "QBrainAi.Launcher", "QBrainAi.Launcher.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["IL2026"] = 12,
            }),
        new(
            "QBrainAi.McpAgent.SampleHost",
            TrimTargetKind.Executable,
            Path.Combine("src", "QBrainAi.McpAgent.SampleHost", "QBrainAi.McpAgent.SampleHost.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["IL2104"] = 5,
            }),
        new(
            "QBrainAi.QBAgent",
            TrimTargetKind.Executable,
            Path.Combine("src", "QBrainAi.QBAgent", "QBrainAi.QBAgent.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["IL2026"] = 12,
                ["IL2104"] = 5,
            }),
        new(
            "QBrainAi.Repl.Host",
            TrimTargetKind.Executable,
            Path.Combine("src", "QBrainAi.Repl.Host", "QBrainAi.Repl.Host.csproj"),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["IL2026"] = 10,
                ["IL2037"] = 1,
                ["IL2104"] = 5,
            }),
    ];

    /// <summary>
    /// W19 coverage: source packable libraries and executables must run the trim
    /// analyzer with suppression disabled and match the captured warning inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Theory]
    [MemberData(nameof(GetTrimTargets))]
    public async Task TrimAnalysis_WithRepositorySuppressionDisabled_MatchesCapturedInventory(TrimTarget target)
    {
        var result = await RunTrimAnalysisAsync(target).ConfigureAwait(true);
        var actualWarnings = CountTrimWarnings(result.Output);

        Assert.True(result.ExitCode == 0, $"{target.Name} trim analysis failed.\n{result.Output}");
        Assert.Equal(target.ExpectedTrimWarnings, actualWarnings);
    }

    /// <summary>
    /// Supplies trim-analysis projects to the theory tests.
    /// </summary>
    public static TheoryData<TrimTarget> GetTrimTargets()
    {
        var data = new TheoryData<TrimTarget>();
        foreach (var target in TrimTargets)
        {
            data.Add(target);
        }

        return data;
    }

    private static async Task<ProcessResult> RunTrimAnalysisAsync(TrimTarget target)
    {
        var artifactsRoot = Path.Combine(Path.GetTempPath(), "mcpserver-trim-analysis", Guid.NewGuid().ToString("N"));
        try
        {
            var arguments = target.Kind == TrimTargetKind.Executable
                ? CreateExecutableArguments(target.ProjectPath, artifactsRoot)
                : CreateLibraryArguments(target.ProjectPath, artifactsRoot);

            return await RunDotnetAsync(arguments).ConfigureAwait(false);
        }
        finally
        {
            TrimArtifactCleanup.TryDeleteArtifacts(artifactsRoot);
        }
    }

    private static string[] CreateExecutableArguments(string projectPath, string artifactsRoot)
    {
        return
        [
            "publish",
            Path.Combine(FindRepositoryRoot(), projectPath),
            "-c",
            "Debug",
            "-v",
            "minimal",
            "-r",
            "win-x64",
            "--artifacts-path",
            artifactsRoot,
            "-p:PublishTrimmed=true",
            "-p:SelfContained=true",
            "-p:SuppressTrimAnalysisWarnings=false",
            "-p:TreatWarningsAsErrors=false",
            $"-p:PublishDir={Path.Combine(artifactsRoot, "publish")}{Path.DirectorySeparatorChar}",
        ];
    }

    private static string[] CreateLibraryArguments(string projectPath, string artifactsRoot)
    {
        return
        [
            "build",
            Path.Combine(FindRepositoryRoot(), projectPath),
            "-c",
            "Debug",
            "-v",
            "minimal",
            "--artifacts-path",
            artifactsRoot,
            "-p:EnableTrimAnalyzer=true",
            "-p:IsTrimmable=true",
            "-p:SuppressTrimAnalysisWarnings=false",
            "-p:TreatWarningsAsErrors=false",
        ];
    }

    private static async Task<ProcessResult> RunDotnetAsync(string[] arguments)
    {
        var result = await BoundedProcess.RunAsync(
            BoundedProcess.Dotnet(FindRepositoryRoot(), arguments),
            TimeSpan.FromMinutes(20),
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        return new ProcessResult(result.ExitCode, string.Concat(result.Stdout, Environment.NewLine, result.Stderr));
    }

    private static Dictionary<string, int> CountTrimWarnings(string output)
    {
        return TrimWarningPattern().Matches(output)
            .Select(match => match.Groups[1].Value)
            .GroupBy(code => code, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing QBrainAi.sln.");
    }

    [GeneratedRegex(@"warning (IL\d{4})", RegexOptions.CultureInvariant)]
    private static partial Regex TrimWarningPattern();

    /// <summary>
    /// Describes one source project included in W19 trim-analysis coverage.
    /// </summary>
    public sealed record TrimTarget(
        string Name,
        TrimTargetKind Kind,
        string ProjectPath,
        Dictionary<string, int> ExpectedTrimWarnings)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Identifies the trim-analysis command shape for a target project.
    /// </summary>
    public enum TrimTargetKind
    {
        /// <summary>
        /// Packable source library analyzed with EnableTrimAnalyzer.
        /// </summary>
        Library,

        /// <summary>
        /// Executable source project analyzed with PublishTrimmed.
        /// </summary>
        Executable,
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}

