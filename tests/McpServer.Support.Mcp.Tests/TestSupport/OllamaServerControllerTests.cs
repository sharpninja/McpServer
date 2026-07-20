using McpServer.TestSupport.Ollama;
using Xunit;

namespace McpServer.Support.Mcp.Tests.TestSupport;

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
