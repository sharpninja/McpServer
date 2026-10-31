using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003: Process spawning coverage for local triage agent execution. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because its case sets and restores GH_TOKEN in the process environment,
/// the baseline the production delta compares against (TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 as
/// appended on 2026-10-29; hostile validation round 8b finding R8B-F1). The rule itself is proved on explicit maps in
/// <see cref="EnvironmentDeltaContractTests"/>.
/// </summary>
[Trait("Category", "Integration")]
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class DesktopProcessSpawnerTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: Desktop process launches pass only explicit
    /// environment deltas so the desktop user's profile environment remains
    /// authoritative for auth caches and temp folders. Native binding of the delta rule against the real process
    /// environment: the case owns its GH_TOKEN baseline (set to a value other than the child's, restored afterwards),
    /// so its outcome does not depend on the ambient token. TEMP is also set to a fixed baseline and restored;
    /// the unchanged-temp assertion always runs (TR-MCP-USECASE-018 AC-006, slice M SVC-2).
    /// </summary>
    [Fact]
    public void ExtractEnvironment_RemovesServiceDefaultsAndKeepsExplicitDeltas()
    {
        var previousToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        var previousTemp = Environment.GetEnvironmentVariable("TEMP");
        Environment.SetEnvironmentVariable("GH_TOKEN", "baseline-token");
        Environment.SetEnvironmentVariable("TEMP", "/fixture-owned-temp-baseline");
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "codex",
                UseShellExecute = false,
            };
            var path = startInfo.Environment.TryGetValue("PATH", out var existingPath)
                ? existingPath
                : Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            startInfo.Environment["PATH"] = $"C:\\Users\\example\\AppData\\Roaming\\npm;{path}";
            startInfo.Environment["GH_TOKEN"] = "test-token";

            var deltas = DesktopProcessSpawner.ExtractEnvironment(startInfo);

            Assert.NotNull(deltas);
            Assert.Equal("test-token", deltas["GH_TOKEN"]);
            Assert.StartsWith("C:\\Users\\example\\AppData\\Roaming\\npm;", deltas["PATH"], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TEMP", deltas.Keys, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GH_TOKEN", previousToken);
            Environment.SetEnvironmentVariable("TEMP", previousTemp);
        }
    }
}

/// <summary>
/// TEST-MCP-TRIAGE-003: the default spawner on an interactive Windows host. Windows-only and dependent on an
/// interactive session, so an integration test on Windows hosts that fails, instead of returning silently, when
/// the session is not interactive (operator 2026-10-19: "Platform=Windows, fail loudly (Recommended)";
/// TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class DesktopProcessSpawnerWindowsInteractiveTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: Interactive local hosts use the default process spawner
    /// instead of the Windows service desktop-token launcher.
    /// </summary>
    [Fact]
    public async Task Spawn_WhenHostIsInteractive_CanRunCmdThroughDefaultSpawner()
    {
        Assert.True(
            Environment.UserInteractive,
            "This case needs an interactive Windows session; a non-interactive runner (for example a WSMan session) cannot prove the interactive default-spawner path.");

        var spawner = new DesktopProcessSpawner(NullLoggerFactory.Instance);
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("echo desktop-spawner-default");

        using var process = spawner.Spawn(startInfo);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stdout = await stdoutTask.ConfigureAwait(true);
        var stderr = await stderrTask.ConfigureAwait(true);
        Assert.Equal(0, process.ExitCode);
        Assert.Contains("desktop-spawner-default", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(stderr), stderr);
    }
}
