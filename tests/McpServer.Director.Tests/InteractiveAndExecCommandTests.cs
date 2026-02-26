namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <c>interactive</c> (with aliases <c>tui</c> and <c>ui</c>)
/// and the MVVM commands <c>exec</c> and <c>list-viewmodels</c>.
/// The interactive command is only tested via <c>--help</c> since it launches a TUI.
/// </summary>
public sealed class InteractiveAndExecCommandTests
{
    // ── interactive / tui / ui ───────────────────────────────────────────

    [Fact]
    public async Task InteractiveHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("interactive --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task TuiAlias_Help_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("tui --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task UiAlias_Help_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("ui --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    // ── exec ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("exec --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("viewmodel", result.StdOut);
        Assert.Contains("--input", result.StdOut);
    }

    [Fact]
    public async Task Exec_UnknownViewModel_PrintsError()
    {
        var result = await DirectorRunner.RunAsync("exec nonexistent-viewmodel");

        Assert.Equal(0, result.ExitCode);
        // Should report the VM was not found.
        Assert.Contains("Error", result.AllOutput);
    }

    [Fact]
    public async Task Exec_ListTodos_ReturnsResult()
    {
        var result = await DirectorRunner.RunAsync("exec list-todos");

        Assert.Equal(0, result.ExitCode);
        // Should contain "Resolved:" and either a result or an error.
        Assert.Contains("Resolved", result.AllOutput);
    }

    // ── list-viewmodels ─────────────────────────────────────────────────

    [Fact]
    public async Task ListViewModels_ExitZero_ShowsTable()
    {
        var result = await DirectorRunner.RunAsync("list-viewmodels");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Alias", result.AllOutput);
        Assert.Contains("Type", result.AllOutput);
    }

    [Fact]
    public async Task ListViewModels_WithFilter_FiltersResults()
    {
        var result = await DirectorRunner.RunAsync("list-viewmodels --filter todo");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("todo", result.AllOutput, StringComparison.OrdinalIgnoreCase);
    }
}
