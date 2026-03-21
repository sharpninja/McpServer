using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

public sealed class Http500ErrorContractTests : IClassFixture<Http500ErrorContractTests.ErrorContractWebFactory>, IDisposable
{
    private readonly ErrorContractWebFactory _factory;
    private readonly HttpClient _client;

    public Http500ErrorContractTests(ErrorContractWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", factory.GetFullWorkspaceApiKey());
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task TodoMove_WhenSourceDeleteFails_ReturnsStandardized500()
    {
        var response = await _client.PostAsJsonAsync("/mcpserver/todo/TEST-001/move", new
        {
            targetWorkspacePath = _factory.TargetWorkspacePath
        }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HttpErrorPayload>().ConfigureAwait(true);
        Assert.NotNull(payload);
        Assert.Equal(500, payload!.Status);
        Assert.Equal("internal_server_error", payload.Error);
        Assert.Contains("source deletion failed", payload.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MoveAsync", payload.Operation, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
        Assert.DoesNotContain("   at ", payload.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnhandledException_ViaTodoQuery_ReturnsStandardized500WithSanitizedDetail()
    {
        var response = await _client.GetAsync("/mcpserver/todo?keyword=trigger-500").ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HttpErrorPayload>().ConfigureAwait(true);
        Assert.NotNull(payload);
        Assert.Equal(500, payload!.Status);
        Assert.Equal("internal_server_error", payload.Error);
        Assert.Contains("trigger-500", payload.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("abc123", payload.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("letmein", payload.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[REDACTED]", payload.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task VoiceStreaming_PreStreamFailure_ReturnsNon500TypedErrorBeforeStreamStarts()
    {
        var response = await _client.PostAsJsonAsync("/mcpserver/voice/session/session-1/turn/stream", new
        {
            userTranscriptText = "hello"
        }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("pre-stream validation failed", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TodoPromptStream_PostStreamFailure_EmitsStructuredErrorEvent()
    {
        var response = await _client.GetAsync("/mcpserver/todo/TEST-001/prompt/plan", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("event: error", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Stream failed", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record HttpErrorPayload(int Status, string Error, string Message, string Detail, string Operation, string TraceId, DateTimeOffset TimestampUtc);

    public sealed class ErrorContractWebFactory : WebApplicationFactory<McpApiEntryPoint>, IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "mcp-http500-tests-" + Guid.NewGuid().ToString("N")[..8]);
        public string TargetWorkspacePath => Path.Combine(_tempDir, "target-workspace");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var projectDir = Path.Combine(_tempDir, "docs", "Project");
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(Path.Combine(TargetWorkspacePath, "docs", "Project"));
            File.WriteAllText(Path.Combine(projectDir, "TODO.yaml"), SeedYaml);
            File.WriteAllText(Path.Combine(TargetWorkspacePath, "docs", "Project", "TODO.yaml"), "mvp-app:\n  high-priority:\n    - id: TEST-001\n      title: Existing target item\n      done: false\n");

            builder.UseEnvironment("Test");
            builder.UseContentRoot(ResolveContentRoot());
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Mcp:DataSource", ":memory:" },
                    { "DataFolder", _tempDir },
                    { "Mcp:RepoRoot", _tempDir },
                    { "Mcp:TodoFilePath", "docs/Project/TODO.yaml" },
                    { "Mcp:TodoStorage:Provider", "sqlite" },
                    { "Mcp:TodoStorage:SqliteDataSource", "mcp.db" }
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IVoiceConversationService>();
                services.RemoveAll<ITodoPromptService>();
                services.RemoveAll<ITodoServiceFactory>();
                services.RemoveAll<ITodoService>();
                services.RemoveAll<TodoServiceResolver>();
                services.RemoveAll<IWorkspaceService>();

                services.AddSingleton<IVoiceConversationService, FailingVoiceConversationService>();
                services.AddSingleton<ITodoPromptService, FailingTodoPromptService>();
                services.AddSingleton<ITodoServiceFactory, FailingTodoServiceFactory>();
                services.AddSingleton<ITodoService>(sp => sp.GetRequiredService<ITodoServiceFactory>().CreatePrimary());
                services.AddSingleton<TodoServiceResolver>();
                services.AddSingleton<IWorkspaceService>(new FailingWorkspaceService(TargetWorkspacePath));
            });
        }

        private static string ResolveContentRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                var solutionPath = Path.Combine(current.FullName, "McpServer.sln");
                if (File.Exists(solutionPath))
                    return Path.Combine(current.FullName, "src", "McpServer.Support.Mcp");

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the solution root for HTTP 500 contract integration tests.");
        }

        public string GetFullWorkspaceApiKey()
        {
            var tokenService = Services.GetRequiredService<WorkspaceTokenService>();
            return tokenService.GetToken(_tempDir)
                   ?? throw new InvalidOperationException("Workspace full API key was not generated for test host.");
        }

        private new void Dispose()
        {
            base.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private const string SeedYaml = """
            mvp-app:
              high-priority:
                - id: TEST-001
                  title: Test item one
                  done: false
            """;
    }
}

public sealed class FailingWorkspaceService : IWorkspaceService
{
    private readonly string _targetWorkspacePath;

    public FailingWorkspaceService(string targetWorkspacePath)
    {
        _targetWorkspacePath = targetWorkspacePath;
    }

    public Task<WorkspaceListResult> ListAsync(CancellationToken ct = default)
        => Task.FromResult(new WorkspaceListResult([], 0));

    public Task<WorkspaceDto?> GetAsync(string workspacePath, CancellationToken ct = default)
        => Task.FromResult(workspacePath == _targetWorkspacePath
            ? new WorkspaceDto
            {
                WorkspacePath = _targetWorkspacePath,
                Name = "target",
                TodoPath = "docs/Project/TODO.yaml",
                StatusPrompt = string.Empty,
                ImplementPrompt = string.Empty,
                PlanPrompt = string.Empty,
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
            }
            : null);

    public Task<WorkspaceMutationResult> CreateAsync(WorkspaceCreateRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<WorkspaceMutationResult> UpdateAsync(string workspacePath, WorkspaceUpdateRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<WorkspaceMutationResult> DeleteAsync(string workspacePath, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<WorkspaceInitResult> InitAsync(string workspacePath, CancellationToken ct = default) => throw new NotSupportedException();
}

public sealed class FailingTodoServiceFactory : ITodoServiceFactory
{
    private readonly TodoServiceFactory _innerFactory;

    public FailingTodoServiceFactory(
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> options,
        IWriteAuditLog auditLog,
        ILoggerFactory loggerFactory,
        IChangeEventBus? eventBus = null)
    {
        _innerFactory = new TodoServiceFactory(ingestionOptions, options, auditLog, loggerFactory, eventBus);
    }

    public ITodoService CreatePrimary() => new FailingDeleteTodoService(_innerFactory.CreatePrimary());
    public ITodoService CreateForWorkspace(string workspacePath, WorkspaceContext workspaceContext)
        => new PassThroughTodoService(_innerFactory.CreateForWorkspace(workspacePath, workspaceContext));
}

public sealed class PassThroughTodoService : ITodoService
{
    private readonly ITodoService _inner;

    public PassThroughTodoService(ITodoService inner) => _inner = inner;

    public Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken) => _inner.QueryAsync(request, cancellationToken);
    public Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken) => _inner.GetByIdAsync(id, cancellationToken);
    public Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        => _inner.GetAuditAsync(id, limit, offset, cancellationToken);
    public Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
        => _inner.GetProjectionStatusAsync(cancellationToken);
    public Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
        => _inner.RepairProjectionAsync(cancellationToken);
    public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken) => Task.FromResult(new TodoMutationResult(true, null, new TodoFlatItem { Id = request.Id, Title = request.Title, Section = request.Section, Priority = request.Priority, Done = false }));
    public Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken) => _inner.UpdateAsync(id, request, cancellationToken);
    public Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken) => _inner.DeleteAsync(id, cancellationToken);
}

public sealed class FailingDeleteTodoService : ITodoService
{
    private readonly ITodoService _inner;

    public FailingDeleteTodoService(ITodoService inner) => _inner = inner;

    public Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.Keyword, "trigger-500", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unhandled test failure with token=abc123 and password=letmein while processing trigger-500");
        return _inner.QueryAsync(request, cancellationToken);
    }

    public Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken) => _inner.GetByIdAsync(id, cancellationToken);
    public Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        => _inner.GetAuditAsync(id, limit, offset, cancellationToken);
    public Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
        => _inner.GetProjectionStatusAsync(cancellationToken);
    public Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
        => _inner.RepairProjectionAsync(cancellationToken);
    public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken) => _inner.CreateAsync(request, cancellationToken);
    public Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken) => _inner.UpdateAsync(id, request, cancellationToken);
    public Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken)
        => id == "TEST-001"
            ? Task.FromResult(new TodoMutationResult(false, "source deletion failed with token=abc123; password=secret"))
            : _inner.DeleteAsync(id, cancellationToken);
}

public sealed class FailingTodoPromptService : ITodoPromptService
{
    public async IAsyncEnumerable<string> StreamStatusAsync(string id, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "starting";
        await Task.Yield();
        throw new InvalidOperationException("status stream exploded");
    }

    public async IAsyncEnumerable<string> StreamImplementAsync(string id, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "starting";
        await Task.Yield();
        throw new InvalidOperationException("implement stream exploded");
    }

    public async IAsyncEnumerable<string> StreamPlanAsync(string id, string? prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "starting";
        await Task.Yield();
        throw new InvalidOperationException("plan stream exploded");
    }
}

public sealed class FailingVoiceConversationService : IVoiceConversationService
{
    public Task<VoiceSessionCreateResponse> CreateSessionAsync(VoiceSessionCreateRequest? request, CancellationToken cancellationToken = default)
        => Task.FromResult(new VoiceSessionCreateResponse
        {
            SessionId = "session-1",
            Status = "idle",
            Language = request?.Language ?? "en-US",
            ExecutionStrategy = "test"
        });

    public VoiceSessionStatusDto? FindSessionByDevice(string deviceId) => null;

    public Task<VoiceTurnResponse?> SubmitTurnAsync(string sessionId, VoiceTurnRequest request, CancellationToken cancellationToken)
        => Task.FromResult<VoiceTurnResponse?>(null);

    public async IAsyncEnumerable<VoiceTurnStreamEvent> SubmitTurnStreamingAsync(string sessionId, VoiceTurnRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new VoiceTurnStreamEvent { Type = "progress", Message = "started" };
        await Task.Yield();
        throw new InvalidOperationException("stream failed");
    }

    public Task<VoiceInterruptResponse?> InterruptAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult<VoiceInterruptResponse?>(null);
    public Task<bool> SendEscapeAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult(false);
    public Task<VoiceSessionStatusDto?> GetStatusAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult<VoiceSessionStatusDto?>(null);
    public Task<VoiceTranscriptResponse?> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult<VoiceTranscriptResponse?>(null);
    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult(false);
    public Task<bool> SendSessionMessageAsync(string sessionId, string message, CancellationToken cancellationToken)
        => throw new ArgumentException("pre-stream validation failed with apiKey=abc123");
}
