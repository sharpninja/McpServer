using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using QBrainAi.Client;
using QBrainAi.Cqrs.Mvvm;
using QBrainAi.Repl.Core;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Overlay G5 hygiene: named tests against shipped
/// <see cref="WorkspaceValidationService"/>. TEST-MCP-HYGIENE-001 through 005.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): CreateService hands every service the class's settable clock instead of TimeProvider.System, the two stale-turn cases seed from it, and the D3 binding was added (hostile validation round 12 finding R12-F2, S3 and S4); the assertions are unchanged.</description></item>
/// <item><term>2026-11-06</term><description>FR-MCP-HYGIENE-001, FR-MCP-HYGIENE-002, FR-MCP-HYGIENE-003, FR-MCP-HYGIENE-004 and FR-MCP-HYGIENE-005 (every criterion of each; they carry no criterion ids): cites the criteria whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TEST-MCP-HYGIENE-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (Parity_RestDirectorReplPlugin_SameRuleCodesAndCounts and ValidationService_UsesLiveTriageDomainAndBatchedQueries) and the helpers only they use (AssertParity, CreateProductionWorkflow, FindRepoRoot, InvokePluginValidateAsync, RecordingValidationHandler and ServiceForwardingValidationHandler) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S8, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_workspace, _connection, _options, _clock, .ctor, Dispose, CreateService, CreateDb and SeedTodo) moved once into the abstract base class WorkspaceHygieneG5OverlayTestsBase in Services/WorkspaceHygieneG5OverlayTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class WorkspaceHygieneG5OverlayTests : WorkspaceHygieneG5OverlayTestsBase, IDisposable
{
    /// <summary>TEST-MCP-HYGIENE-001: result contract fields.</summary>
    [Fact]
    public async Task WorkspaceValidation_ResultContract_HasRequiredFields()
    {
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.True(result.Success);
        Assert.Equal(WorkspaceValidationRuleRegistry.Version, result.RuleRegistryVersion);
        Assert.Equal(ExpectedWorkspaceKey.Of(_workspace), result.WorkspaceId);
        Assert.Equal(48, result.StaleTurnThresholdHours);
        Assert.NotEqual(default, result.RunUtc);
        Assert.NotNull(result.Findings);
        Assert.NotNull(result.Diagnostics);
    }

    /// <summary>TEST-MCP-HYGIENE-001: clean workspace has zero findings.</summary>
    [Fact]
    public async Task WorkspaceValidation_CleanWorkspace_ZeroFindings()
    {
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Empty(result.Findings);
        Assert.Equal(0, result.TotalFindings);
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>TEST-MCP-HYGIENE-001: unknown rule codes are diagnostics, not silent skips.</summary>
    [Fact]
    public async Task WorkspaceValidation_UnknownRuleCode_Diagnostic()
    {
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest { RuleCodes = ["not-a-rule"] }, TestContext.Current.CancellationToken);
        Assert.Contains(result.Diagnostics, item => item.Contains("unknown_rule", StringComparison.Ordinal));
    }

    /// <summary>TEST-MCP-HYGIENE-002: missing AC is a finding.</summary>
    [Fact]
    public async Task Rule_FrTrTest_MissingAcceptanceCriteria_FindsRecord()
    {
        SeedRequirement("fr", "FR-HY-001");
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "missing_acceptance_criteria" && item.RecordId == "FR-HY-001");
    }

    /// <summary>TEST-MCP-HYGIENE-002: TR with no FR mapping.</summary>
    [Fact]
    public async Task Rule_TrWithNoFr_Orphan()
    {
        SeedRequirement("tr", "TR-HY-001");
        AddCriterion("tr", "TR-HY-001");
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "tr_orphan_no_fr" && item.RecordId == "TR-HY-001");
    }

    /// <summary>TEST-MCP-HYGIENE-002: FR missing TR or TEST.</summary>
    [Fact]
    public async Task Rule_FrMissingTrOrTest_Orphan()
    {
        SeedRequirement("fr", "FR-HY-002");
        AddCriterion("fr", "FR-HY-002");
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "fr_missing_tr_or_test" && item.RecordId == "FR-HY-002");
    }

    /// <summary>TEST-MCP-HYGIENE-002: TEST with no FR mapping.</summary>
    [Fact]
    public async Task Rule_TestWithNoFr_Orphan()
    {
        SeedRequirement("test", "TEST-HY-001");
        AddCriterion("test", "TEST-HY-001");
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "test_orphan_no_fr" && item.RecordId == "TEST-HY-001");
    }

    /// <summary>TEST-MCP-HYGIENE-002: broken or duplicate mappings.</summary>
    [Fact]
    public async Task Rule_BrokenOrDuplicateMapping()
    {
        SeedRequirement("fr", "FR-HY-003");
        AddCriterion("fr", "FR-HY-003");
        using (var db = CreateDb())
        {
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
            db.RequirementTraceabilityLinks.Add(Link("FR-HY-003", "tr", "TR-MISSING"));
            db.SaveChanges();
        }

        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "broken_or_duplicate_mapping");
    }

    /// <summary>TEST-MCP-HYGIENE-003: done=true with incomplete tasks.</summary>
    [Fact]
    public async Task Rule_DoneTrue_IncompleteTasks()
    {
        SeedTodo("TODO-HY-001", done: true, doneSummary: "done", remaining: null);
        SeedTask("TODO-HY-001", done: false);
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "todo_done_incomplete_tasks" && item.RecordId == "TODO-HY-001");
    }

    /// <summary>TEST-MCP-HYGIENE-003: done=false with all tasks complete.</summary>
    [Fact]
    public async Task Rule_DoneFalse_AllTasksComplete()
    {
        SeedTodo("TODO-HY-002", done: false, doneSummary: null, remaining: null);
        SeedTask("TODO-HY-002", done: true);
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "todo_open_all_tasks_complete" && item.RecordId == "TODO-HY-002");
    }

    /// <summary>TEST-MCP-HYGIENE-003: done without doneSummary.</summary>
    [Fact]
    public async Task Rule_DoneWithoutDoneSummary()
    {
        SeedTodo("TODO-HY-003", done: true, doneSummary: null, remaining: null);
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "todo_done_missing_summary" && item.RecordId == "TODO-HY-003");
    }

    /// <summary>TEST-MCP-HYGIENE-003: remaining text contradicts completion.</summary>
    [Fact]
    public async Task Rule_RemainingContradictsCompletion()
    {
        SeedTodo("TODO-HY-004", done: true, doneSummary: "shipped", remaining: "still more work");
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "todo_remaining_contradicts_completion" && item.Severity == "Warning");
    }

    /// <summary>TEST-MCP-HYGIENE-003: missing dependency target.</summary>
    [Fact]
    public async Task Rule_MissingDependencyTarget()
    {
        SeedTodo("TODO-HY-005", done: false, doneSummary: null, remaining: null);
        using (var db = CreateDb())
        {
            db.TodoItemListItems.Add(new TodoItemListItemEntity
            {
                WorkspaceId = _workspace,
                TodoId = "TODO-HY-005",
                ListType = "DependsOn",
                Value = "TODO-DOES-NOT-EXIST",
            });
            db.SaveChanges();
        }

        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "todo_missing_dependency");
    }

    /// <summary>TEST-MCP-HYGIENE-003: missing referenced requirement.</summary>
    [Fact]
    public async Task Rule_MissingReferencedRequirementId()
    {
        SeedTodo("TODO-HY-006", done: false, doneSummary: null, remaining: null);
        using (var db = CreateDb())
        {
            db.TodoItemListItems.Add(new TodoItemListItemEntity
            {
                WorkspaceId = _workspace,
                TodoId = "TODO-HY-006",
                ListType = "FunctionalRequirement",
                Value = "FR-DOES-NOT-EXIST",
            });
            db.SaveChanges();
        }

        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "todo_missing_requirement");
    }

    /// <summary>TEST-MCP-HYGIENE-004: stale in_progress turn using UTC timestamps.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): the turn is seeded 49 hours before the class's settable clock, which the service reads, instead of DateTimeOffset.UtcNow; the assertion is unchanged (hostile validation round 12 finding R12-F2).</remarks>
    [Fact]
    public async Task Rule_InProgressTurn_OlderThan48h_UtcClock()
    {
        SeedStaleTurn(_clock.GetUtcNow().AddHours(-49));
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "session_stale_in_progress");
    }

    /// <summary>TEST-MCP-HYGIENE-004: triage non-terminal uses live domain statuses; failed is distinct.</summary>
    [Fact]
    public async Task Rule_TriageNonTerminal_UsesLiveDomainEnum()
    {
        SeedTriage("triage-pending", "collecting");
        SeedTriage("triage-failed", "failed");
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.Contains(result.Findings, item => item.RuleCode == "triage_nonterminal" && item.RecordId == "triage-pending");
        Assert.Contains(result.Findings, item => item.RuleCode == "triage_processing_failed" && item.RecordId == "triage-failed");
        Assert.DoesNotContain(result.Findings, item => item.RuleCode == "triage_nonterminal" && item.RecordId == "triage-failed");
    }

    /// <summary>TEST-MCP-HYGIENE-004: threshold override is bounded, authenticated, and recorded.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): the turn is seeded 10 hours before the class's settable clock, which the service reads, instead of DateTimeOffset.UtcNow; the assertions are unchanged (hostile validation round 12 finding R12-F2).</remarks>
    [Fact]
    public async Task Rule_StaleThresholdOverride_BoundedAuthenticatedRecorded()
    {
        SeedStaleTurn(_clock.GetUtcNow().AddHours(-10));
        var tooWide = await CreateService().ValidateAsync(new WorkspaceValidationRequest { StaleTurnThresholdHours = 200 }, TestContext.Current.CancellationToken);
        Assert.Equal("threshold_out_of_range", tooWide.ErrorCode);
        var recorded = await CreateService().ValidateAsync(new WorkspaceValidationRequest { StaleTurnThresholdHours = 8 }, TestContext.Current.CancellationToken);
        Assert.True(recorded.ThresholdOverrideRecorded);
        Assert.Equal(8, recorded.StaleTurnThresholdHours);
        Assert.Contains(recorded.Findings, item => item.RuleCode == "session_stale_in_progress");
    }

    /// <summary>TEST-MCP-HYGIENE-004: unauthenticated validate is rejected.</summary>
    [Fact]
    public async Task Validation_AuthRequired()
    {
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest { Authenticated = false }, TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        Assert.Equal(401, result.HttpStatus);
        Assert.Equal("unauthenticated", result.ErrorCode);
    }

    /// <summary>TEST-MCP-HYGIENE-004: cancellation is honored.</summary>
    [Fact]
    public async Task Validation_CancellationHonored()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService().ValidateAsync(new WorkspaceValidationRequest(), cts.Token));
    }

    /// <summary>TEST-MCP-HYGIENE-004: large workspaces paginate.</summary>
    [Fact]
    public async Task Validation_LargeWorkspace_Paginates()
    {
        SeedRequirement("fr", "FR-PAGE-1");
        SeedRequirement("fr", "FR-PAGE-2");
        SeedRequirement("fr", "FR-PAGE-3");
        var page = await CreateService().ValidateAsync(new WorkspaceValidationRequest { Limit = 1, Offset = 0 }, TestContext.Current.CancellationToken);
        Assert.True(page.TotalFindings >= 3);
        Assert.Single(page.Findings);
        var next = await CreateService().ValidateAsync(new WorkspaceValidationRequest { Limit = 1, Offset = 1 }, TestContext.Current.CancellationToken);
        Assert.NotEqual(page.Findings[0].RecordId, next.Findings[0].RecordId);
    }

    /// <summary>TEST-MCP-HYGIENE-005: Director exit 1 when Error findings exist.</summary>
    [Fact]
    public async Task Director_Exit1_WhenErrorSeverityPresent()
    {
        SeedTodo("TODO-HY-ERR", done: true, doneSummary: null, remaining: null);
        var command = new WorkspaceValidationDirectorCommand(new WorkspaceValidationDirectorExecutor(CreateService()));
        await command.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        var result = Assert.IsType<WorkspaceValidationResult>(command.Result);
        Assert.Equal(1, result.ExitCode);
        Assert.True(result.ErrorCount > 0);
    }

    /// <summary>TEST-MCP-HYGIENE-005: Director exit 0 when Warning-only.</summary>
    [Fact]
    public async Task Director_Exit0_WhenWarningOnly()
    {
        SeedTodo("TODO-HY-WARN", done: true, doneSummary: "done", remaining: "leftover");
        var command = new WorkspaceValidationDirectorCommand(new WorkspaceValidationDirectorExecutor(CreateService()));
        await command.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        var result = Assert.IsType<WorkspaceValidationResult>(command.Result);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(result.WarningCount > 0);
    }

    /// <summary>TEST-MCP-HYGIENE-005: validation never auto-repairs.</summary>
    [Fact]
    public async Task Validation_NeverAutoRepairs()
    {
        SeedTodo("TODO-HY-KEEP", done: true, doneSummary: null, remaining: null);
        await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        using var verify = CreateDb();
        var todo = Assert.Single(verify.TodoItems);
        Assert.True(todo.Done);
        Assert.True(string.IsNullOrWhiteSpace(todo.DoneSummary));
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding, decisions D3 of
    /// <see cref="QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests"/>: for each scripted reference time and
    /// each contract row, one in-progress turn recorded at the row's time raises the stale-turn finding exactly when the
    /// contract says, under the default 48 hour threshold and under an 8 hour override (no finding exactly at the
    /// threshold, because only a turn before the cutoff is stale). Test data: the contract's times; one turn per row.
    /// </summary>
    /// <returns>A task that completes when every row has been asserted.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added for hostile validation round 12 finding R12-F2 (the reviewer's proposed WorkspaceHygiene_UsesScriptedStaleThreshold).</remarks>
    [Fact]
    public async Task WorkspaceHygiene_UsesScriptedStaleThreshold()
    {
        foreach (var (name, threshold) in new (string Name, int? Threshold)[]
                 {
                     ("D3 hygiene stale turn, default threshold", null),
                     ("D3 hygiene stale turn, override threshold", 8),
                 })
        {
            await QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests.AssertDecisionAsync(name, async (reference, recorded) =>
            {
                _clock.Set(reference);
                var key = Guid.NewGuid().ToString("N")[..12];
                SeedInProgressTurn(recorded, key);
                var result = await CreateService().ValidateAsync(
                    new WorkspaceValidationRequest { StaleTurnThresholdHours = threshold },
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
                Assert.True(result.Success, result.ErrorCode);
                return result.Findings.Any(item => item.RuleCode == "session_stale_in_progress" && item.RecordId == "req-" + key);
            }).ConfigureAwait(true);
        }
    }

    private void SeedRequirement(string kind, string id)
    {
        using var db = CreateDb();
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspace,
            Kind = kind,
            Id = id,
            Title = id,
            Body = "body",
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = "2026-09-10T00:00:00Z",
            UpdatedAtUtc = "2026-09-10T00:00:00Z",
        });
        db.SaveChanges();
    }

    private void AddCriterion(string kind, string id)
    {
        using var db = CreateDb();
        db.RequirementAcceptanceCriteria.Add(new RequirementAcceptanceCriterionEntity
        {
            WorkspaceId = _workspace,
            RequirementKind = kind,
            RequirementId = id,
            CriterionId = id + "-ac1",
            Text = "criterion",
        });
        db.SaveChanges();
    }

    private RequirementTraceabilityLinkEntity Link(string frId, string targetKind, string targetId)
        => new()
        {
            WorkspaceId = _workspace,
            SourceKind = "fr",
            FrId = frId,
            TargetKind = targetKind,
            TargetId = targetId,
            CreatedAtUtc = "2026-09-10T00:00:00Z",
        };

    private void SeedTask(string todoId, bool done)
    {
        using var db = CreateDb();
        db.TodoItemTasks.Add(new TodoItemTaskEntity
        {
            WorkspaceId = _workspace,
            TodoId = todoId,
            Task = "task",
            Done = done,
        });
        db.SaveChanges();
    }

    private void SeedStaleTurn(DateTimeOffset timestamp)
    {
        using var db = CreateDb();
        var session = new SessionLogEntity
        {
            WorkspaceId = _workspace,
            SourceType = "GrokCode",
            SessionId = "GrokCode-20260910T000000Z-hygiene",
        };
        db.SessionLogs.Add(session);
        db.SaveChanges();
        db.SessionLogTurns.Add(new SessionLogTurnEntity
        {
            WorkspaceId = _workspace,
            SessionLogId = session.Id,
            RequestId = "req-stale",
            Status = "in_progress",
            Timestamp = timestamp,
            PlanFile = "None",
            TodoId = "None",
        });
        db.SaveChanges();
    }

    /// <summary>Seeds one in-progress turn at <paramref name="timestamp"/> in its own session, named by <paramref name="key"/>.</summary>
    /// <param name="timestamp">The turn's recorded time.</param>
    /// <param name="key">The row key; the turn's request id is "req-" followed by it.</param>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended): added for the D3 binding (hostile validation round 12 finding R12-F2).</remarks>
    private void SeedInProgressTurn(DateTimeOffset timestamp, string key)
    {
        using var db = CreateDb();
        var session = new SessionLogEntity
        {
            WorkspaceId = _workspace,
            SourceType = "GrokCode",
            SessionId = "GrokCode-20260910T000000Z-hygiene" + key,
        };
        db.SessionLogs.Add(session);
        db.SaveChanges();
        db.SessionLogTurns.Add(new SessionLogTurnEntity
        {
            WorkspaceId = _workspace,
            SessionLogId = session.Id,
            RequestId = "req-" + key,
            Status = "in_progress",
            Timestamp = timestamp,
            PlanFile = "None",
            TodoId = "None",
        });
        db.SaveChanges();
    }

    private void SeedTriage(string reportId, string status)
    {
        using var db = CreateDb();
        if (!db.TriageGroups.Any(group => group.GroupId == "group-hy"))
        {
            db.TriageGroups.Add(new TriageGroupEntity
            {
                GroupId = "group-hy",
                GroupKey = "group-hy",
                EffectiveWorkspacePath = _workspace,
                Title = "hygiene",
                Summary = "hygiene",
                Status = "collecting",
                WorkspaceId = _workspace,
                FirstReportAtUtc = DateTimeOffset.UtcNow,
            });
        }

        db.TriageReports.Add(new TriageReportEntity
        {
            ReportId = reportId,
            GroupId = "group-hy",
            OriginalWorkspacePath = _workspace,
            EffectiveWorkspacePath = _workspace,
            Title = reportId,
            Summary = "summary",
            Fingerprint = reportId,
            Status = status,
            WorkspaceId = _workspace,
            CreatedUtc = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
    }
}
