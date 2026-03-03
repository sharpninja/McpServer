namespace McpServer.Director.Tests;

/// <summary>
/// Integration coverage for new Director screen areas executed through
/// <c>director exec</c> aliases.
/// </summary>
public sealed class ExecAreaCoverageTests
{
    public static TheoryData<string> NewAreaListCommands => new()
    {
        "exec tools-list",
        "exec github-issues-list",
        "exec requirements-fr-list",
        "exec events-stream",
        "exec context-search",
        "exec list-repo-entries",
    };

    public static TheoryData<string> NewAreaGetCommands => new()
    {
        "exec tool-detail",
        "exec github-issue-detail",
        "exec requirements-fr-detail",
        "exec events-stream --input \"{\\\"CategoryFilter\\\":\\\"todo\\\"}\"",
        "exec list-context-sources",
        "exec get-repo-file --input \"{\\\"Path\\\":\\\"README.md\\\"}\"",
    };

    [Theory]
    [MemberData(nameof(NewAreaListCommands))]
    public async Task Exec_NewAreaListOperations_Completes(string command)
    {
        var result = await DirectorRunner.RunAsync(command);
        AssertExecOutput(result);
    }

    [Theory]
    [MemberData(nameof(NewAreaGetCommands))]
    public async Task Exec_NewAreaGetOperations_Completes(string command)
    {
        var result = await DirectorRunner.RunAsync(command);
        AssertExecOutput(result);
    }

    private static void AssertExecOutput(CliResult result)
    {
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Resolved:", result.AllOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Executing...", result.AllOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "does not expose an IAsyncRelayCommand property",
            result.AllOutput,
            StringComparison.OrdinalIgnoreCase);
    }
}
