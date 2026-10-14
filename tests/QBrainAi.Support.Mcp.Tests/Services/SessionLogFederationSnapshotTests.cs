using System.Text.Json;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Services.FederationAdapters;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-SESSIONLOG-FED-001 / TR-MCP-FED-SESSIONLOG-001 / FR-MCP-103: the session-log federation
/// snapshot carries the full session graph, equal to the sanitized local session-log read.
/// Data: hand-built <see cref="UnifiedSessionLogDto"/> graphs with every session field, every turn
/// field and every child collection populated.
/// </summary>
public sealed class SessionLogFederationSnapshotTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-federation-snapshot";
    private const string SanitizedResponse = "[REDACTED:secret]";
    private const string RawResponse = "raw response before sanitizing";

    private static readonly JsonSerializerOptions CompareOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// TEST-MCP-SESSIONLOG-FED-001 AC-001 and AC-003 (mock gate): the snapshot assertion accepts a
    /// payload equal to the read, and rejects the pre-change partial payload (session scalars and
    /// eight turn fields only) and a payload that carries the unsanitized value the read redacts.
    /// Data: <see cref="BuildFullRead"/> serialized with web defaults.
    /// </summary>
    [Fact]
    public void SnapshotAssertion_AcceptsPayloadEqualToReadAndRejectsPartialOrUnsanitizedPayloads()
    {
        var read = BuildFullRead(SanitizedResponse);
        AssertSnapshotEqualsRead(JsonSerializer.Serialize(read, CompareOptions), read);

        var partial = new
        {
            read.SourceType,
            read.SessionId,
            read.AgentDefinitionId,
            read.Title,
            read.Model,
            read.Started,
            read.LastUpdated,
            read.Status,
            read.TurnCount,
            read.TotalTokens,
            Turns = read.Turns!.Select(turn => new
            {
                turn.RequestId,
                turn.Timestamp,
                turn.Model,
                turn.ModelProvider,
                turn.QueryTitle,
                turn.Status,
                turn.TokenCount,
                turn.FailureNote,
            }),
        };
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertSnapshotEqualsRead(JsonSerializer.Serialize(partial, CompareOptions), read));

        var unsanitized = BuildFullRead(RawResponse);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertSnapshotEqualsRead(JsonSerializer.Serialize(unsanitized, CompareOptions), read));
    }

    /// <summary>
    /// TEST-MCP-SESSIONLOG-FED-001 AC-001, AC-003 and AC-004: the adapter snapshot of a stored session
    /// equals the sanitized local session-log read (every session field, turn field and child
    /// collection, commit files included), carries the sanitizer's replacement instead of the raw
    /// value, and makes no federation data call. Data: SQLite store; <see cref="BuildFullRead"/>
    /// submitted with a response containing "probe-123", which the test sanitizer rule redacts.
    /// </summary>
    [Fact]
    public async Task SnapshotAsync_ExistingSession_EqualsSanitizedLocalReadWithoutFederationCalls()
    {
        using var connection = OpenConnection();
        var dataClient = Substitute.For<IFederationDataClient>();
        await using var provider = BuildProvider(connection, dataClient);
        var source = BuildFullRead("response probe-123");
        await SubmitAsync(provider, source).ConfigureAwait(true);
        var adapter = new SessionLogFederationStateAdapter(provider.GetRequiredService<IServiceScopeFactory>());

        var snapshot = await adapter.SnapshotAsync($"{source.SourceType}/{source.SessionId}", TestContext.Current.CancellationToken).ConfigureAwait(true);

        var read = await ReadSanitizedAsync(provider, source.SourceType!, source.SessionId!).ConfigureAwait(true);
        Assert.NotNull(read);
        Assert.Equal($"response {ProbeReplacement}", read!.Turns!.Single().Response);
        Assert.Equal(2, read.Turns!.Single().Commits!.Single().FilesChanged!.Count);
        AssertSnapshotEqualsRead(snapshot.PayloadJson, read);
        Assert.DoesNotContain("probe-123", snapshot.PayloadJson, StringComparison.Ordinal);
        Assert.Empty(dataClient.ReceivedCalls());
    }

    /// <summary>
    /// TEST-MCP-SESSIONLOG-FED-001 AC-002: a snapshot payload applied through the adapter's ApplyAsync
    /// to a store without that session reproduces it: the target's local read equals the source's.
    /// Data: two SQLite stores; <see cref="BuildFullRead"/> with values the sanitizer leaves unchanged.
    /// </summary>
    [Fact]
    public async Task SnapshotPayload_AppliedToEmptyStore_ReproducesTheSession()
    {
        using var sourceConnection = OpenConnection();
        using var targetConnection = OpenConnection();
        await using var sourceProvider = BuildProvider(sourceConnection, Substitute.For<IFederationDataClient>());
        await using var targetProvider = BuildProvider(targetConnection, Substitute.For<IFederationDataClient>());
        var source = BuildFullRead("plain response");
        await SubmitAsync(sourceProvider, source).ConfigureAwait(true);
        var resourceId = $"{source.SourceType}/{source.SessionId}";
        var ct = TestContext.Current.CancellationToken;

        var snapshot = await new SessionLogFederationStateAdapter(sourceProvider.GetRequiredService<IServiceScopeFactory>())
            .SnapshotAsync(resourceId, ct).ConfigureAwait(true);
        var applied = await new SessionLogFederationStateAdapter(targetProvider.GetRequiredService<IServiceScopeFactory>())
            .ApplyAsync(new FederationStateOperation
            {
                OperationId = "op-a5-round-trip",
                Domain = "session_log",
                ResourceId = resourceId,
                HttpMethod = "POST",
                PayloadJson = snapshot.PayloadJson,
            }, ct).ConfigureAwait(true);

        Assert.True(applied.Applied);
        var sourceRead = await ReadSanitizedAsync(sourceProvider, source.SourceType!, source.SessionId!).ConfigureAwait(true);
        var targetRead = await ReadSanitizedAsync(targetProvider, source.SourceType!, source.SessionId!).ConfigureAwait(true);
        Assert.NotNull(sourceRead);
        Assert.NotNull(targetRead);
        AssertSnapshotEqualsRead(JsonSerializer.Serialize(targetRead, CompareOptions), sourceRead!);
    }

    /// <summary>
    /// TEST-MCP-SESSIONLOG-FED-001 AC-005: a missing session and a deleted session yield PayloadJson
    /// "null" and Version null; an existing session's Version is its explicit version (the submitted
    /// content hash). Data: SQLite store; <see cref="BuildFullRead"/> submitted with content hash "hash-a5".
    /// </summary>
    [Fact]
    public async Task SnapshotAsync_MissingOrDeletedSession_ReturnsNullAndExistingKeepsExplicitVersion()
    {
        using var connection = OpenConnection();
        await using var provider = BuildProvider(connection, Substitute.For<IFederationDataClient>());
        var adapter = new SessionLogFederationStateAdapter(provider.GetRequiredService<IServiceScopeFactory>());
        var ct = TestContext.Current.CancellationToken;

        var missing = await adapter.SnapshotAsync("ClaudeCode/ClaudeCode-20261013T120000Z-missing", ct).ConfigureAwait(true);
        Assert.Equal("null", missing.PayloadJson);
        Assert.Null(missing.Version);

        var source = BuildFullRead("plain response");
        await SubmitAsync(provider, source, contentHash: "hash-a5").ConfigureAwait(true);
        var resourceId = $"{source.SourceType}/{source.SessionId}";
        var existing = await adapter.SnapshotAsync(resourceId, ct).ConfigureAwait(true);
        Assert.Equal("hash-a5", existing.Version);

        await using (var scope = provider.CreateAsyncScope())
        {
            Assert.True(await scope.ServiceProvider.GetRequiredService<ISessionLogService>()
                .DeleteSessionAsync(source.SourceType!, source.SessionId!, ct).ConfigureAwait(true));
        }

        var deleted = await adapter.SnapshotAsync(resourceId, ct).ConfigureAwait(true);
        Assert.Equal("null", deleted.PayloadJson);
        Assert.Null(deleted.Version);
    }

    private const string ProbeReplacement = "[REDACTED:a5-probe]";

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    // TR-MCP-FED-SESSIONLOG-001: the local store, the local session-log service, the outbound sanitizer
    // with one probe rule, and a federation data client double that records any call.
    private static ServiceProvider BuildProvider(SqliteConnection connection, IFederationDataClient dataClient)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddScoped(_ => new WorkspaceContext { WorkspacePath = WorkspacePath });
        services.AddDbContext<McpDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton(Substitute.For<IChangeEventBus>());
        services.AddSingleton(dataClient);
        services.AddSingleton<ISessionLogSanitizer>(new SessionLogSanitizer(MsOptions.Options.Create(new SessionLogSanitizationOptions
        {
            Rules = [new SessionLogRedactionRuleOptions { Id = "a5-probe", Pattern = "probe-[0-9]+" }],
        })));
        services.AddScoped<ISessionLogService>(sp => ActivatorUtilities.CreateInstance<SessionLogService>(sp));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();
        return provider;
    }

    private static async Task SubmitAsync(ServiceProvider provider, UnifiedSessionLogDto dto, string? contentHash = null)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISessionLogService>()
            .SubmitAsync(dto, contentHash: contentHash, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    // TR-MCP-FED-SESSIONLOG-001 AC-001: the expected value is the existing sanitized local read path.
    private static async Task<UnifiedSessionLogDto?> ReadSanitizedAsync(ServiceProvider provider, string sourceType, string sessionId)
    {
        await using var scope = provider.CreateAsyncScope();
        var sanitized = new SessionLogSanitizingService(
            scope.ServiceProvider.GetRequiredService<ISessionLogService>(),
            scope.ServiceProvider.GetRequiredService<ISessionLogSanitizer>());
        return await sanitized.GetAsync(sourceType, sessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    // TR-MCP-FED-SESSIONLOG-001 AC-001: the snapshot payload, read back as the DTO, serializes exactly like the read.
    internal static void AssertSnapshotEqualsRead(string payloadJson, UnifiedSessionLogDto read)
    {
        var snapshot = JsonSerializer.Deserialize<UnifiedSessionLogDto>(payloadJson, CompareOptions);
        Assert.NotNull(snapshot);
        Assert.Equal(JsonSerializer.Serialize(read, CompareOptions), JsonSerializer.Serialize(snapshot, CompareOptions));
    }

    // TR-MCP-FED-SESSIONLOG-001 AC-001: one value in every session field, turn field and child collection.
    internal static UnifiedSessionLogDto BuildFullRead(string response)
        => new()
        {
            SourceType = "ClaudeCode",
            SessionId = "ClaudeCode-20261013T120000Z-fed-snapshot",
            // AgentDefinitionId stays null: SessionLogs.AgentDefinitionId is a foreign key to AgentDefinitions.
            AgentDefinitionId = null,
            AgentSessionId = "agent-session-1",
            AgentSessionTranscriptFile = "transcripts/agent-session-1.jsonl",
            AgentExecutablePath = "/usr/local/bin/claude",
            AgentExecutableVersion = "9.9.9",
            Title = "Federation snapshot session",
            Model = "claude-opus-5-5",
            Started = "2026-10-13T12:00:00+00:00",
            LastUpdated = "2026-10-13T12:30:00+00:00",
            Status = "in_progress",
            TurnCount = 1,
            Tags = ["session-tag"],
            TotalTokens = 1200,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20261013T120100Z-fed-snapshot",
                    Timestamp = "2026-10-13T12:01:00+00:00",
                    QueryText = "query text",
                    QueryTitle = "query title",
                    Response = response,
                    Interpretation = "interpretation",
                    Status = "completed",
                    Model = "claude-opus-5-5",
                    ModelProvider = "Anthropic",
                    TokenCount = 1200,
                    FailureNote = "failure note",
                    Score = 0.9,
                    IsPremium = true,
                    RawContext = JsonSerializer.SerializeToElement(new { tool = "raw-context" }),
                    OriginalEntry = JsonSerializer.SerializeToElement(new { entry = "original" }),
                    PlanFile = "docs/plans/PLAN-SESSIONTRIAGEFIX-001.md",
                    TodoId = "PLAN-SESSIONTRIAGEFIX-001",
                    Actions = [new UnifiedActionDto { Order = 1, Description = "edit", Type = "edit", Status = "completed", FilePath = "src/a.cs" }],
                    Tags = ["turn-tag"],
                    ContextList = ["docs/context.md"],
                    ProcessingDialog = [new ProcessingDialogItemDto { Timestamp = "2026-10-13T12:01:30+00:00", Role = "model", Content = "thinking", Category = "reasoning" }],
                    Commits = [new SessionLogCommitDto { Sha = "abc123", Branch = "main", Message = "commit", Author = "Payton", Timestamp = "2026-10-13T12:02:00+00:00", FilesChanged = ["src/a.cs", "src/b.cs"] }],
                    DesignDecisions = ["decision"],
                    RequirementsDiscovered = ["FR-MCP-103"],
                    FilesModified = ["src/a.cs"],
                    Blockers = ["blocker"],
                },
            ],
        };
}
