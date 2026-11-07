namespace NukeBuild.IntegrationTests;

/// <summary>
/// TEST-NUKE-POWERSHELL-001 (historical): Guards build and deployment automation so child
/// PowerShell hosts run without user profiles or interactive prompts.
/// </summary>
/// <remarks>2026-11-07, TEST-NUKE-POWERSHELL-001 (historical); FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"); the helper-flags case now reads RequirementsWikiPublishScriptTests.cs at its new path under tests/Build.IntegrationTests, because that class moved with it; nothing else changed but the namespace.</remarks>
public sealed class PowerShellInvocationTests
{
    /// <summary>
    /// TEST-NUKE-POWERSHELL-001 (historical): Verifies the Nuke PowerShell bootstrap
    /// relaunches through an explicit non-interactive PowerShell host.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task BuildBootstrap_RelaunchesWithNonInteractivePowerShellFlags()
    {
        var script = await ReadRepositoryTextAsync("build.ps1").ConfigureAwait(true);

        Assert.Contains("MCP_NUKE_POWERSHELL_BOOTSTRAPPED", script, StringComparison.Ordinal);
        Assert.Contains("-NoLogo", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-NoProfile", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-NonInteractive", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-File", script, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-NUKE-POWERSHELL-001 (historical): Verifies Build.Tests helpers that spawn
    /// <c>pwsh.exe</c> also use the same non-interactive host flags.
    /// </summary>
    /// <remarks>
    /// <para>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</para>
    /// <para>2026-11-07, TEST-NUKE-POWERSHELL-001 (historical); FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: reads the helper source at tests/Build.IntegrationTests/RequirementsWikiPublishScriptTests.cs, where slice S4 moved it from tests/Build.Tests; the assertions are unchanged.</para>
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task BuildTests_PwshProcessStartInfo_UsesNonInteractiveFlags()
    {
        var testSource = await ReadRepositoryTextAsync(
            Path.Combine("tests", "Build.IntegrationTests", "RequirementsWikiPublishScriptTests.cs")).ConfigureAwait(true);

        Assert.Contains("new ProcessStartInfo(\"pwsh.exe\")", testSource, StringComparison.Ordinal);
        Assert.Contains("psi.ArgumentList.Add(\"-NoLogo\")", testSource, StringComparison.Ordinal);
        Assert.Contains("psi.ArgumentList.Add(\"-NoProfile\")", testSource, StringComparison.Ordinal);
        Assert.Contains("psi.ArgumentList.Add(\"-NonInteractive\")", testSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-NUKE-POWERSHELL-001 (historical): Verifies live deployment guidance does not
    /// tell operators to run script PowerShell hosts interactively.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task DeploymentGuidance_PwshCommandsUseNonInteractiveFlags()
    {
        var files = new[]
        {
            Path.Combine("scripts", "Manage-McpService.ps1"),
            Path.Combine("docs", "USER-GUIDE.md"),
            Path.Combine("docs", "FAQ.md"),
            Path.Combine("docs", "MCP-SERVER.md"),
            Path.Combine("docs", "AGENT-PLUGIN-AVAILABILITY.md"),
        };

        foreach (var relativePath in files)
        {
            var text = await ReadRepositoryTextAsync(relativePath).ConfigureAwait(true);
            var offendingLines = text
                .Split('\n')
                .Select((line, index) => new { Line = line.TrimEnd('\r'), LineNumber = index + 1 })
                .Where(entry => IsPwshCommandLine(entry.Line))
                .Where(entry => !ContainsFlag(entry.Line, "-NoLogo")
                    || !ContainsFlag(entry.Line, "-NoProfile")
                    || !ContainsFlag(entry.Line, "-NonInteractive"))
                .Select(entry => $"{relativePath}:{entry.LineNumber}: {entry.Line}")
                .ToArray();

            Assert.True(
                offendingLines.Length == 0,
                "PowerShell command lines must include -NoLogo, -NoProfile, and -NonInteractive:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, offendingLines));
        }
    }

    private static bool IsPwshCommandLine(string line)
    {
        if (!line.Contains("pwsh.exe", StringComparison.OrdinalIgnoreCase)
            && !line.Contains("pwsh ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return line.Contains("-File", StringComparison.OrdinalIgnoreCase)
            || line.Contains("-Command", StringComparison.OrdinalIgnoreCase)
            || line.Contains(" -c ", StringComparison.OrdinalIgnoreCase)
            || line.Contains("./scripts/", StringComparison.OrdinalIgnoreCase)
            || line.Contains(".\\scripts\\", StringComparison.OrdinalIgnoreCase)
            || line.Contains(".\\build.ps1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsFlag(string line, string flag)
    {
        return line.Contains(flag, StringComparison.OrdinalIgnoreCase);
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
