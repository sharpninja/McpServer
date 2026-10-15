using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-001 AC-001 (operator 2026-10-15: "Don't triage, fix it"): the build.ps1 bootstrap re-launches through the PowerShell
/// executable of the running installation ($PSHOME), never through the current process's executable (wsmprovhost.exe under WinRM, which
/// exits 0 without building), and stops with an error naming the path when that executable is missing. The mock gate proves the outcome
/// check against pinned cases with a reference rule and negative controls; the bindings run the shipped function from build.ps1 through
/// pwsh over the same cases and check the relaunch line.
/// </summary>
public sealed class BuildBootstrapHostTests
{
    /// <summary>
    /// One pinned case: the files present in a PowerShell home folder, the PowerShell edition and platform, and the executable file name the
    /// bootstrap must choose there, or null when it must stop with an error.
    /// </summary>
    /// <param name="Name">Readable case name used in failure output.</param>
    /// <param name="Edition">PowerShell edition: Core for PowerShell 7, Desktop for Windows PowerShell.</param>
    /// <param name="OnWindows">Whether the platform is Windows.</param>
    /// <param name="Files">File names present in the PowerShell home folder.</param>
    /// <param name="ExpectedFile">The executable file name the bootstrap must choose, or null for an error.</param>
    internal sealed record BootstrapCase(string Name, string Edition, bool OnWindows, IReadOnlyList<string> Files, string? ExpectedFile);

    /// <summary>TR-MCP-NUKE-001 AC-001: the pinned cases the mock gate and the bindings share.</summary>
    internal static readonly IReadOnlyList<BootstrapCase> Cases =
    [
        new("PowerShell 7 on Linux", "Core", false, ["pwsh"], "pwsh"),
        new("PowerShell 7 on Windows", "Core", true, ["pwsh.exe"], "pwsh.exe"),
        new("Windows PowerShell", "Desktop", true, ["powershell.exe"], "powershell.exe"),
        new("PowerShell 7 home without its executable", "Core", true, [], null),
        new("Linux home holding only the Windows executable name", "Core", false, ["pwsh.exe"], null),
    ];

    /// <summary>
    /// TR-MCP-NUKE-001 AC-001 reference rule: the installation's executable for the edition and platform inside the PowerShell home, or
    /// null (an error) when that file is not there.
    /// </summary>
    /// <param name="bootstrapCase">The pinned case.</param>
    /// <param name="home">The PowerShell home folder.</param>
    /// <returns>The executable path, or null for an error.</returns>
    internal static string? ReferenceRule(BootstrapCase bootstrapCase, string home)
    {
        ArgumentNullException.ThrowIfNull(bootstrapCase);
        var name = ExecutableName(bootstrapCase.Edition, bootstrapCase.OnWindows);
        return bootstrapCase.Files.Contains(name, StringComparer.Ordinal) ? Path.Combine(home, name) : null;
    }

    /// <summary>The executable file name of a PowerShell installation for an edition and platform.</summary>
    /// <param name="edition">Core or Desktop.</param>
    /// <param name="onWindows">Whether the platform is Windows.</param>
    /// <returns>pwsh, pwsh.exe or powershell.exe.</returns>
    internal static string ExecutableName(string edition, bool onWindows)
    {
        if (!string.Equals(edition, "Core", StringComparison.Ordinal))
        {
            return "powershell.exe";
        }

        return onWindows ? "pwsh.exe" : "pwsh";
    }

    /// <summary>The names of the pinned cases a rule gets wrong: a different executable, or no error where one is required.</summary>
    /// <param name="rule">The rule under check, given a case and a PowerShell home folder.</param>
    /// <returns>The mismatching case names; empty when the rule meets every case.</returns>
    internal static string[] Mismatches(Func<BootstrapCase, string, string?> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        const string home = "pinned-powershell-home";
        return Cases
            .Where(bootstrapCase =>
            {
                var expected = bootstrapCase.ExpectedFile is null ? null : Path.Combine(home, bootstrapCase.ExpectedFile);
                return !string.Equals(rule(bootstrapCase, home), expected, StringComparison.Ordinal);
            })
            .Select(bootstrapCase => bootstrapCase.Name)
            .ToArray();
    }

