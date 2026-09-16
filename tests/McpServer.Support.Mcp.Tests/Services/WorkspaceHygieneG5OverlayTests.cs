using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using McpServer.Client;
using McpServer.Cqrs.Mvvm;
using McpServer.Repl.Core;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Overlay G5 hygiene: named tests against shipped
/// <see cref="WorkspaceValidationService"/>. TEST-MCP-HYGIENE-001 through 005.
/// </summary>
public sealed class WorkspaceHygieneG5OverlayTests : IDisposable
{
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Isolated in-memory SQLite workspace.</summary>
    public WorkspaceHygieneG5OverlayTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "hy-g5", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>TEST-MCP-HYGIENE-001: result contract fields.</summary>
    [Fact]
    public async Task WorkspaceValidation_ResultContract_HasRequiredFields()
    {
        var result = await CreateService().ValidateAsync(new WorkspaceValidationRequest(), TestContext.Current.CancellationToken);
        Assert.True(result.Success);
        Assert.Equal(WorkspaceValidationRuleRegistry.Version, result.RuleRegistryVersion);
        Assert.Equal(_workspace, result.WorkspaceId);
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
    [Fact]
    public async Task Rule_InProgressTurn_OlderThan48h_UtcClock()
    {
        SeedStaleTurn(DateTimeOffset.UtcNow.AddHours(-49));
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
    [Fact]
    public async Task Rule_StaleThresholdOverride_BoundedAuthenticatedRecorded()
    {
        SeedStaleTurn(DateTimeOffset.UtcNow.AddHours(-10));
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

    /// <summary>TEST-MCP-HYGIENE-005: REST, REPL, Director, plugin expose the same validation.</summary>
    [Fact]
    public async Task Parity_RestDirectorReplPlugin_SameRuleCodesAndCounts()
    {
        Assert.Contains("ValidateAsync", typeof(WorkspaceValidationController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(method => method.Name));
        Assert.Equal("workspace.validate", WorkspaceValidationCommandShapes.ValidateMethod);
        Assert.Equal("validate-workspace", WorkspaceValidationDirectorCommands.Validate);
        var toolNames = typeof(FwhMcpTools).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        Assert.Contains("workspace_validate", toolNames);
        var skill = File.ReadAllText(Path.Combine(FindRepoRoot(), "plugins", "core", "skills", "workspace-validation", "SKILL.md"));
        Assert.Contains("workspace.validate", skill, StringComparison.Ordinal);
        Assert.DoesNotContain("workflow.workspace.repair", skill, StringComparison.Ordinal);

        SeedTodo("TODO-HY-PARITY", done: true, doneSummary: null, remaining: null);
        var service = CreateService();
        var request = new WorkspaceValidationRequest();
        var expected = await service.ValidateAsync(request, TestContext.Current.CancellationToken);

        var rest = await new WorkspaceValidationController(service).ValidateAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(expected.TotalFindings, rest.TotalFindings);
        Assert.Equal(expected.ErrorCount, rest.ErrorCount);
        Assert.Equal(expected.WarningCount, rest.WarningCount);
        AssertParity(expected, rest);

        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            workspaceValidationWorkflow: CreateProductionWorkflow(service));
        var envelope = await dispatcher.DispatchAsync(new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-g5-hygiene",
                Method = WorkspaceValidationCommandShapes.ValidateMethod,
                Params = new Dictionary<string, object?>(),
            },
        }, TestContext.Current.CancellationToken);
        Assert.Equal("result", envelope.Type);
        var repl = Assert.IsType<McpServer.Client.Models.WorkspaceValidationResult>((envelope.Payload as ResultPayload)!.Result);
        AssertParity(expected, repl);

        var command = new WorkspaceValidationDirectorCommand(new WorkspaceValidationDirectorExecutor(service));
        await command.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        var director = Assert.IsType<WorkspaceValidationResult>(command.Result);
        Assert.Equal(expected.TotalFindings, director.TotalFindings);
        Assert.Equal(expected.ErrorCount, director.ErrorCount);
        Assert.Equal(expected.WarningCount, director.WarningCount);
        AssertParity(expected, director);

        var pluginJson = await InvokePluginValidateAsync(service, TestContext.Current.CancellationToken);
        var plugin = System.Text.Json.JsonSerializer.Deserialize<WorkspaceValidationResult>(pluginJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(plugin);
        Assert.Equal(expected.TotalFindings, plugin!.TotalFindings);
        Assert.Equal(expected.ErrorCount, plugin.ErrorCount);
        Assert.Equal(expected.WarningCount, plugin.WarningCount);
        AssertParity(expected, plugin);

        var recorder = new RecordingValidationHandler();
        using var http = new HttpClient(recorder);
        var shippedWorkflow = new WorkspaceValidationWorkflow(new WorkspaceValidationClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        }));
        await shippedWorkflow.ValidateAsync(new Dictionary<string, object?>
        {
            ["ruleCodes"] = new[] { "todo_done_missing_summary" },
            ["offset"] = 0,
            ["limit"] = 1,
            ["staleTurnThresholdHours"] = 24d,
        }, TestContext.Current.CancellationToken);
        Assert.Contains("/mcpserver/workspace-validation/validate", recorder.Path, StringComparison.Ordinal);
        Assert.Contains("todo_done_missing_summary", recorder.Body, StringComparison.Ordinal);
        Assert.Contains("\"limit\":1", recorder.Body, StringComparison.Ordinal);
        Assert.Contains("staleTurnThresholdHours", recorder.Body, StringComparison.Ordinal);
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

    private WorkspaceValidationService CreateService()
        => new(CreateDb(), TimeProvider.System);

    private McpDbContext CreateDb()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

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

    private void SeedTodo(string id, bool done, string? doneSummary, string? remaining)
    {
        using var db = CreateDb();
        db.TodoItems.Add(new TodoItemEntity
        {
            Id = id,
            Title = id,
            Section = "overlay",
            Priority = "high",
            WorkspaceId = _workspace,
            Done = done,
            DoneSummary = doneSummary,
            Remaining = remaining,
        });
        db.SaveChanges();
    }

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

    /// <summary>TEST-MCP-HYGIENE-003: triage statuses come from the live triage domain, queries are batched.</summary>
    [Fact]
    public void ValidationService_UsesLiveTriageDomainAndBatchedQueries()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "McpServer.Services", "Services", "WorkspaceValidationService.cs"));
        Assert.Contains("TriageService.IsNonTerminalStatus", source, StringComparison.Ordinal);
        Assert.Contains("TriageService.IsFailedStatus", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NonTerminalTriageStatuses", source, StringComparison.Ordinal);
        Assert.Contains("QueryBatchSize", source, StringComparison.Ordinal);
        Assert.Contains("Take(QueryBatchSize)", source, StringComparison.Ordinal);
    }

    private static void AssertParity(WorkspaceValidationResult expected, WorkspaceValidationResult actual)
    {
        Assert.Equal(expected.StaleTurnThresholdHours, actual.StaleTurnThresholdHours);
        Assert.Equal(expected.TotalFindings, actual.TotalFindings);
        Assert.Equal(expected.ErrorCount, actual.ErrorCount);
        Assert.Equal(expected.WarningCount, actual.WarningCount);
        Assert.Equal(
            expected.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId),
            actual.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId));
    }

    private static void AssertParity(WorkspaceValidationResult expected, McpServer.Client.Models.WorkspaceValidationResult actual)
    {
        Assert.Equal(expected.StaleTurnThresholdHours, actual.StaleTurnThresholdHours);
        Assert.Equal(expected.TotalFindings, actual.TotalFindings);
        Assert.Equal(expected.ErrorCount, actual.ErrorCount);
        Assert.Equal(expected.WarningCount, actual.WarningCount);
        Assert.Equal(
            expected.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId),
            actual.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId));
    }

    private static WorkspaceValidationWorkflow CreateProductionWorkflow(IWorkspaceValidationService service)
    {
        var http = new HttpClient(new ServiceForwardingValidationHandler(service))
        {
            BaseAddress = new Uri("http://localhost:7147/"),
        };
        return new WorkspaceValidationWorkflow(new WorkspaceValidationClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        }));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs", "Project")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }

    private async Task<string> InvokePluginValidateAsync(IWorkspaceValidationService service, CancellationToken cancellationToken)
    {
        var tools = (FwhMcpTools)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FwhMcpTools));
        var ingestion = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _workspace });
        var resolver = new TodoServiceResolver(Substitute.For<ITodoService>(), ingestion, Substitute.For<ITodoServiceFactory>());
        var accessor = new WorkspaceServiceAccessor(resolver, new Microsoft.AspNetCore.Http.HttpContextAccessor(), ingestion);
        typeof(FwhMcpTools).GetField("_workspaceValidationService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, service);
        typeof(FwhMcpTools).GetField("_workspaceContext", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, new WorkspaceContext { WorkspacePath = _workspace });
        typeof(FwhMcpTools).GetField("_httpContextAccessor", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        typeof(FwhMcpTools).GetField("_workspaceAccessor", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, accessor);
        typeof(FwhMcpTools).GetField("_db", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, CreateDb());
        return await tools.WorkspaceValidate(_workspace, cancellationToken: cancellationToken);
    }

    /// <summary>Records the shipped client HTTP body for workflow contract proof.</summary>
    private sealed class RecordingValidationHandler : HttpMessageHandler
    {
        public string Path { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath ?? string.Empty;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success":true,"httpStatus":200,"findings":[],"totalFindings":0,"exitCode":0,"errorCount":0,"warningCount":0}""", Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>Production client HTTP path that forwards to the shipped validation service.</summary>
    private sealed class ServiceForwardingValidationHandler(IWorkspaceValidationService service) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = request.Content is null
                ? "{}"
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var incoming = System.Text.Json.JsonSerializer.Deserialize<McpServer.Client.Models.WorkspaceValidationRequest>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new McpServer.Client.Models.WorkspaceValidationRequest();
            var result = await service.ValidateAsync(
                    new WorkspaceValidationRequest
                    {
                        Authenticated = incoming.Authenticated,
                        StaleTurnThresholdHours = incoming.StaleTurnThresholdHours,
                        RuleCodes = incoming.RuleCodes,
                        Offset = incoming.Offset,
                        Limit = incoming.Limit,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            var payload = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
        }
    }
}
