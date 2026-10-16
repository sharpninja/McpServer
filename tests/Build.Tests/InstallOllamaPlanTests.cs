namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-016 (FR-MCP-BUILD-001; PLAN-SESSIONTRIAGEFIX-001 B13, triage-report-680916ed): InstallOllama's plan per operating system. The
/// plan assertion states the requirement independently of the planner; hand-built facts and plans prove it before the planner is bound to it.
/// </summary>
public sealed class InstallOllamaPlanTests
{
    private const string Model = "gemma4:e4b";
    private const string LinuxTools = "/home/lab/.local/share/QBrainAi/test-tools";
    private const string WindowsTools = @"C:\Users\lab\AppData\Local\QBrainAi\test-tools";

    /// <summary>
    /// TR-MCP-NUKE-016 AC-001 mock gate: the plan assertion accepts hand-built plans for Linux (the linux-amd64 release, ollama/bin/ollama) and
    /// Windows (the windows-amd64 zip, ollama/ollama.exe), an installed or portable executable before any download, a start only when the server
    /// does not answer, a pull only when the model is absent, and nothing when both hold; it rejects a Windows-only plan on Linux, a missing
    /// start, a missing pull, a download although an executable exists, and a step when nothing is needed. Data: hand-built facts and plans.
    /// </summary>
    [Fact]
    public void OllamaInstallPlanAssertion_AcceptsPerOsPlanAndRejectsWindowsOnlyOrMissingStep()
    {
        var linuxBare = new OllamaInstallFacts(false, false, false, null, false, LinuxTools, Model);
        var linuxDownload = new OllamaInstallPlan(
            OllamaExecutableSource.Download, Path.Combine(LinuxTools, "ollama", "bin", "ollama"), "https://ollama.com/download/ollama-linux-amd64.tar.zst", true, true, Model);
        var windowsBare = linuxBare with { IsWindows = true, TestToolsDirectory = WindowsTools };
        var windowsDownload = new OllamaInstallPlan(
            OllamaExecutableSource.Download, Path.Combine(WindowsTools, "ollama", "ollama.exe"), "https://github.com/ollama/ollama/releases/latest/download/ollama-windows-amd64.zip", true, true, Model);
        var linuxInstalled = linuxBare with { InstalledExecutable = "/usr/local/bin/ollama" };
        var linuxPortableServerUp = linuxBare with { ServerAnswers = true, PortableInstallExists = true };
        var linuxReady = linuxBare with { ServerAnswers = true, ModelPresent = true, PortableInstallExists = true };

        AssertPlanFor(linuxBare, linuxDownload);
        AssertPlanFor(windowsBare, windowsDownload);
        AssertPlanFor(linuxInstalled, new OllamaInstallPlan(OllamaExecutableSource.Installed, "/usr/local/bin/ollama", null, true, true, Model));
        AssertPlanFor(
            linuxPortableServerUp,
            new OllamaInstallPlan(OllamaExecutableSource.PortableInstall, Path.Combine(LinuxTools, "ollama", "bin", "ollama"), null, false, true, Model));
        AssertPlanFor(linuxReady, new OllamaInstallPlan(OllamaExecutableSource.None, null, null, false, false, Model));

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertPlanFor(linuxBare, windowsDownload with { Executable = Path.Combine(LinuxTools, "ollama", "ollama.exe") }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertPlanFor(linuxBare, linuxDownload with { DownloadUrl = windowsDownload.DownloadUrl }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertPlanFor(linuxBare, linuxDownload with { StartServer = false }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertPlanFor(linuxBare, linuxDownload with { PullModel = false }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertPlanFor(linuxInstalled, linuxDownload));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertPlanFor(linuxReady, new OllamaInstallPlan(OllamaExecutableSource.None, null, null, false, true, Model)));
    }

    /// <summary>
    /// TR-MCP-NUKE-016 AC-001 binding: the shipped planner, given Linux and Windows facts (bare machine, installed executable, previous portable
    /// install, portable install with the server up, ready), produces the plans the gate's assertion accepts.
    /// </summary>
    [Fact]
    public void InstallOllama_Plan_PerOperatingSystem_UsesTheShippedPlanner()
    {
        foreach (var facts in Cases())
            AssertPlanFor(facts, OllamaInstallPlan.Choose(facts));
    }

    /// <summary>
    /// TR-MCP-NUKE-016 AC-002: when a server already answers with the required model, the shipped planner takes no step on Linux or Windows,
    /// whatever executables exist.
    /// </summary>
    [Fact]
    public void InstallOllama_Plan_ServerAnswersWithModel_ChangesNothing()
    {
        foreach (var facts in Cases().Select(item => item with { ServerAnswers = true, ModelPresent = true }))
        {
            var plan = OllamaInstallPlan.Choose(facts);
            AssertPlanFor(facts, plan);
            Assert.Equal(new OllamaInstallPlan(OllamaExecutableSource.None, null, null, false, false, Model), plan);
        }
    }

    private static IEnumerable<OllamaInstallFacts> Cases()
    {
        foreach (var (isWindows, tools, installed) in new[] { (false, LinuxTools, "/usr/local/bin/ollama"), (true, WindowsTools, @"C:\Tools\ollama.exe") })
        {
            var bare = new OllamaInstallFacts(isWindows, false, false, null, false, tools, Model);
            yield return bare;
            yield return bare with { InstalledExecutable = installed };
            yield return bare with { PortableInstallExists = true };
            yield return bare with { ServerAnswers = true, PortableInstallExists = true };
            yield return bare with { ServerAnswers = true, ModelPresent = true };
        }
    }

    // TR-MCP-NUKE-016 AC-001 and AC-002, stated from the requirement: nothing when a server answers with the model; otherwise an installed
    // ollama, else a previous portable install (ollama.exe on Windows, bin/ollama on Linux, under test-tools/ollama), else that operating
    // system's portable release; then a start when the server does not answer and a pull when the model is absent.
    private static void AssertPlanFor(OllamaInstallFacts facts, OllamaInstallPlan plan)
    {
        Assert.Equal(facts.Model, plan.Model);
        if (facts.ServerAnswers && facts.ModelPresent)
        {
            Assert.Equal(OllamaExecutableSource.None, plan.Source);
            Assert.Null(plan.Executable);
            Assert.Null(plan.DownloadUrl);
            Assert.False(plan.StartServer);
            Assert.False(plan.PullModel);
            return;
        }

        var portable = facts.IsWindows
            ? Path.Combine(facts.TestToolsDirectory, "ollama", "ollama.exe")
            : Path.Combine(facts.TestToolsDirectory, "ollama", "bin", "ollama");
        if (facts.InstalledExecutable is { Length: > 0 } installed)
        {
            Assert.Equal(OllamaExecutableSource.Installed, plan.Source);
            Assert.Equal(installed, plan.Executable);
            Assert.Null(plan.DownloadUrl);
        }
        else if (facts.PortableInstallExists)
        {
            Assert.Equal(OllamaExecutableSource.PortableInstall, plan.Source);
            Assert.Equal(portable, plan.Executable);
            Assert.Null(plan.DownloadUrl);
        }
        else
        {
            Assert.Equal(OllamaExecutableSource.Download, plan.Source);
            Assert.Equal(portable, plan.Executable);
            if (facts.IsWindows)
                Assert.EndsWith("/ollama-windows-amd64.zip", plan.DownloadUrl ?? string.Empty, StringComparison.Ordinal);
            else
                Assert.Contains("/ollama-linux-amd64.", plan.DownloadUrl ?? string.Empty, StringComparison.Ordinal);
        }

        Assert.Equal(!facts.ServerAnswers, plan.StartServer);
        Assert.Equal(!facts.ModelPresent, plan.PullModel);
    }
}
