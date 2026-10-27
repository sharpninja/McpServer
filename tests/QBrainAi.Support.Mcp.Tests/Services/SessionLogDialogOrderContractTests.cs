// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Xunit;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-QB-003 / AC-QB-007/008/009/069: partial dialog batches retain exact ordered
/// evidence. One assertion runner binds an independent row-store model and fresh SQLite reads.
/// Original distinct-timestamp cases and equal-timestamp audit-only patches retain insertion order; C7 owns UUID event identity.
/// </summary>
public sealed class SessionLogDialogOrderContractTests
{
    private const string Agent = "Codex";
    private const string Session = "Codex-20260101T000000Z-dialog-order";
    private const string Request = "req-20260101T000001Z-dialog-order";
    private static readonly string[] Cases =
    [
        "two-partial-batches", "omitted-field", "redelivery", "equal-content-distinct-timestamp",
        "duplicate-in-batch", "mixed-append-upsert", "scalar-only-fresh-query",
        "equal-timestamp-action-only", "equal-timestamp-decision-only",
    ];

    /// <summary>Every original dialog scenario runs against the same real SQLite boundary.</summary>
    public static TheoryData<string> Scenarios
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var name in Cases)
                data.Add(name);
            return data;
        }
    }

    /// <summary>
    /// Independent stateful rows exercise the same assertions before any service repair.
    /// Wrong ordinal allocation, read order, deduplication and literal fields must reject.
    /// </summary>
    [Fact]
    public async Task IndependentMockAndRejectingControls_SatisfyOrderedDialogContract()
    {
        var result = await RunIndependentMockGateAsync().ConfigureAwait(true);
        Assert.Equal(9, (int)result["cases"]);
        Assert.Equal(10, (int)result["negativeControls"]);
    }

    /// <summary>
    /// The real service executes the same retained partial batches, omitted-field patches,
    /// redeliveries, dedicated append and fresh read assertions used by the independent model.
    /// </summary>
    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task RealSqlite_PartialDialogPatchesPreserveUniqueMonotonicOrdinalsAndExactContent(string scenario)
    {
        await using var fixture = await SqliteFixture.CreateAsync().ConfigureAwait(true);
        await RunScenarioAsync(fixture, scenario).ConfigureAwait(true);
    }

    internal static async Task<Dictionary<string, object>> RunIndependentMockGateAsync()
    {
        var evidence = new List<object>();
        foreach (var scenario in Cases)
        {
            await using var model = new ModelFixture();
            await RunScenarioAsync(model, scenario).ConfigureAwait(false);
            evidence.Add(new { scenario, effects = model.Effects.ToArray(), rows = await model.ReadRowsAsync().ConfigureAwait(false) });
        }

        (string Scenario, string Fault)[] controls =
        [
            ("two-partial-batches", "batch-zero-ordinal"),
            ("omitted-field", "clear-omitted"),
            ("redelivery", "no-dedup"),
            ("equal-content-distinct-timestamp", "content-dedup"),
            ("two-partial-batches", "reverse-read"),
            ("two-partial-batches", "drop-cr"),
            ("two-partial-batches", "wrong-timestamp"),
            ("two-partial-batches", "gap-ordinal"),
            ("equal-timestamp-action-only", "tie-reorder-after-audit"),
            ("equal-timestamp-decision-only", "tie-reorder-after-audit"),
        ];
        var rejected = new List<object>();
        foreach (var (scenario, fault) in controls)
        {
            await using var model = new ModelFixture(fault);
            try
            {
                await RunScenarioAsync(model, scenario).ConfigureAwait(false);
            }
            catch (XunitException exception)
            {
                rejected.Add(new { scenario, fault, assertion = exception.Message, effects = model.Effects.ToArray() });
                continue;
            }

            throw new InvalidOperationException($"Wrong dialog model behavior was not rejected: {fault}");
        }

        return new Dictionary<string, object>
        {
            ["cases"] = Cases.Length, ["negativeControls"] = rejected.Count,
            ["failed"] = 0, ["skipped"] = 0, ["observations"] = evidence, ["rejectingControls"] = rejected,
        };
    }

    private static async Task RunScenarioAsync(IDialogFixture fixture, string scenario)
    {
        var initial = Enumerable.Range(0, 3).Select(Dialog).ToList();
        var first = Enumerable.Range(3, 2).Select(Dialog).ToList();
        var second = Enumerable.Range(5, 2).Select(Dialog).ToList();
        if (scenario is "equal-timestamp-action-only" or "equal-timestamp-decision-only")
        {
            initial[1].Timestamp = initial[0].Timestamp;
            first[1].Timestamp = first[0].Timestamp;
        }
        var expected = initial.Select(Copy).ToList();
        await fixture.SeedAsync(initial).ConfigureAwait(false);
        await AssertSnapshotAsync(fixture, expected).ConfigureAwait(false);
        switch (scenario)
        {
            case "two-partial-batches":
                await fixture.PatchAsync(first).ConfigureAwait(false);
                expected.AddRange(first.Select(Copy));
                await AssertSnapshotAsync(fixture, expected).ConfigureAwait(false);
                await fixture.PatchAsync(second).ConfigureAwait(false);
                expected.AddRange(second.Select(Copy));
                break;
            case "omitted-field":
                await fixture.PatchAsync(null).ConfigureAwait(false);
                break;
            case "redelivery":
                await fixture.PatchAsync(first).ConfigureAwait(false);
                await fixture.PatchAsync(first.Select(Copy).ToList()).ConfigureAwait(false);
                expected.AddRange(first.Select(Copy));
                break;
            case "equal-content-distinct-timestamp":
                first[1].Content = first[0].Content;
                await fixture.PatchAsync(first).ConfigureAwait(false);
                expected.AddRange(first.Select(Copy));
                break;
            case "duplicate-in-batch":
                await fixture.PatchAsync([first[0], Copy(first[0]), first[1]]).ConfigureAwait(false);
                expected.AddRange(first.Select(Copy));
                break;
            case "mixed-append-upsert":
                await fixture.AppendAsync(first).ConfigureAwait(false);
                expected.AddRange(first.Select(Copy));
                await AssertSnapshotAsync(fixture, expected).ConfigureAwait(false);
                await fixture.PatchAsync(second).ConfigureAwait(false);
                expected.AddRange(second.Select(Copy));
                break;
            case "scalar-only-fresh-query":
                await fixture.PatchAsync(first).ConfigureAwait(false);
                expected.AddRange(first.Select(Copy));
                await fixture.PatchAsync(null).ConfigureAwait(false);
                break;
            case "equal-timestamp-action-only":
            case "equal-timestamp-decision-only":
                await fixture.PatchAsync(first).ConfigureAwait(false);
                expected.AddRange(first.Select(Copy));
                await AssertSnapshotAsync(fixture, expected).ConfigureAwait(false);
                var decisionOnly = scenario == "equal-timestamp-decision-only";
                for (var update = 1; update <= 3; update++)
                {
                    await fixture.PatchAuditOnlyAsync(decisionOnly, update).ConfigureAwait(false);
                    await AssertSnapshotAsync(fixture, expected).ConfigureAwait(false);
                    var auditRead = await fixture.ReadAsync().ConfigureAwait(false);
                    if (decisionOnly)
                        Assert.Equal(Enumerable.Range(1, update).Select(index => $"unrelated decision {index}"), auditRead.DesignDecisions ?? []);
                    else
                        Assert.Equal(Enumerable.Range(1, update).Select(index => $"unrelated action {index}"), (auditRead.Actions ?? []).Select(action => action.Description));
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown frozen dialog scenario.");
        }

        for (var read = 0; read < 3; read++)
            await AssertSnapshotAsync(fixture, expected).ConfigureAwait(false);
    }

    private static async Task AssertSnapshotAsync(IDialogFixture fixture, IReadOnlyList<ProcessingDialogItemDto> expected)
    {
        var rows = await fixture.ReadRowsAsync().ConfigureAwait(false);
        Assert.Equal(Enumerable.Range(0, expected.Count), rows.Select(row => row.Ordinal));
        var dto = await fixture.ReadAsync().ConfigureAwait(false);
        Assert.Equal("Original request\r\nwith exact ingress", dto.QueryText);
        Assert.Equal("Original response", dto.Response);
        Assert.Equal("in_progress", dto.Status);
        var actual = (dto.ProcessingDialog ?? []).ToList();
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].Timestamp, actual[index].Timestamp);
            Assert.Equal(expected[index].Role, actual[index].Role);
            Assert.Equal(expected[index].Category, actual[index].Category);
            Assert.Equal(expected[index].Content, actual[index].Content);
            Assert.Equal(expected[index].Content, rows[index].Item.Content);
        }
    }

    private static ProcessingDialogItemDto Dialog(int index) => new()
    {
        Timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, index, TimeSpan.Zero).ToString("o"),
        Role = index % 2 == 0 ? "model" : "tool",
        Category = index % 2 == 0 ? "decision" : "tool_result",
        Content = $"dialog-{index}: exact \\u2014 literal; Ω\r\nline two",
    };

    private static UnifiedRequestEntryDto Turn(List<ProcessingDialogItemDto>? dialog) => new()
    {
        RequestId = Request, QueryText = "Original request\r\nwith exact ingress",
        QueryTitle = "Preserve ordered dialogs", Response = "Original response", Status = "in_progress",
        ProcessingDialog = dialog, PlanFile = "None", TodoId = "None",
    };

    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    private sealed record Row(int Ordinal, ProcessingDialogItemDto Item);

    private interface IDialogFixture : IAsyncDisposable
    {
        Task SeedAsync(List<ProcessingDialogItemDto> items);
        Task PatchAsync(List<ProcessingDialogItemDto>? items);
        Task AppendAsync(List<ProcessingDialogItemDto> items);
        Task PatchAuditOnlyAsync(bool decisionOnly, int order);
        Task<UnifiedRequestEntryDto> ReadAsync();
        Task<IReadOnlyList<Row>> ReadRowsAsync();
    }

    private sealed class ModelFixture(string? fault = null) : IDialogFixture
    {
        private readonly List<Row> _rows = [];
        private UnifiedRequestEntryDto? _turn;
        private bool _auditOnlyApplied;
        internal List<string> Effects { get; } = [];

        public Task SeedAsync(List<ProcessingDialogItemDto> items)
        {
            Effects.Add("submit-initial");
            _turn = Turn([]);
            foreach (var item in items)
                Add(item, _rows.Count);
            return Task.CompletedTask;
        }

        public Task PatchAsync(List<ProcessingDialogItemDto>? items)
        {
            Effects.Add(items is null ? "patch-omitted" : "patch-dialog");
            if (items is null)
            {
                if (fault == "clear-omitted")
                    _rows.Clear();
                return Task.CompletedTask;
            }

            var batchPosition = 0;
            foreach (var item in items)
            {
                var exists = _rows.Any(row =>
                    row.Item.Content == item.Content &&
                    (fault == "content-dedup" ||
                     row.Item.Timestamp == item.Timestamp && row.Item.Role == item.Role && row.Item.Category == item.Category));
                if (exists && fault != "no-dedup")
                    continue;
                var ordinal = fault == "batch-zero-ordinal" ? batchPosition :
                    _rows.Count == 0 ? 0 : _rows.Max(row => row.Ordinal) + (fault == "gap-ordinal" ? 2 : 1);
                Add(item, ordinal);
                batchPosition++;
            }

            return Task.CompletedTask;
        }

        public Task AppendAsync(List<ProcessingDialogItemDto> items)
        {
            Effects.Add("append-dedicated");
            foreach (var item in items)
                Add(item, _rows.Count == 0 ? 0 : _rows.Max(row => row.Ordinal) + 1);
            return Task.CompletedTask;
        }

        public Task PatchAuditOnlyAsync(bool decisionOnly, int order)
        {
            var turn = _turn ?? throw new InvalidOperationException("Model session not seeded.");
            Effects.Add(decisionOnly ? "patch-decision-only" : "patch-action-only");
            if (decisionOnly)
            {
                turn.DesignDecisions ??= [];
                turn.DesignDecisions.Add($"unrelated decision {order}");
            }
            else
            {
                turn.Actions ??= [];
                turn.Actions.Add(new UnifiedActionDto { Order = order, Type = "edit", Status = "completed", Description = $"unrelated action {order}" });
            }
            _auditOnlyApplied = true;
            return Task.CompletedTask;
        }

        private void Add(ProcessingDialogItemDto input, int ordinal)
        {
            var item = Copy(input);
            if (fault == "drop-cr")
                item.Content = item.Content?.Replace("\r", "", StringComparison.Ordinal);
            if (fault == "wrong-timestamp")
                item.Timestamp = "2026-01-01T01:00:00.0000000+00:00";
            _rows.Add(new Row(ordinal, item));
            Effects.Add($"commit-row:{ordinal}");
        }

        public Task<UnifiedRequestEntryDto> ReadAsync()
        {
            Effects.Add("fresh-read");
            var result = Copy(_turn ?? throw new InvalidOperationException("Model session not seeded."));
            IEnumerable<Row> rows = _rows.OrderBy(row => row.Ordinal);
            if (fault == "tie-reorder-after-audit" && _auditOnlyApplied)
                rows = rows.OrderBy(row => row.Item.Timestamp, StringComparer.Ordinal).ThenByDescending(row => row.Ordinal);
            result.ProcessingDialog = (fault == "reverse-read" ? rows.Reverse() : rows).Select(row => Copy(row.Item)).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Row>> ReadRowsAsync()
        {
            Effects.Add("independent-row-read");
            return Task.FromResult<IReadOnlyList<Row>>(_rows.OrderBy(row => row.Ordinal).Select(row => new Row(row.Ordinal, Copy(row.Item))).ToArray());
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class SqliteFixture(SqliteConnection connection, McpDbContext db, WorkspaceContext workspace, ISessionLogService service) : IDialogFixture
    {
        internal static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            var workspace = new WorkspaceContext { WorkspacePath = "/tests/partial-dialog-order" };
            var db = CreateDb(connection, workspace);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            return new SqliteFixture(connection, db, workspace, CreateService(db, workspace));
        }

        public async Task SeedAsync(List<ProcessingDialogItemDto> items) =>
            _ = await service.SubmitAsync(new UnifiedSessionLogDto { SourceType = Agent, SessionId = Session, Turns = [Turn(items)] },
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);

        public async Task PatchAsync(List<ProcessingDialogItemDto>? items) =>
            _ = await service.UpsertTurnAsync(Agent, Session, new UnifiedRequestEntryDto { RequestId = Request, ProcessingDialog = items },
                TestContext.Current.CancellationToken).ConfigureAwait(false);

        public async Task AppendAsync(List<ProcessingDialogItemDto> items) =>
            _ = await service.AppendProcessingDialogAsync(Agent, Session, Request, items, TestContext.Current.CancellationToken).ConfigureAwait(false);

        public async Task PatchAuditOnlyAsync(bool decisionOnly, int order)
        {
            var payload = new UnifiedRequestEntryDto { RequestId = Request };
            if (decisionOnly)
                payload.DesignDecisions = [$"unrelated decision {order}"];
            else
                payload.Actions = [new UnifiedActionDto { Order = order, Type = "edit", Status = "completed", Description = $"unrelated action {order}" }];
            _ = await service.UpsertTurnAsync(Agent, Session, payload, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        public async Task<UnifiedRequestEntryDto> ReadAsync()
        {
            await using var fresh = CreateDb(connection, workspace);
            var result = await CreateService(fresh, workspace).GetAsync(Agent, Session, TestContext.Current.CancellationToken).ConfigureAwait(false);
            return Assert.Single(Assert.IsType<UnifiedSessionLogDto>(result).Turns!);
        }

        public async Task<IReadOnlyList<Row>> ReadRowsAsync()
        {
            await using var fresh = CreateDb(connection, workspace);
            var rows = await fresh.SessionLogProcessingDialogs.AsNoTracking().OrderBy(row => row.Ordinal).ThenBy(row => row.Id)
                .ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            return rows.Select(row => new Row(row.Ordinal, new ProcessingDialogItemDto
            {
                Timestamp = row.Timestamp.ToString("o"), Role = row.Role, Category = row.Category, Content = row.Content,
            })).ToArray();
        }

        private static McpDbContext CreateDb(SqliteConnection connection, WorkspaceContext workspace) =>
            new(new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options, workspace);

        private static SessionLogService CreateService(McpDbContext db, WorkspaceContext workspace) =>
            new(db, NullLogger<SessionLogService>.Instance, Substitute.For<IChangeEventBus>(), workspace);

        public async ValueTask DisposeAsync()
        {
            await db.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
