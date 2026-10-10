using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-SESSIONLIFE-004: Controlled service tasks verify that REST receipts only
/// acknowledge completed persistence and identify an unambiguous submitted turn.
/// </summary>
public sealed class SessionLogControllerReceiptTests
{
    /// <summary>A pending mocked write cannot produce success; a completed write carries explicit evidence.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task SubmitAsync_AcknowledgesOnlyAfterPersistence(int turnCount)
    {
        var service = Substitute.For<ISessionLogService>();
        var persisted = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dto = CreateRequest(turnCount);
        service.SubmitAsync(dto, null, null, Arg.Any<CancellationToken>()).Returns(persisted.Task);
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);

        var pending = controller.SubmitAsync(dto, TestContext.Current.CancellationToken);
        Assert.False(pending.IsCompleted);
        persisted.SetResult(42);
        var result = Assert.IsType<CreatedResult>(await pending.ConfigureAwait(true));
        var receipt = JsonSerializer.SerializeToElement(result.Value);

        Assert.Equal(42, receipt.GetProperty("id").GetInt64());
        Assert.Equal(dto.SourceType, receipt.GetProperty("sourceType").GetString());
        Assert.Equal(dto.SessionId, receipt.GetProperty("sessionId").GetString());
        Assert.True(receipt.GetProperty("persisted").GetBoolean());
        Assert.False(receipt.GetProperty("degraded").GetBoolean());
        Assert.False(receipt.GetProperty("queued").GetBoolean());
        Assert.Equal(turnCount == 1 ? dto.Turns!.Single().RequestId : null, receipt.GetProperty("requestId").GetString());
        await service.Received(1).SubmitAsync(dto, null, null, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>A database failure remains an error and never returns a positive persistence receipt.</summary>
    [Fact]
    public async Task SubmitAsync_FailedPersistence_DoesNotAcknowledge()
    {
        var service = Substitute.For<ISessionLogService>();
        var dto = CreateRequest(1);
        service.SubmitAsync(dto, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<long>(new DbUpdateException("Synthetic write failure")));
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);

        var result = await controller.SubmitAsync(dto, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.IsNotType<CreatedResult>(result);
        var error = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.True(error.StatusCode >= 400);
        var receipt = JsonSerializer.SerializeToElement(error.Value);
        Assert.False(receipt.TryGetProperty("persisted", out var persisted) && persisted.GetBoolean());
    }

    /// <summary>Cancellation before persistence propagates instead of producing a success receipt.</summary>
    [Fact]
    public async Task SubmitAsync_CanceledPersistence_DoesNotAcknowledge()
    {
        var service = Substitute.For<ISessionLogService>();
        var dto = CreateRequest(1);
        service.SubmitAsync(dto, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<long>(new CancellationToken(canceled: true)));
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.SubmitAsync(dto, TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>A real service write must be readable from storage before its controller acknowledges persistence.</summary>
    [Fact]
    public async Task SubmitAsync_RealService_AcknowledgesReadableSession()
    {
        var workspace = new WorkspaceContext { WorkspacePath = "receipt-test-workspace" };
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"receipt-{Guid.NewGuid()}").Options;
        using var db = new McpDbContext(options, workspace);
        var service = new SessionLogService(db, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);
        var dto = CreateRequest(1);

        var result = Assert.IsType<CreatedResult>(await controller.SubmitAsync(dto, TestContext.Current.CancellationToken).ConfigureAwait(true));
        db.ChangeTracker.Clear();
        var stored = await service.GetAsync(dto.SourceType!, dto.SessionId!, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(stored);
        Assert.Equal(dto.Turns!.Single().Response, Assert.Single(stored.Turns!).Response);
        Assert.True(JsonSerializer.SerializeToElement(result.Value).GetProperty("persisted").GetBoolean());
    }

    /// <summary>A caller-owned transaction cannot receive a durable receipt before an outer rollback.</summary>
    [Fact]
    public async Task SubmitAsync_OuterRollback_NeverReturnsDurableReceipt()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var workspace = new WorkspaceContext { WorkspacePath = "receipt-rollback-workspace" };
        await using var db = new McpDbContext(new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options, workspace);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var service = new SessionLogService(db, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await controller.SubmitAsync(CreateRequest(1), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.IsNotType<CreatedResult>(result);
        var error = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.True(error.StatusCode >= 400);
        Assert.False(JsonSerializer.SerializeToElement(error.Value).TryGetProperty("persisted", out var persisted) && persisted.GetBoolean());
        await transaction.RollbackAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.SessionLogs.ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    private static UnifiedSessionLogDto CreateRequest(int turnCount) => new()
    {
        SourceType = "Codex",
        SessionId = "Codex-20261001T190000Z-receipt",
        Turns = Enumerable.Range(0, turnCount).Select(index => new UnifiedRequestEntryDto
        {
            RequestId = $"req-20261001T190000Z-receipt-{index}",
            Timestamp = "2026-10-01T19:00:00Z", QueryTitle = "Verify receipt",
            QueryText = "Persist the synthetic turn", Response = "Synthetic response", Status = "completed",
            PlanFile = "None", TodoId = "None"
        }).ToList()
    };
}
