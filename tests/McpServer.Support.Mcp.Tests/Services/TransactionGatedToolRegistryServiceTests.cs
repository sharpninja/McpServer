using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Tool registry mutations execute through the turn transaction
/// coordinator and restore the durable tool/tag graph on post-mutation failure.
/// </summary>
public sealed class TransactionGatedToolRegistryServiceTests
{
    private const string WorkspacePath = @"E:\tests\transaction-gated-tool-registry";

    /// <summary>
    /// TEST-MCP-161: A pre-mutation coordinator rejection prevents a tool row
    /// or tag row from being persisted.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotPersistTool()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var result = await sut.CreateAsync(CreateRequest(), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.False(result.Success);
            Assert.Contains("signing failed", result.Error, StringComparison.Ordinal);
        }

        Assert.NotNull(coordinator.Request);
        Assert.Equal("tool_registry.create", coordinator.Request.OperationName);
        Assert.Equal(0, CountRows(connection, "ToolDefinitions"));
        Assert.Equal(0, CountRows(connection, "ToolDefinitionTags"));
    }

    /// <summary>
    /// TEST-MCP-161: Add-style rollback preserves the just-created tool row and
    /// restores it visible with the same generated database identity.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenCommitFailsAfterCreatedTool_RestoresCreatedToolRecord()
    {
        using var connection = OpenConnection();
        long? createdToolRowId = null;
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
            BeforeRollback = () => createdToolRowId = ScalarLong(
                connection,
                "SELECT Id FROM ToolDefinitions WHERE Name = $name",
                ("$name", "tool-alpha")),
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var result = await sut.CreateAsync(CreateRequest(), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.False(result.Success);
            Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
        }

        Assert.True(coordinator.RollbackAttempted);
        Assert.True(coordinator.RollbackSucceeded);
        Assert.NotNull(createdToolRowId);
        Assert.Equal(
            createdToolRowId.Value,
            ScalarLong(connection, "SELECT Id FROM ToolDefinitions WHERE Name = $name", ("$name", "tool-alpha")));
        Assert.Equal(1, CountVisibleTools(connection, "tool-alpha"));
        Assert.Equal(2, CountVisibleTags(connection, "tool-alpha"));

        var restored = await GetToolAsync(connection, (int)createdToolRowId.Value).ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("tool-alpha", restored!.Name);
        Assert.Equal(["alpha", "mcp"], restored.Tags.OrderBy(tag => tag).ToArray());
    }

    /// <summary>
    /// TEST-MCP-161: Update rollback restores the prior tool scalars and tags,
    /// and leaves tags introduced by the failed update soft-deleted.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenCommitFailsAfterMutation_RestoresPriorToolAndTags()
    {
        using var connection = OpenConnection();
        var toolId = await SeedToolAsync(connection).ConfigureAwait(true);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var result = await sut.UpdateAsync(
                    toolId,
                    new ToolUpdateRequest(
                        Name: "tool-beta",
                        Description: "Updated description",
                        Tags: ["beta", "gamma"],
                        ParameterSchema: """{"type":"string"}""",
                        CommandTemplate: "pwsh -File updated.ps1",
                        WorkspacePath: @"E:\tests\other-workspace"), ct: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            Assert.False(result.Success);
            Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
        }

        var restored = await GetToolAsync(connection, toolId).ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("tool-alpha", restored!.Name);
        Assert.Equal("Original description", restored.Description);
        Assert.Equal("""{"type":"object"}""", restored.ParameterSchema);
        Assert.Equal("pwsh -File original.ps1", restored.CommandTemplate);
        Assert.Null(restored.WorkspacePath);
        Assert.Equal(["alpha", "mcp"], restored.Tags.OrderBy(tag => tag).ToArray());
        Assert.Equal(2, CountSoftDeletedTags(connection, toolId, "beta", "gamma"));
    }

    /// <summary>
    /// TEST-MCP-161: Delete rollback clears soft-delete metadata from the tool
    /// and its existing tag rows instead of recreating different rows.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenCommitFailsAfterMutation_RestoresPriorToolAndTags()
    {
        using var connection = OpenConnection();
        var toolId = await SeedToolAsync(connection).ConfigureAwait(true);
        var tagIdsBefore = TagIds(connection, toolId);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var result = await sut.DeleteAsync(toolId, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.False(result.Success);
            Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
        }

        Assert.Equal(1, CountVisibleTools(connection, "tool-alpha"));
        Assert.Equal(2, CountVisibleTags(connection, "tool-alpha"));
        Assert.Equal(tagIdsBefore, TagIds(connection, toolId));

        var restored = await GetToolAsync(connection, toolId).ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("tool-alpha", restored!.Name);
        Assert.Equal(["alpha", "mcp"], restored.Tags.OrderBy(tag => tag).ToArray());
    }

    /// <summary>
    /// TEST-MCP-161: Registry reads are not mutating entry points and do not
    /// allocate coordinator transactions.
    /// </summary>
    [Fact]
    public async Task SearchListAndGetAsync_DelegateWithoutCoordinatorTransaction()
    {
        using var connection = OpenConnection();
        var toolId = await SeedToolAsync(connection).ConfigureAwait(true);
        var coordinator = new CapturingCoordinator();
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var search = await sut.SearchAsync("alpha", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var list = await sut.ListAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var tool = await sut.GetAsync(toolId, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Single(search.Tools);
            Assert.Single(list.Tools);
            Assert.Equal("tool-alpha", tool?.Name);
        }

        Assert.Null(coordinator.Request);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static (TransactionGatedToolRegistryService Sut, McpDbContext Db) BuildGatedSut(
        SqliteConnection connection,
        CapturingCoordinator coordinator)
    {
        var db = CreateContext(connection);
        db.Database.EnsureCreated();
        var inner = new ToolRegistryService(db, NullLogger<ToolRegistryService>.Instance);
        var sut = new TransactionGatedToolRegistryService(
            inner,
            db,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
        return (sut, db);
    }

    private static async Task<int> SeedToolAsync(SqliteConnection connection)
    {
        var db = CreateContext(connection);
        db.Database.EnsureCreated();
        await using (db.ConfigureAwait(false))
        {
            var registry = new ToolRegistryService(db, NullLogger<ToolRegistryService>.Instance);
            var result = await registry.CreateAsync(CreateRequest()).ConfigureAwait(false);
            Assert.True(result.Success);
            Assert.NotNull(result.Tool);
            return result.Tool!.Id;
        }
    }

    private static async Task<ToolDto?> GetToolAsync(SqliteConnection connection, int id)
    {
        var db = CreateContext(connection);
        await using (db.ConfigureAwait(false))
        {
            var registry = new ToolRegistryService(db, NullLogger<ToolRegistryService>.Instance);
            return await registry.GetAsync(id).ConfigureAwait(false);
        }
    }

    private static McpDbContext CreateContext(SqliteConnection connection)
        => new(
            new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options,
            new WorkspaceContext { WorkspacePath = WorkspacePath });

    private static ToolCreateRequest CreateRequest()
        => new(
            "tool-alpha",
            "Original description",
            ["mcp", "alpha"],
            """{"type":"object"}""",
            "pwsh -File original.ps1");

    private static int CountRows(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int CountVisibleTools(SqliteConnection connection, string name)
        => ScalarInt(
            connection,
            "SELECT COUNT(*) FROM ToolDefinitions WHERE Name = $name AND IsDeleted = 0",
            ("$name", name));

    private static int CountVisibleTags(SqliteConnection connection, string toolName)
        => ScalarInt(
            connection,
            "SELECT COUNT(*) FROM ToolDefinitionTags tags JOIN ToolDefinitions tools ON tools.Id = tags.ToolDefinitionId " +
            "WHERE tools.Name = $name AND tags.IsDeleted = 0",
            ("$name", toolName));

    private static int CountSoftDeletedTags(SqliteConnection connection, int toolId, params string[] tags)
    {
        var total = 0;
        foreach (var tag in tags)
        {
            total += ScalarInt(
                connection,
                "SELECT COUNT(*) FROM ToolDefinitionTags WHERE ToolDefinitionId = $id AND Tag = $tag AND IsDeleted = 1",
                ("$id", toolId),
                ("$tag", tag));
        }

        return total;
    }

    private static IReadOnlyList<long> TagIds(SqliteConnection connection, int toolId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM ToolDefinitionTags WHERE ToolDefinitionId = $id ORDER BY Id";
        command.Parameters.AddWithValue("$id", toolId);
        using var reader = command.ExecuteReader();
        var ids = new List<long>();
        while (reader.Read())
            ids.Add(reader.GetInt64(0));
        return ids;
    }

    private static int ScalarInt(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
        => Convert.ToInt32(Scalar(connection, sql, args));

    private static long ScalarLong(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
        => Convert.ToInt64(Scalar(connection, sql, args));

    private static object Scalar(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in args)
            command.Parameters.AddWithValue(name, value);
        return command.ExecuteScalar()!;
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public bool InvokeMutation { get; init; } = true;

        public bool InvokeRollback { get; init; }

        public string Status { get; init; } = "committed";

        public TransactionFailureReason Reason { get; init; } = TransactionFailureReason.None;

        public string? Message { get; init; }

        public bool RollbackAttempted { get; private set; }

        public bool RollbackSucceeded { get; private set; }

        public Action? BeforeRollback { get; init; }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => ExecuteCoreAsync(request, mutation, cancellationToken);

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = false,
                LastReason = TransactionFailureReason.None,
                Message = "ready",
            };

        private async Task<TurnTransactionResult> ExecuteCoreAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken)
        {
            Request = request;
            TurnMutationResult? mutationResult = null;
            string? rollbackError = null;

            if (InvokeMutation)
            {
                mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
                if (InvokeRollback && mutationResult.RollbackAsync is not null)
                {
                    RollbackAttempted = true;
                    BeforeRollback?.Invoke();
                    try
                    {
                        await mutationResult.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        RollbackSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        rollbackError = ex.Message;
                    }
                }
            }

            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-tool-registry-test",
                Status = Status,
                Reason = Reason,
                MutationApplied = InvokeMutation,
                MutationResult = mutationResult,
                Message = Message,
                RollbackAttempted = RollbackAttempted,
                RollbackSucceeded = RollbackSucceeded,
                RollbackError = rollbackError,
            };
        }
    }
}
