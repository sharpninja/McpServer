using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-WS-002, MVP-APP-007: Verifies <see cref="WorkspaceService"/> populates
/// <c>WorkspaceDto.GitRemoteUrl</c> from git remote origin when available.
/// Uses a temp appsettings.json workspace registry plus a mocked <see cref="IProcessRunner"/>
/// so behavior can be validated deterministically without invoking a real git process.
/// </summary>
public sealed class WorkspaceServiceGitRemoteTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _appsettingsPath;

    /// <summary>
    /// Creates an isolated temp root and appsettings fixture used by all tests.
    /// The fixture uses an empty <c>Mcp:Workspaces</c> list so each test controls
    /// workspace creation and expected DTO behavior independently.
    /// </summary>
    public WorkspaceServiceGitRemoteTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"mcp-workspace-git-remote-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        _appsettingsPath = Path.Combine(_tempRoot, "appsettings.json");
        File.WriteAllText(
            _appsettingsPath,
            """
            {
              "Mcp": {
                "Workspaces": []
              }
            }
            """);
    }

    /// <summary>
    /// Cleans up temporary filesystem fixtures created for this test class.
    /// Cleanup is best-effort because file watchers can transiently hold handles.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    /// <summary>
    /// MVP-APP-007: Confirms successful git command output is trimmed and surfaced
    /// in <c>WorkspaceDto.GitRemoteUrl</c> during workspace creation.
    /// Uses a real existing workspace directory fixture because missing directories
    /// intentionally skip git execution by design.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenGitRemoteCommandSucceeds_PopulatesGitRemoteUrl()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner
            .RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "https://github.com/octocat/Hello-World.git\r\n", null));

        var sut = CreateSut(processRunner);
        var workspacePath = Path.Combine(_tempRoot, "workspace-with-git");
        Directory.CreateDirectory(workspacePath);

        var result = await sut.CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = workspacePath,
            Name = "workspace-with-git",
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Equal("https://github.com/octocat/Hello-World.git", result.Workspace!.GitRemoteUrl);
        _ = processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(req =>
                req != null
                && req.FileName == "git"
                && req.Arguments.Contains("config --get remote.origin.url", StringComparison.Ordinal)
                && req.Arguments.Contains(workspacePath, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// MVP-APP-007: Validates non-zero git exit codes are handled gracefully and
    /// exposed as a null <c>GitRemoteUrl</c> rather than failing workspace DTO creation.
    /// Uses a workspace directory fixture to ensure the git path executes.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenGitRemoteCommandFails_ReturnsNullGitRemoteUrl()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner
            .RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "fatal: not a git repository"));

        var sut = CreateSut(processRunner);
        var workspacePath = Path.Combine(_tempRoot, "workspace-git-fails");
        Directory.CreateDirectory(workspacePath);

        var result = await sut.CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = workspacePath,
            Name = "workspace-git-fails",
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Null(result.Workspace!.GitRemoteUrl);
    }

    /// <summary>
    /// MVP-APP-007: Ensures missing workspace directories skip process execution and
    /// still return a valid DTO with null <c>GitRemoteUrl</c>.
    /// This verifies directory existence is used as a guard before invoking git.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenWorkspaceDirectoryMissing_DoesNotInvokeGitRunner()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        var sut = CreateSut(processRunner);
        var workspacePath = Path.Combine(_tempRoot, "workspace-missing");

        var result = await sut.CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = workspacePath,
            Name = "workspace-missing",
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Null(result.Workspace!.GitRemoteUrl);
        _ = processRunner.Received(0).RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>());
    }

    private WorkspaceService CreateSut(IProcessRunner processRunner)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_tempRoot)
            .AddJsonFile(Path.GetFileName(_appsettingsPath), optional: false, reloadOnChange: false)
            .Build();

        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_tempRoot);

        return new WorkspaceService(
            configuration,
            environment,
            processRunner,
            NullLogger<WorkspaceService>.Instance);
    }
}

