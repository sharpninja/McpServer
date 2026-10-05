using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-AIUNIT-002: Captures trim-analysis warnings currently hidden by
/// repository-wide trim warning suppression.
/// </summary>
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
            TryDeleteArtifacts(artifactsRoot);
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
        var processStartInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = FindRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(20));
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException($"dotnet timed out after 20 minutes: {string.Join(" ", arguments)}");
        }

        var output = string.Concat(await stdoutTask.ConfigureAwait(false), Environment.NewLine, await stderrTask.ConfigureAwait(false));
        return new ProcessResult(process.ExitCode, output);
    }

    /// <summary>
    /// Best-effort artifact cleanup. Recursive <see cref="Directory.Delete(string, bool)"/> of a
    /// trimmed self-contained publish tree can block testhost for tens of minutes under AV scan.
    /// </summary>
    private static void TryDeleteArtifacts(string artifactsRoot)
    {
        if (!Directory.Exists(artifactsRoot))
        {
            return;
        }

        try
        {
            var start = new ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/c rmdir /s /q \"{artifactsRoot}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process.Start(start)?.Dispose();
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
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

