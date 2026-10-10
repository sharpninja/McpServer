using System.Net;
using System.Text;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-REQRECOVERY-001: REPL recovery commands delegate to the requirements client routes.
/// </summary>
public sealed class RequirementsRecoveryWorkflowTests
{
    private static readonly McpServerClientOptions Options = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>planRecovery posts mode dry-run and returns the planned body.</summary>
    [Fact]
    public async Task PlanRecoveryAsync_PostsDryRun()
    {
        var handler = new StatusHandler(HttpStatusCode.OK, """{"idempotencyKey":"recover-001","status":"planned","applied":false,"items":[]}""");
        using var http = new HttpClient(handler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));

        var result = await workflow.PlanRecoveryAsync(Sample(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("planned", result.Status);
        Assert.Contains("/mcpserver/requirements/recovery", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"dry-run\"", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>applyRecovery posts mode apply.</summary>
    [Fact]
    public async Task ApplyRecoveryAsync_PostsApply()
    {
        var handler = new StatusHandler(HttpStatusCode.OK, """{"idempotencyKey":"recover-001","status":"applied","applied":true,"replay":false,"items":[]}""");
        using var http = new HttpClient(handler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));

        var result = await workflow.ApplyRecoveryAsync(Sample(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Applied);
        Assert.Contains("\"mode\":\"apply\"", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>getRecovery uses the idempotency key route. 404, 409, and 503 stay typed.</summary>
    [Fact]
    public async Task GetAndFailures_UseRecoveryContract()
    {
        var ok = new StatusHandler(HttpStatusCode.OK, """{"idempotencyKey":"recover-001","status":"applied","applied":true,"items":[]}""");
        using var http = new HttpClient(ok);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));
        var stored = await workflow.GetRecoveryAsync("recover-001", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("recover-001", stored.IdempotencyKey);
        Assert.Contains("/mcpserver/requirements/recovery/recover-001", ok.LastRequestUri, StringComparison.Ordinal);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new RequirementsWorkflow(new RequirementsClient(new HttpClient(new StatusHandler(HttpStatusCode.NotFound, """{"error":"not_found","message":"Requirements recovery run 'missing' was not found."}""")), Options))
                .GetRecoveryAsync("missing", TestContext.Current.CancellationToken)).ConfigureAwait(true);

        var conflict = await Assert.ThrowsAsync<McpConflictException>(() =>
            new RequirementsWorkflow(new RequirementsClient(new HttpClient(new StatusHandler(HttpStatusCode.Conflict, """{"error":"conflict","message":"Requirements recovery idempotency key 'recover-001' conflicts with a stored payload."}""")), Options))
                .ApplyRecoveryAsync(Sample(), TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Equal("conflict", ReplMcpErrorClassifier.FromException(conflict).Code);

        var unavailable = await Assert.ThrowsAsync<McpServerException>(() =>
            new RequirementsWorkflow(new RequirementsClient(new HttpClient(new StatusHandler(HttpStatusCode.ServiceUnavailable, """{"error":"backend_unavailable","code":"backend_unavailable","message":"The storage backend is currently unreachable.","retryable":true}""")), Options))
                .ApplyRecoveryAsync(Sample(), TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Equal("backend_unavailable", ReplMcpErrorClassifier.FromException(unavailable).Code);
        Assert.True(ReplMcpErrorClassifier.FromException(unavailable).Retryable);
        Assert.Equal("backend_unavailable", unavailable.ErrorCode);
        Assert.True(unavailable.Retryable);

        var pendingMigration = await Assert.ThrowsAsync<McpServerException>(() =>
            new RequirementsWorkflow(new RequirementsClient(new HttpClient(new StatusHandler(HttpStatusCode.ServiceUnavailable, """{"error":"persistence_error","code":"persistence_error","message":"SessionLogs schema is pending migration.","retryable":false}""")), Options))
                .ApplyRecoveryAsync(Sample(), TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Equal("persistence_error", pendingMigration.ErrorCode);
        Assert.False(pendingMigration.Retryable);
        Assert.Equal("persistence_error", ReplMcpErrorClassifier.FromException(pendingMigration).Code);
        Assert.False(ReplMcpErrorClassifier.FromException(pendingMigration).Retryable);
    }

    private static RequirementsRecoveryRequest Sample() => new()
    {
        IdempotencyKey = "recover-001",
        Items = [new RequirementsRecoveryItem { Kind = "fr", Id = "FR-MCP-REQRECOVERY-001", Title = "Recover", Body = "Atomic apply" }],
    };

    private sealed class StatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StatusHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public string LastRequestUri { get; private set; } = string.Empty;

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString() ?? string.Empty;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
