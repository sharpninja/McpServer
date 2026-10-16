using QBrainAi.TestSupport.Ollama;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TEST-MCP-QBOLLAMA-002: Unit tests for <see cref="OllamaServerController"/> covering probe-first startup,
/// ownership-scoped teardown, missing-executable diagnostics, and orphan-free timeout handling.
/// Fixtures are fake probe/resolver/launcher delegates plus a fake clock, so no real Ollama binary,
/// process, or network endpoint is required and the cases run inside the default unit gate.
/// Validates FR-MCP-QBOLLAMA-002 and TR-MCP-QBOLLAMA-002.
/// </summary>
public sealed class OllamaServerControllerTests
{
    /// <summary>
    /// Verifies a server that already answers the probe is adopted rather than launched.
    /// Fixture: probe returns true on the first call; the launcher records any invocation.
    /// </summary>
    [Fact]
    public async Task EnsureRunningAsync_WhenServerAlreadyRunning_AdoptsWithoutLaunching()
    {
        var launcher = new RecordingLauncher();
        var controller = Build(probeResults: [true], launcher: launcher);

        var result = await controller.EnsureRunningAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.WasAlreadyRunning);
        Assert.False(result.StartedByController);
        Assert.Equal(0, launcher.StartCount);
    }

    /// <summary>
    /// Verifies teardown never stops a server the controller did not start.
    /// Fixture: probe succeeds immediately so nothing is owned, then StopAsync is called.
    /// </summary>
    [Fact]
    public async Task StopAsync_WhenServerWasAlreadyRunning_LeavesItRunning()
    {
        var launcher = new RecordingLauncher();
        var controller = Build(probeResults: [true], launcher: launcher);
        await controller.EnsureRunningAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await controller.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, launcher.StartCount);
        Assert.Empty(launcher.Handles);
    }

    /// <summary>
    /// Verifies a missing server is launched exactly once and polled until it answers.
    /// Fixture: probe returns false, false, then true, so the controller must poll twice after launching.
    /// </summary>
    [Fact]
    public async Task EnsureRunningAsync_WhenServerMissing_LaunchesOnceAndPollsUntilReady()
    {
        var launcher = new RecordingLauncher();
        var controller = Build(probeResults: [false, false, true], launcher: launcher);

        var result = await controller.EnsureRunningAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.WasAlreadyRunning);
        Assert.True(result.StartedByController);
        Assert.Equal(1, launcher.StartCount);
        Assert.Equal(ExecutablePath, result.ExecutablePath);
        Assert.False(launcher.Handles[0].Killed);
    }

    /// <summary>
    /// Verifies an undiscoverable executable produces an actionable message and no launch attempt.
    /// Fixture: probe returns false and the resolver returns null.
    /// </summary>
    [Fact]
    public async Task EnsureRunningAsync_WhenExecutableMissing_ThrowsNamingInstallTarget()
    {
        var launcher = new RecordingLauncher();
        var controller = Build(probeResults: [false], launcher: launcher, executable: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await controller.EnsureRunningAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        Assert.Contains("InstallOllama", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, launcher.StartCount);
    }

    /// <summary>
    /// Verifies a server that never answers is killed before the failure surfaces, leaving no orphan.
    /// Fixture: probe always returns false and the fake clock advances past the configured timeout.
    /// </summary>
    [Fact]
    public async Task EnsureRunningAsync_WhenServerNeverAnswers_KillsStartedProcessAndThrows()
    {
        var launcher = new RecordingLauncher();
        var controller = Build(probeResults: [], launcher: launcher);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await controller.EnsureRunningAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        Assert.Equal(1, launcher.StartCount);
        Assert.True(launcher.Handles[0].Killed);
    }

    /// <summary>
    /// Verifies teardown of an owned server kills the process once and tolerates repeated calls.
    /// Fixture: probe returns false then true so the controller owns the process, then StopAsync runs twice.
    /// </summary>
    [Fact]
    public async Task StopAsync_WhenControllerStartedServer_KillsOnceAndIsIdempotent()
    {
        var launcher = new RecordingLauncher();
        var controller = Build(probeResults: [false, true], launcher: launcher);
        await controller.EnsureRunningAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await controller.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await controller.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, launcher.Handles[0].KillCount);
    }

    /// <summary>
    /// TEST-MCP-QBOLLAMA-002 AC-001 mock gate, case (7) (TR-MCP-QBOLLAMA-002, TR-MCP-NUKE-016; PLAN-SESSIONTRIAGEFIX-001 B13,
    /// triage-report-680916ed): the resolver assertion accepts hand-built candidate lists in the required order for Linux (each PATH entry's
    /// ollama, then the test-tools ollama/bin/ollama) and Windows (each PATH entry's ollama.exe, LocalAppData/Programs/Ollama/ollama.exe, then
    /// the test-tools ollama/ollama.exe), and rejects the Windows layout on Linux, a list without the PATH entries and a list in another order.
    /// Data: hand-built PATH entries, LocalAppData folders and candidate lists; no file system.
    /// </summary>
    [Fact]
    public void OllamaExecutableCandidatesAssertion_AcceptsPerOsLayoutAndRejectsWindowsLayoutOnLinux()
    {
        string[] linuxPath = ["/usr/local/bin", "/usr/bin"];
        const string linuxAppData = "/home/lab/.local/share";
        string[] linuxCandidates =
        [
            Path.Combine("/usr/local/bin", "ollama"),
            Path.Combine("/usr/bin", "ollama"),
            Path.Combine(linuxAppData, "QBrainAi", "test-tools", "ollama", "bin", "ollama"),
        ];
        string[] windowsPath = [@"C:\Tools"];
        const string windowsAppData = @"C:\Users\lab\AppData\Local";
        string[] windowsCandidates =
        [
            Path.Combine(@"C:\Tools", "ollama.exe"),
            Path.Combine(windowsAppData, "Programs", "Ollama", "ollama.exe"),
            Path.Combine(windowsAppData, "QBrainAi", "test-tools", "ollama", "ollama.exe"),
        ];

        AssertCandidatesFor(isWindows: false, linuxPath, linuxAppData, linuxCandidates);
        AssertCandidatesFor(isWindows: true, windowsPath, windowsAppData, windowsCandidates);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertCandidatesFor(
            false, linuxPath, linuxAppData, [linuxCandidates[0], linuxCandidates[1], Path.Combine(linuxAppData, "QBrainAi", "test-tools", "ollama", "ollama.exe")]));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertCandidatesFor(false, linuxPath, linuxAppData, [linuxCandidates[2]]));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertCandidatesFor(
            false, linuxPath, linuxAppData, [linuxCandidates[2], linuxCandidates[0], linuxCandidates[1]]));
    }

    /// <summary>
    /// TEST-MCP-QBOLLAMA-002 AC-001 binding, case (7): the shipped resolver, given the gate's PATH entries and LocalAppData folders, returns the
    /// candidates the assertion accepts on Linux and on Windows. Data: the same hand-built PATH entries and folders; no file system.
    /// </summary>
    [Fact]
    public void OllamaExecutableCandidates_PerOperatingSystem_UsesTheShippedResolver()
    {
        string[] linuxPath = ["/usr/local/bin", "/usr/bin"];
        const string linuxAppData = "/home/lab/.local/share";
        string[] windowsPath = [@"C:\Tools"];
        const string windowsAppData = @"C:\Users\lab\AppData\Local";

        AssertCandidatesFor(isWindows: false, linuxPath, linuxAppData, OllamaExecutableCandidates.For(false, linuxPath, linuxAppData));
        AssertCandidatesFor(isWindows: true, windowsPath, windowsAppData, OllamaExecutableCandidates.For(true, windowsPath, windowsAppData));
    }

    // TEST-MCP-QBOLLAMA-002 AC-001, case (7), stated from the requirement: each PATH entry's ollama (ollama.exe on Windows), on Windows Ollama's
    // install folder, then the portable executable under the test-tools folder (ollama/ollama.exe on Windows; ollama/bin/ollama on Linux).
    private static void AssertCandidatesFor(bool isWindows, IReadOnlyList<string> pathEntries, string localAppData, IReadOnlyList<string> candidates)
    {
        var expected = pathEntries.Select(entry => Path.Combine(entry, isWindows ? "ollama.exe" : "ollama")).ToList();
        if (isWindows)
            expected.Add(Path.Combine(localAppData, "Programs", "Ollama", "ollama.exe"));
        expected.Add(isWindows
            ? Path.Combine(localAppData, "QBrainAi", "test-tools", "ollama", "ollama.exe")
            : Path.Combine(localAppData, "QBrainAi", "test-tools", "ollama", "bin", "ollama"));
        Assert.Equal(expected, candidates);
    }

    private const string ExecutablePath = @"C:\fake\ollama.exe";

    private static OllamaServerController Build(
        IReadOnlyList<bool> probeResults,
        RecordingLauncher launcher,
        string? executable = ExecutablePath)
    {
        var probeIndex = 0;
        var now = DateTimeOffset.UnixEpoch;

        return new OllamaServerController(
            probeAsync: _ =>
            {
                var value = probeIndex < probeResults.Count && probeResults[probeIndex];
                probeIndex++;
                return Task.FromResult(value);
            },
            resolveExecutable: () => executable,
            startProcess: path => launcher.Start(path),
            pollInterval: TimeSpan.FromMilliseconds(10),
            startupTimeout: TimeSpan.FromMilliseconds(50),
            delayAsync: (delay, _) =>
            {
                now = now.Add(delay);
                return Task.CompletedTask;
            },
            utcNow: () => now);
    }

    private sealed class RecordingLauncher
    {
        internal int StartCount { get; private set; }

        internal List<FakeHandle> Handles { get; } = [];

        internal IOllamaProcessHandle Start(string executablePath)
        {
            StartCount++;
            var handle = new FakeHandle(executablePath);
            Handles.Add(handle);
            return handle;
        }
    }

    private sealed class FakeHandle : IOllamaProcessHandle
    {
        internal FakeHandle(string executablePath) => ExecutablePath = executablePath;

        internal string ExecutablePath { get; }

        internal bool Killed => KillCount > 0;

        internal int KillCount { get; private set; }

        public bool HasExited => Killed;

        public void Kill() => KillCount++;

        public void Dispose()
        {
        }
    }
}
