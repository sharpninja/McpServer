// SPDX-License-Identifier: Apache-2.0
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-AUDITACTION-001: SQLite-backed session writes prove tool execution
/// metadata validation, unchanged legacy behavior and rejection before mutation.
/// </summary>
public sealed class SessionLogToolExecutionTests : IDisposable
{
    private const string Agent = "Codex";
    private const string Session = "Codex-20261009T210000Z-tool-audit";
    private const string Request = "req-20261009T210001Z-tool-audit";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly McpDbContext _db;
    private readonly SessionLogService _service;

    /// <summary>Creates a real isolated SQLite graph, avoiding mocked persistence.</summary>
    public SessionLogToolExecutionTests()
    {
        _connection.Open();
        var workspace = new WorkspaceContext { WorkspacePath = "/tests/tool-audit" };
        _db = new McpDbContext(new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options, workspace);
        _db.Database.EnsureCreated();
        _service = new SessionLogService(_db, NullLogger<SessionLogService>.Instance, Substitute.For<IChangeEventBus>(), workspace);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    /// <summary>All four write surfaces reject each malformed field under TEST-MCP-AUDITACTION-001.</summary>
    public static TheoryData<string, string> InvalidWrites
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var operation in new[] { "submit", "upsert", "replace", "section" })
                foreach (var invalidField in new[] { "description", "status", "order" })
                    data.Add(operation, invalidField);
            return data;
        }
    }

    /// <summary>Valid actions retain metadata and paired dialog without creating a design decision.</summary>
    [Theory]
    [InlineData("tool_execution", "completed")]
    [InlineData("TOOL_EXECUTION", "failed")]
    [InlineData("tool_execution", "pending")]
    public async Task ValidExecution_RoundTripsWithoutDecisionPromotion(string type, string status)
    {
        var turn = Turn();
        turn.Actions = [Action(type, status)];
        turn.ProcessingDialog =
        [
            new() { Role = "model", Category = "tool_call", Content = "build invocation" },
            new() { Role = "tool", Category = "tool_result", Content = "build output" },
        ];
        await _service.SubmitAsync(SessionDto(turn), cancellationToken: TestContext.Current.CancellationToken);
        var saved = Assert.Single((await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken))!.Turns!);
        var action = Assert.Single(saved.Actions!);
        Assert.Equal(type, action.Type);
        Assert.Equal(status, action.Status);
        Assert.Equal(3, action.Order);
        Assert.Equal("Execute repository build", action.Description);
        Assert.Equal("src/worker.cs", action.FilePath);
        Assert.Equal(new[] { "tool_call", "tool_result" }, saved.ProcessingDialog!.Select(item => item.Category));
        Assert.Empty(saved.DesignDecisions ?? []);
        Assert.DoesNotContain(saved.ProcessingDialog!, item => item.Category == "decision");
    }

    /// <summary>Invalid mixed action batches cannot leak scalar or collection changes through a later save.</summary>
    [Theory]
    [MemberData(nameof(InvalidWrites))]
    public async Task InvalidExecution_RejectsBeforeTrackedOrPersistedMutation(string operation, string field)
    {
        var original = Turn();
        original.Actions = [Action("edit", "completed")];
        await _service.SubmitAsync(SessionDto(original), cancellationToken: TestContext.Current.CancellationToken);
        var invalid = Action("tool_execution", "completed");
        if (field == "description") invalid.Description = " ";
        if (field == "status") invalid.Status = null;
        if (field == "order") invalid.Order = -1;
        var replacement = Turn();
        replacement.Response = "REJECTED CHANGE";
        replacement.Actions = [Action("create", "completed"), invalid];
        var error = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            switch (operation)
            {
                case "submit":
                    var dto = SessionDto(replacement);
                    dto.Title = "REJECTED TITLE";
                    await _service.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken);
                    break;
                case "upsert":
                    await _service.UpsertTurnAsync(Agent, Session, replacement, TestContext.Current.CancellationToken);
                    break;
                case "replace":
                    await _service.ReplaceTurnAsync(Agent, Session, replacement, TestContext.Current.CancellationToken);
                    break;
                case "section":
                    await _service.ReplaceTurnSectionAsync(Agent, Session, Request, "actions", replacement, TestContext.Current.CancellationToken);
                    break;
            }
        });
        Assert.Contains("tool_execution", error.Message, StringComparison.Ordinal);
        Assert.Contains(field, error.Message, StringComparison.Ordinal);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var saved = (await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken))!;
        Assert.Equal("Original session", saved.Title);
        var turn = Assert.Single(saved.Turns!);
        Assert.Equal("Original response", turn.Response);
        Assert.Equal("edit", Assert.Single(turn.Actions!).Type);
    }

    /// <summary>A later invalid turn prevents any turn or session in a new submission from being persisted.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidLaterTurn_RejectsWholeSubmission(bool existing)
    {
        if (existing)
            await _service.SubmitAsync(SessionDto(Turn()), cancellationToken: TestContext.Current.CancellationToken);
        var first = Turn();
        first.Actions = [Action("tool_execution", "completed")];
        var second = Turn();
        second.RequestId = "req-20261009T210002Z-tool-audit";
        second.Actions = [new UnifiedActionDto { Type = "tool_execution", Status = "completed" }];
        var dto = SessionDto(first);
        dto.Turns = [first, second];
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var saved = await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken);
        if (existing) Assert.Empty(Assert.Single(saved!.Turns!).Actions ?? []);
        else Assert.Null(saved);
    }

    /// <summary>Existing custom action payloads keep their prior acceptance behavior.</summary>
    [Fact]
    public async Task LegacyCustomAction_RemainsAccepted()
    {
        var turn = Turn();
        turn.Actions = [new UnifiedActionDto { Type = "custom-host-action", Order = -1 }];
        await _service.SubmitAsync(SessionDto(turn), cancellationToken: TestContext.Current.CancellationToken);
        var saved = Assert.Single((await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken))!.Turns!);
        Assert.Equal("custom-host-action", Assert.Single(saved.Actions!).Type);
    }

    /// <summary>Submit validates only the last duplicate, for both new and existing sessions.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DuplicateRequestIds_LastWinsBeforeActionValidation(bool existing, bool invalidLast)
    {
        if (existing)
            await _service.SubmitAsync(SessionDto(Turn()), cancellationToken: TestContext.Current.CancellationToken);
        var valid = Turn();
        valid.Actions = [Action("tool_execution", "completed")];
        var invalid = Turn();
        invalid.Actions = [new UnifiedActionDto { Type = "tool_execution" }];
        var dto = SessionDto(valid);
        dto.Turns = invalidLast ? [valid, invalid] : [invalid, valid];
        if (invalidLast)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken));
            await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var saved = await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken);
            if (existing) Assert.Empty(Assert.Single(saved!.Turns!).Actions ?? []);
            else Assert.Null(saved);
        }
        else
        {
            await _service.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken);
            var saved = Assert.Single((await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken))!.Turns!);
            Assert.Equal("tool_execution", Assert.Single(saved.Actions!).Type);
        }
    }

    /// <summary>Ignored late snapshots cannot reject or alter retained terminal history.</summary>
    [Theory]
    [InlineData("in_progress")]
    [InlineData("canceled")]
    public async Task LateSnapshot_InvalidActionsRemainIgnored(string lateStatus)
    {
        var original = Turn();
        original.Status = "completed";
        original.Actions = [Action("edit", "completed")];
        await _service.SubmitAsync(SessionDto(original), cancellationToken: TestContext.Current.CancellationToken);
        var late = Turn();
        late.Status = lateStatus;
        late.Actions = [new UnifiedActionDto { Type = "tool_execution" }];
        await _service.SubmitAsync(SessionDto(late), cancellationToken: TestContext.Current.CancellationToken);
        var saved = Assert.Single((await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken))!.Turns!);
        Assert.Equal("completed", saved.Status);
        Assert.Equal("edit", Assert.Single(saved.Actions!).Type);
    }

    /// <summary>A zero execution order stays zero even when not first in the action collection.</summary>
    [Fact]
    public async Task ZeroExecutionOrder_RoundTripsWithoutPositionalRewrite()
    {
        var execution = Action("tool_execution", "completed");
        execution.Order = 0;
        var turn = Turn();
        turn.Actions = [Action("edit", "completed"), execution];
        await _service.SubmitAsync(SessionDto(turn), cancellationToken: TestContext.Current.CancellationToken);
        var saved = Assert.Single((await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken))!.Turns!);
        Assert.Equal(0, Assert.Single(saved.Actions!, a => a.Type == "tool_execution").Order);
    }

    /// <summary>Replacing a different section does not validate or persist unrelated payload actions.</summary>
    [Fact]
    public async Task OtherSection_IgnoresUnwrittenActions()
    {
        await _service.SubmitAsync(SessionDto(Turn()), cancellationToken: TestContext.Current.CancellationToken);
        var payload = Turn();
        payload.Actions = [new UnifiedActionDto { Type = "tool_execution" }];
        payload.Tags = ["tag-update"];
        await _service.ReplaceTurnSectionAsync(Agent, Session, Request, "tags", payload, TestContext.Current.CancellationToken);
        var saved = Assert.Single((await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken))!.Turns!);
        Assert.Empty(saved.Actions ?? []);
        Assert.Contains("tag-update", saved.Tags!);
    }
    /// <summary>TEST-MCP-AUDITACTION-001: invalid resurrection rolls back tombstone changes in SQLite.</summary>
    [Fact]
    public async Task InvalidResubmission_LeavesDeletedSessionDeleted()
    {
        await _service.SubmitAsync(SessionDto(Turn()), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(await _service.DeleteSessionAsync(Agent, Session, cancellationToken: TestContext.Current.CancellationToken));
        var invalid = Turn();
        invalid.Actions = [new UnifiedActionDto { Type = "tool_execution" }];
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SubmitAsync(SessionDto(invalid), cancellationToken: TestContext.Current.CancellationToken));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Null(await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken));
        _db.ChangeTracker.Clear();
        Assert.Null(await _service.GetAsync(Agent, Session, TestContext.Current.CancellationToken));
    }
    private static UnifiedActionDto Action(string type, string status) => new()
    {
        Order = 3, Type = type, Status = status,
        Description = "Execute repository build", FilePath = "src/worker.cs",
    };

    private static UnifiedRequestEntryDto Turn() => new()
    {
        RequestId = Request, Status = "in_progress", Response = "Original response",
        QueryText = "Build", QueryTitle = "Build repository", PlanFile = "None", TodoId = "None",
    };

    private static UnifiedSessionLogDto SessionDto(UnifiedRequestEntryDto turn) => new()
    {
        SourceType = Agent, SessionId = Session, Title = "Original session", Turns = [turn],
    };
}
