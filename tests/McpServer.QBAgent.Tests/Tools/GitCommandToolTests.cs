using McpServer.QBAgent.Tools;
using McpServer.Support.Mcp.Services;

namespace McpServer.QBAgent.Tests.Tools;

/// <summary>
/// TEST-MCP-QBTOOLS-002: Verifies the QBAgent git tool builds expected git invocations, constrains push to the
/// origin remote, gates push behind the opt-in, and rejects unknown subcommands (FR-MCP-QBTOOLS-004).
/// </summary>
public sealed class GitCommandToolTests
{
    private const string Workspace = "F:/work/repo";

    private static FakeProcessRunner OkRunner(string? stdout = "ok")
        => new(new ProcessRunResult(0, stdout, null));

    /// <summary>status builds `git status` in the workspace and reports success.</summary>
    [Fact]
    public async Task Status_RunsGitStatus_InWorkspace()
    {
        var runner = OkRunner("clean");
        var tool = new GitCommandTool(runner, Workspace, allowPush: false);

        var result = await tool.RunAsync("status", null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("git", runner.LastRequest!.FileName);
        Assert.Equal("status", runner.LastRequest!.Arguments);
        Assert.Equal(Workspace, runner.LastRequest!.WorkingDirectory);
        Assert.Equal("clean", result.Output);
    }

    /// <summary>An argument string is appended after the subcommand.</summary>
    [Fact]
    public async Task Log_AppendsArguments()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: false);

        await tool.RunAsync("log", "--oneline -5", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("log --oneline -5", runner.LastRequest!.Arguments);
    }

    /// <summary>An unknown subcommand is rejected and git is never launched.</summary>
    [Fact]
    public async Task UnknownSubcommand_Rejected_WithoutLaunchingGit()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: false);

        var result = await tool.RunAsync("rm", "-rf .", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(0, runner.InvocationCount);
        Assert.Contains("not allowed", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>push is refused when the opt-in is off and git is never launched.</summary>
    [Fact]
    public async Task Push_Disabled_WhenAllowPushFalse()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: false);

        var result = await tool.RunAsync("push", null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(0, runner.InvocationCount);
        Assert.Contains("disabled", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>push to a non-origin remote is rejected even when push is enabled.</summary>
    [Fact]
    public async Task Push_NonOriginRemote_Rejected()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: true);

        var result = await tool.RunAsync("push", "github main", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(0, runner.InvocationCount);
        Assert.Contains("origin", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>push with no remote injects origin so it never relies on ambient defaults.</summary>
    [Fact]
    public async Task Push_NoRemote_AppendsOrigin()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: true);

        var result = await tool.RunAsync("push", null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("push origin", runner.LastRequest!.Arguments);
    }

    /// <summary>push to origin with a branch is allowed and passed through.</summary>
    [Fact]
    public async Task Push_OriginBranch_Allowed()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: true);

        await tool.RunAsync("push", "origin feature/x", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("push origin feature/x", runner.LastRequest!.Arguments);
        Assert.DoesNotContain("github", runner.LastRequest!.Arguments, StringComparison.Ordinal);
    }

    /// <summary>GuardPush appends origin when only flags are present.</summary>
    [Fact]
    public void GuardPush_FlagsOnly_AppendsOrigin()
    {
        var (ok, args, error) = GitCommandTool.GuardPush("--set-upstream");

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("--set-upstream origin", args);
    }

    /// <summary>push -u origin main is allowed (the short upstream flag precedes the origin remote).</summary>
    [Fact]
    public async Task Push_UpstreamFlagOrigin_Allowed()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: true);

        await tool.RunAsync("push", "-u origin main", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("push -u origin main", runner.LastRequest!.Arguments);
    }

    /// <summary>push to a URL remote is rejected before git is launched.</summary>
    [Fact]
    public async Task Push_UrlRemote_Rejected()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: true);

        var result = await tool.RunAsync("push", "https://evil.example/repo.git main", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(0, runner.InvocationCount);
        Assert.Contains("URL remote", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>push to an scp-style remote (git@host:repo) is rejected.</summary>
    [Fact]
    public async Task Push_ScpRemote_Rejected()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: true);

        var result = await tool.RunAsync("push", "git@github.com:org/repo.git main", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(0, runner.InvocationCount);
    }

    /// <summary>A mutating subcommand also runs in the workspace directory.</summary>
    [Fact]
    public async Task Commit_RunsInWorkspaceDirectory()
    {
        var runner = OkRunner();
        var tool = new GitCommandTool(runner, Workspace, allowPush: false);

        await tool.RunAsync("commit", "-m message", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(Workspace, runner.LastRequest!.WorkingDirectory);
        Assert.Equal("commit -m message", runner.LastRequest!.Arguments);
    }
}
