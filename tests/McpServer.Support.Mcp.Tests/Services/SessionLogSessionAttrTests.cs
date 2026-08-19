using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-SESSIONATTR-001 / FR-MCP-SESSIONATTR-001 / TR-MCP-SESSIONATTR-001 (BUG-TRIAGE-108):
/// filesModified and commit artifacts that resolve outside the workspace root are rejected
/// unless the turn explicitly marks them as foreign-repo or cross-workspace. Forward-only.
/// Fixture: in-memory <see cref="McpDbContext"/> with workspace
/// <c>E:\tests\sessionlog-session-attr</c>.
/// </summary>
public sealed class SessionLogSessionAttrTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\sessionlog-session-attr";
    private const string ForeignPluginPath = @"F:\GitHub\mcpserver-claude-code-plugin\src\index.ts";
    private const string LocalRelativePath = @"src\McpServer.Services\SessionLogService.cs";
    private const string ForeignPrefix = "foreign:";
    private const string ForeignRepoTag = "foreign-repo";
    private const string Agent = "Cursor";

    private readonly McpDbContext _db;
    private readonly SessionLogService _sut;

    /// <summary>Builds an in-memory session-log service stamped to the test workspace.</summary>
    public SessionLogSessionAttrTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogSessionAttr_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);
        _sut = new SessionLogService(
            _db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath });
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>
    /// AC: filesModified outside workspace root without a foreign marker is rejected.
    /// Named: filesModified outside root rejected or tagged.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_FilesModifiedOutsideRoot_Unmarked_IsRejected()
    {
        var dto = CreateSession("Cursor-20260819T220000Z-attr-unmarked-files");
        dto.Turns!.First().FilesModified = [ForeignPluginPath];

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("foreign", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await _sut.GetAsync(
            Agent,
            dto.SessionId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>
    /// AC: filesModified outside workspace root with an item-level <c>foreign:</c> prefix persists
    /// so completeness audits can filter the prefix.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_FilesModifiedOutsideRoot_ForeignPrefixed_Persists()
    {
        var dto = CreateSession("Cursor-20260819T220000Z-attr-prefixed-files");
        var marked = ForeignPrefix + ForeignPluginPath;
        dto.Turns!.First().FilesModified = [marked];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync(
            Agent,
            dto.SessionId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var files = Assert.Single(fetched!.Turns!).FilesModified;
        Assert.NotNull(files);
        Assert.Contains(marked, files!);
    }

    /// <summary>
    /// AC: filesModified outside workspace root with turn tag <c>foreign-repo</c> persists;
    /// audits can filter that tag.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_FilesModifiedOutsideRoot_ForeignTagged_Persists()
    {
        var dto = CreateSession("Cursor-20260819T220000Z-attr-tagged-files");
        dto.Turns!.First().Tags = [ForeignRepoTag];
        dto.Turns!.First().FilesModified = [ForeignPluginPath];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync(
            Agent,
            dto.SessionId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var turn = Assert.Single(fetched!.Turns!);
        Assert.Contains(ForeignRepoTag, turn.Tags!);
        Assert.Contains(ForeignPluginPath, turn.FilesModified!);
    }

    /// <summary>
    /// AC: commit filesChanged outside workspace root without a foreign marker is rejected.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_CommitFilesOutsideRoot_Unmarked_IsRejected()
    {
        var dto = CreateSession("Cursor-20260819T220000Z-attr-unmarked-commit");
        dto.Turns!.First().Commits =
        [
            new SessionLogCommitDto
            {
                Sha = "abc123def456",
                Message = "plugin reload",
                FilesChanged = [ForeignPluginPath],
            },
        ];

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("foreign", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AC: commit filesChanged outside workspace root with a <c>foreign:</c> prefix persists.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_CommitFilesOutsideRoot_ForeignPrefixed_Persists()
    {
        var dto = CreateSession("Cursor-20260819T220000Z-attr-prefixed-commit");
        var marked = ForeignPrefix + ForeignPluginPath;
        dto.Turns!.First().Commits =
        [
            new SessionLogCommitDto
            {
                Sha = "abc123def456",
                Message = "plugin reload",
                FilesChanged = [marked],
            },
        ];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync(
            Agent,
            dto.SessionId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var commit = Assert.Single(Assert.Single(fetched!.Turns!).Commits!);
        Assert.Contains(marked, commit.FilesChanged!);
    }

    /// <summary>AC: workspace-relative filesModified persist without a foreign marker.</summary>
    [Fact]
    public async Task SubmitAsync_WorkspaceRelativeFilesModified_Persists()
    {
        var dto = CreateSession("Cursor-20260819T220000Z-attr-local-files");
        dto.Turns!.First().FilesModified = [LocalRelativePath];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync(
            Agent,
            dto.SessionId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains(LocalRelativePath, Assert.Single(fetched!.Turns!).FilesModified!);
    }

    /// <summary>
    /// AC: replace_section filesModified outside root without a marker is rejected and
    /// does not mutate the existing turn.
    /// </summary>
    [Fact]
    public async Task ReplaceTurnSectionAsync_FilesModifiedOutsideRoot_Unmarked_IsRejected()
    {
        var sessionId = "Cursor-20260819T220000Z-attr-replace-unmarked";
        var dto = CreateSession(sessionId);
        dto.Turns!.First().FilesModified = [LocalRelativePath];
        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ReplaceTurnSectionAsync(
                Agent,
                sessionId,
                dto.Turns!.First().RequestId!,
                "filesModified",
                new UnifiedRequestEntryDto
                {
                    RequestId = dto.Turns!.First().RequestId,
                    FilesModified = [ForeignPluginPath],
                },
                TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("foreign", ex.Message, StringComparison.OrdinalIgnoreCase);
        var fetched = await _sut.GetAsync(
            Agent,
            sessionId,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains(LocalRelativePath, Assert.Single(fetched!.Turns!).FilesModified!);
        Assert.DoesNotContain(ForeignPluginPath, Assert.Single(fetched.Turns!).FilesModified!);
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = Agent,
            SessionId = sessionId,
            Title = "Session attr",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260819T220000Z-entry-001",
                    Timestamp = "2026-08-19T22:00:00Z",
                    QueryText = "session attr",
                    Status = "in_progress",
                    PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                    TodoId = SessionLogTurnContextValidator.NoneSentinel,
                },
            ],
        };
    }
}
