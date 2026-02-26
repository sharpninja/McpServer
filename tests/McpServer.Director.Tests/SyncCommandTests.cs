namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <c>sync status</c> and <c>sync run</c> commands.
/// Requires the MCP server to be running.
/// </summary>
public sealed class SyncCommandTests
{
    [Fact]
    public async Task SyncHelp_ExitZero_ListsSubcommands()
    {
        var result = await DirectorRunner.RunAsync("sync --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("status", result.StdOut);
        Assert.Contains("run", result.StdOut);
    }

    // ── sync status ─────────────────────────────────────────────────────

    [Fact]
    public async Task SyncStatusHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("sync status --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task SyncStatus_ReturnsStatus()
    {
        var result = await DirectorRunner.RunAsync("sync status");

        Assert.Equal(0, result.ExitCode);
        // Output should contain JSON sync status.
        Assert.False(string.IsNullOrWhiteSpace(result.AllOutput));
    }

    // ── sync run ────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncRunHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("sync run --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task SyncRun_Completes()
    {
        // sync run can take a long time; allow 120s for a full ingestion.
        var result = await DirectorRunner.RunAsync("sync run", timeoutMs: 120_000);

        Assert.True(result.ExitCode == 0, $"Unexpected exit code {result.ExitCode}: {result.AllOutput}");
    }
}
