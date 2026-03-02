namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <c>login</c>, <c>logout</c>, and <c>whoami</c> auth commands.
/// These commands do NOT require a Keycloak server; they test CLI structure and local token cache.
/// </summary>
public sealed class AuthCommandTests
{
    // ── login ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginHelp_ExitZero_ShowsOptions()
    {
        var result = await DirectorRunner.RunAsync("login --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--authority", result.StdOut);
        Assert.Contains("--client-id", result.StdOut);
    }

    // ── logout ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ExitZero_ClearsCache()
    {
        var result = await DirectorRunner.RunAsync("logout");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Logged out", result.AllOutput);
    }

    // ── whoami ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Whoami_WhenNotLoggedIn_ShowsWarning()
    {
        // Ensure clean state.
        await DirectorRunner.RunAsync("logout");

        var result = await DirectorRunner.RunAsync("whoami");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Not logged in", result.AllOutput);
    }
}