    /// <summary>
    /// TR-MCP-NUKE-001 AC-001 mock gate over the pinned <see cref="Cases"/>: the reference rule meets every case; three wrong rules are each
    /// rejected: today's process-path rule (the current process is wsmprovhost.exe under WinRM), a rule without the existence check, and a
    /// rule that ignores the Windows executable name.
    /// </summary>
    [Fact]
    public void BootstrapHostRule_ReferencePassesAndRejectsWrongRules()
    {
        Assert.Empty(Mismatches(ReferenceRule));

        Assert.Contains("PowerShell 7 on Linux", Mismatches((_, _) => @"C:\Windows\System32\wsmprovhost.exe"));
        Assert.Contains(
            "PowerShell 7 home without its executable",
            Mismatches((bootstrapCase, home) => Path.Combine(home, ExecutableName(bootstrapCase.Edition, bootstrapCase.OnWindows))));
        Assert.Contains(
            "PowerShell 7 on Windows",
            Mismatches((bootstrapCase, home) =>
            {
                var name = ExecutableName(bootstrapCase.Edition, onWindows: false);
                return bootstrapCase.Files.Contains(name, StringComparer.Ordinal) ? Path.Combine(home, name) : null;
            }));
    }

    /// <summary>
    /// TR-MCP-NUKE-001 AC-001 binding over the same pinned <see cref="Cases"/>: the shipped Resolve-NukePowerShellHost, taken from
    /// build.ps1 and run through pwsh against a temporary PowerShell home holding each case's files, returns that home's executable or stops
    /// with an error that names the missing path.
    /// </summary>
    [Fact]
    public async Task BootstrapHostRule_ShippedFunction_PinnedCases()
    {
        var script = await ReadRepositoryTextAsync("build.ps1").ConfigureAwait(true);
        var function = Regex.Match(script, @"(?ms)^function Resolve-NukePowerShellHost \{.*?^\}").Value;
        Assert.False(string.IsNullOrEmpty(function), "build.ps1 defines Resolve-NukePowerShellHost.");

        foreach (var bootstrapCase in Cases)
        {
            var home = Directory.CreateTempSubdirectory("nuke-bootstrap-").FullName;
            try
            {
                foreach (var file in bootstrapCase.Files)
                {
                    await File.WriteAllTextAsync(Path.Combine(home, file), string.Empty, TestContext.Current.CancellationToken).ConfigureAwait(true);
                }

                var onWindows = bootstrapCase.OnWindows ? "$true" : "$false";
                var command = function + "\n"
                    + "try { Resolve-NukePowerShellHost -PowerShellHome '" + EscapePowerShellSingleQuotedString(home) + "' -Edition '"
                    + bootstrapCase.Edition + "' -OnWindows " + onWindows + " } catch { 'ERROR: ' + $_.Exception.Message }";
                var (exitCode, stdout, stderr) = await RunPowerShellAsync(command).ConfigureAwait(true);
                Assert.True(exitCode == 0, $"{bootstrapCase.Name}: pwsh exited {exitCode}: {stderr}");

                var output = stdout.Trim();
                if (bootstrapCase.ExpectedFile is null)
                {
                    Assert.True(output.StartsWith("ERROR: ", StringComparison.Ordinal), $"{bootstrapCase.Name}: expected an error, got '{output}'.");
                    Assert.Contains(home, output, StringComparison.Ordinal);
                }
                else
                {
                    Assert.Equal(Path.Combine(home, bootstrapCase.ExpectedFile), output);
                }
            }
            finally
            {
                Directory.Delete(home, recursive: true);
            }
        }
    }

    /// <summary>
    /// TR-MCP-NUKE-001 AC-001 binding: build.ps1's relaunch goes through Resolve-NukePowerShellHost with -NoLogo, -NoProfile and
    /// -NonInteractive, and no longer through the current process's executable.
    /// </summary>
    [Fact]
    public async Task BuildBootstrap_RelaunchesThroughResolvedHostNotProcessPath()
    {
        var script = await ReadRepositoryTextAsync("build.ps1").ConfigureAwait(true);

        Assert.Contains("$powerShellHost = Resolve-NukePowerShellHost", script, StringComparison.Ordinal);
        Assert.Contains("& $powerShellHost -NoLogo -NoProfile -NonInteractive -File $PSCommandPath", script, StringComparison.Ordinal);
        Assert.DoesNotContain("(Get-Process -Id $PID).Path", script, StringComparison.Ordinal);
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunPowerShellAsync(string command)
    {
        var startInfo = new ProcessStartInfo("pwsh");
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);

        var result = await BoundedProcess.RunAsync(startInfo, TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        return (result.ExitCode, result.Stdout, result.Stderr);
    }

    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static async Task<string> ReadRepositoryTextAsync(string relativePath)
    {
        var path = Path.Combine(FindRepositoryRoot(), relativePath);
        return await File.ReadAllTextAsync(path, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
}
