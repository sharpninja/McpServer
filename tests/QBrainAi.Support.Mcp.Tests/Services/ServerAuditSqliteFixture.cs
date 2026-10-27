// SPDX-License-Identifier: Apache-2.0
using System.Collections.Concurrent;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-QB-003/005, TEST-QB-008/009/069: Real SQLite dependency adapter for the
/// unchanged ServerAuditEvent.Contract.ps1 scenario runner. No scenario outcomes
/// are manufactured here; receipts come from the service and projections from fresh reads.
/// </summary>
public sealed partial class ServerAuditSqliteFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;
    private readonly ConcurrentQueue<object> _trace = new();
    private string _nextFailure = string.Empty;

    private ServerAuditSqliteFixture(string databasePath)
    {
        DatabasePath = databasePath;
        _connectionString = $"Data Source={databasePath};Pooling=False";
    }

    /// <summary>Retained fixture database used by independent connections and reopen probes.</summary>
    public string DatabasePath { get; }

    /// <summary>Creates a unique owned database, retaining it for evidence instead of deleting it.</summary>
    /// <param name="artifactRoot">Task-owned artifact directory.</param>
    /// <returns>The initialized real storage adapter.</returns>
    public static ServerAuditSqliteFixture Create(string artifactRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        var path = Path.Combine(artifactRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var fixture = new ServerAuditSqliteFixture(Path.Combine(path, "audit.sqlite"));
        using var db = fixture.OpenDb();
        db.Database.EnsureCreated();
        fixture._trace.Enqueue(new { operation = "CreateDatabase", databasePath = fixture.DatabasePath });
        return fixture;
    }

    /// <summary>Runs one dependency interaction using native JSON without PowerShell DTO coercion.</summary>
    /// <param name="operation">The frozen provider operation.</param>
    /// <param name="inputJson">Exactly serialized scenario input.</param>
    /// <returns>Native JSON from real storage or the actual service response.</returns>
    public string Invoke(string operation, string inputJson)
        => JsonSerializer.Serialize(InvokeAsync(operation, inputJson).GetAwaiter().GetResult(), JsonOptions);

    private async Task<object?> InvokeAsync(string operation, string inputJson)
    {
        using var input = JsonDocument.Parse(inputJson);
        var body = input.RootElement;
        _trace.Enqueue(new { operation, input = body.Clone() });
        switch (operation)
        {
            case "Configure":
                _nextFailure = Text(body, "failAt");
                return new { code = "configured" };
            case "Seed":
            {
                var workspacePath = Text(body, "workspacePath");
                await using var db = OpenDb(workspacePath: workspacePath);
                var service = Service(db, workspacePath);
                var agent = Text(body, "agentIdentity");
                var session = Text(body, "mcpSessionId");
                var turn = new UnifiedRequestEntryDto
                {
                    RequestId = Text(body, "requestId"), Status = Text(body, "status"),
                    PlanFile = "None", TodoId = "None",
                };
                if (await service.GetAsync(agent, session).ConfigureAwait(false) is null)
                    _ = await service.SubmitAsync(new UnifiedSessionLogDto
                    {
                        SourceType = agent, SessionId = session, Model = "contract-fixture",
                        Turns = [turn],
                    }).ConfigureAwait(false);
                else
                    _ = await service.UpsertTurnAsync(agent, session, turn).ConfigureAwait(false);
                return new { code = "seeded" };
            }
            case "Submit":
                return await SubmitAsync(inputJson).ConfigureAwait(false);
            case "Read":
            {
                var workspacePath = Text(body, "workspacePath");
                await using var db = OpenDb(workspacePath: workspacePath);
                return await Service(db, workspacePath).ReadAuditEventAsync(Text(body, "agentIdentity"),
                    Text(body, "mcpSessionId"), Text(body, "requestId"), Text(body, "eventId")).ConfigureAwait(false);
            }
            case "Complete":
            {
                var workspacePath = Text(body, "workspacePath");
                await using var db = OpenDb(workspacePath: workspacePath);
                return await Service(db, workspacePath).CheckAuditCompletionAsync(Text(body, "agentIdentity"),
                    Text(body, "mcpSessionId"), Text(body, "requestId")).ConfigureAwait(false);
            }
            case "Project":
            {
                var workspacePath = Text(body, "workspacePath");
                await using var db = OpenDb(workspacePath: workspacePath);
                var session = await Service(db, workspacePath).GetAsync(Text(body, "agentIdentity"),
                    Text(body, "mcpSessionId")).ConfigureAwait(false);
                var turn = session?.Turns?.Single(item => item.RequestId == Text(body, "requestId"))
                    ?? throw new InvalidOperationException("Actual stored turn is missing");
                var events = await db.SessionAuditEvents.AsNoTracking().Where(row => row.Accepted
                    && row.AgentIdentity == Text(body, "agentIdentity")
                    && row.McpSessionId == Text(body, "mcpSessionId")
                    && row.RequestId == Text(body, "requestId"))
                    .OrderBy(row => row.CapturedOrder).Select(row => row.EventId).ToArrayAsync().ConfigureAwait(false);
                return new
                {
                    turn.Status, actions = turn.Actions ?? [], designDecisions = turn.DesignDecisions ?? [],
                    processingDialog = turn.ProcessingDialog ?? [], eventIds = events,
                };
            }
            case "Snapshot":
                return await SnapshotAsync(Text(body, "workspacePath")).ConfigureAwait(false);
            case "Reopen":
            {
                // Each invocation owns/disposes its context. Explicitly reopen the retained
                // file here and query it, proving this operation is not a serialized model.
                var workspacePath = Text(body, "workspacePath");
                await using var db = OpenDb(workspacePath: workspacePath);
                await db.Database.OpenConnectionAsync().ConfigureAwait(false);
                var rowCount = await db.SessionAuditEvents.AsNoTracking().CountAsync().ConfigureAwait(false);
                _trace.Enqueue(new { operation = "ReopenDatabase", contextId = db.ContextId.ToString(), rowCount });
                return new { code = "reopened" };
            }
            case "ConcurrentSubmit":
            {
                var inputs = body.GetProperty("events").EnumerateArray().Select(item => item.GetRawText()).ToArray();
                var ready = 0;
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var tasks = inputs.Select(item => Task.Run(async () =>
                {
                    var auditInput = JsonSerializer.Deserialize<SessionAuditEventRequest>(item, JsonOptions)!;
                    var workspacePath = auditInput.WorkspacePath;
                    await using var db = OpenDb(workspacePath: workspacePath);
                    await db.Database.OpenConnectionAsync().ConfigureAwait(false);
                    _trace.Enqueue(new { operation = "OpenContext", contextId = db.ContextId.ToString() });
                    if (Interlocked.Increment(ref ready) == inputs.Length)
                        release.TrySetResult();
                    await release.Task.ConfigureAwait(false);
                    _trace.Enqueue(new { operation = "Submit", input = JsonSerializer.Deserialize<JsonElement>(item) });
                    return await Service(db, workspacePath).AppendAuditEventAsync(
                        auditInput).ConfigureAwait(false);
                })).ToArray();
                return new { replies = await Task.WhenAll(tasks).ConfigureAwait(false) };
            }
            default:
                throw new ArgumentException("Unknown frozen provider operation", nameof(operation));
        }
    }

    private async Task<SessionAuditEventReceipt> SubmitAsync(string inputJson)
    {
        var point = Interlocked.Exchange(ref _nextFailure, string.Empty);
        if (point == "before-commit")
            throw new IOException("scripted_transport_timeout_before_commit");
        var interceptor = new ProjectionFailureInterceptor(_trace, point);
        var auditInput = JsonSerializer.Deserialize<SessionAuditEventRequest>(inputJson, JsonOptions)!;
        var workspacePath = auditInput.WorkspacePath;
        await using var db = OpenDb(interceptor, workspacePath);
        var reply = await Service(db, workspacePath).AppendAuditEventAsync(auditInput).ConfigureAwait(false);
        if (point.StartsWith("after-decision-", StringComparison.Ordinal) && !interceptor.Fired)
            throw new InvalidOperationException("Requested physical storage failure was not exercised");
        if (point == "after-commit")
            throw new IOException("scripted_transport_timeout_after_commit");
        return reply;
    }

    private async Task<object> SnapshotAsync(string workspacePath = "F:/fixture/qb")
    {
        await using var db = OpenDb(workspacePath: workspacePath);
        var rows = await db.SessionAuditEvents.AsNoTracking().OrderBy(row => row.Id).ToListAsync().ConfigureAwait(false);
        var accepted = new Dictionary<string, object>(StringComparer.Ordinal);
        var rejected = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var value = new
            {
                root = $"{row.CapturedWorkspacePath}|{row.AgentIdentity}|{row.McpSessionId}|{row.RequestId}",
                @event = new
                {
                    workspacePath = row.CapturedWorkspacePath, row.AgentIdentity, row.McpSessionId, row.RequestId, row.EventId,
                    order = row.CapturedOrder, row.Kind, row.PayloadJson, row.PayloadHash,
                },
                revision = row.Accepted ? (long?)row.Id : null,
            };
            (row.Accepted ? accepted : rejected).Add(row.EventId, value);
        }
        var workspaceId = db.CurrentWorkspaceId;
        var turns = await db.SessionLogTurns.AsNoTracking()
            .Where(row => row.SessionLog!.WorkspaceId == workspaceId)
            .Select(row => new { row.Id, row.RequestId, row.Status })
            .ToArrayAsync().ConfigureAwait(false);
        return new
        {
            events = accepted, rejected, turns, trace = _trace.ToArray(),
            revision = rows.Where(row => row.Accepted).Select(row => row.Id).DefaultIfEmpty(0).Max(),
            databasePath = DatabasePath,
        };
    }

    private McpDbContext OpenDb(ProjectionFailureInterceptor? interceptor = null, string workspacePath = "F:/fixture/qb")
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connectionString);
        if (interceptor is not null)
            options.AddInterceptors(interceptor, new RollbackWitness(_trace));
        return new McpDbContext(options.Options, new WorkspaceContext { WorkspacePath = workspacePath });
    }

    private static SessionLogService Service(McpDbContext db, string workspacePath = "F:/fixture/qb")
        => new(db, NullLogger<SessionLogService>.Instance,
            workspaceContext: new WorkspaceContext { WorkspacePath = workspacePath });

    private static string Text(JsonElement body, string key) => body.GetProperty(key).GetString()!;

    private sealed class ProjectionFailureInterceptor(ConcurrentQueue<object> trace, string point) : DbCommandInterceptor
    {
        internal bool Fired { get; private set; }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            var channel = command.CommandText.Contains("INSERT INTO \"SessionLogTurnStringLists\"", StringComparison.Ordinal) ? "field"
                : command.CommandText.Contains("INSERT INTO \"SessionLogProcessingDialogs\"", StringComparison.Ordinal) ? "dialog"
                : command.CommandText.Contains("INSERT INTO \"SessionLogActions\"", StringComparison.Ordinal) ? "action" : null;
            if (channel is not null)
            {
                trace.Enqueue(new { operation = "Stage", channel, commandId = eventData.CommandId });
                if (point == "after-decision-" + channel && !Fired)
                {
                    Fired = true;
                    await result.DisposeAsync().ConfigureAwait(false);
                    throw new IOException("scripted_storage_failure_" + point.Replace('-', '_'));
                }
            }
            return result;
        }
    }

    private sealed class RollbackWitness(ConcurrentQueue<object> trace) : DbTransactionInterceptor
    {
        public override Task TransactionRolledBackAsync(DbTransaction transaction,
            TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            trace.Enqueue(new { operation = "Rollback", transactionId = eventData.TransactionId });
            return Task.CompletedTask;
        }
    }
}
