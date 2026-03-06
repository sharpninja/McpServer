namespace McpServer.Director.Tests;

/// <summary>
/// Smoke tests for Director sync workflows exposed through the shared GitHub sync ViewModel alias.
/// </summary>
public sealed class SyncCommandTests
{
    [Fact]
    public async Task ListViewModels_FilterSync_ShowsGitHubSyncAlias()
    {
        var result = await DirectorRunner.RunAsync("list-viewmodels --filter sync");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("github-sync", result.AllOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecGitHubSync_CompletesWithoutProcessFailure()
    {
        var result = await DirectorRunner.RunAsync("exec github-sync");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Resolved:", result.AllOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHubSyncViewModel", result.AllOutput, StringComparison.OrdinalIgnoreCase);
    }
}
