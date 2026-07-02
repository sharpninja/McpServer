using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using McpServer.Repl.Host;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using TxnFailureReason = McpServer.TransactionSecurity.Models.TransactionFailureReason;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// TEST-MCP-161 acceptance: REPL TODO create/update workflow mutations are transaction-gated.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TransactionalTodoWorkflowTests
{
    /// <summary>workflow.todo.create executes inside the transaction coordinator and returns only after commit.</summary>
    [Fact]
    public async Task CreateAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsResult()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        var created = CreateItem("ISSUE-42", "Created");
        var mutation = CreateMutation(created);
        inner.CreateAsync(Arg.Any<ITodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(mutation);
        using var handler = new RecordingTodoHandler();
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator();
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        var result = await sut.CreateAsync(CreateRequest("ISSUE-NEW", "Created"), CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("ISSUE-42", result.Item.Id);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("workflow.todo.create", coordinator.Request.OperationName);
        Assert.Contains("\"id\":\"ISSUE-NEW\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
        await inner.Received(1).CreateAsync(Arg.Any<ITodoCreateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        Assert.Empty(handler.Requests);
    }

    /// <summary>workflow.todo.create rollback deletes the canonical server-returned TODO id, including ISSUE-NEW results.</summary>
    [Fact]
    public async Task CreateAsync_WhenCommitFailsAfterMutation_DeletesReturnedCanonicalId()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        var mutation = CreateMutation(CreateItem("ISSUE-42", "Created"));
        inner.CreateAsync(Arg.Any<ITodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(mutation);
        using var handler = new RecordingTodoHandler();
        handler.EnqueueJson(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.CreateAsync(CreateRequest("ISSUE-NEW", "Created"), CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("Rollback completed", exception.Message, StringComparison.Ordinal);
        var delete = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, delete.Method);
        Assert.EndsWith("/mcpserver/todo/ISSUE-42", delete.Path, StringComparison.Ordinal);
    }

    /// <summary>workflow.todo.update pre-mutation coordinator rejection avoids snapshot reads and inner mutation execution.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotReadOrMutate()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        using var handler = new RecordingTodoHandler();
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TxnFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.UpdateAsync("MCP-TODO-001", UpdateRequest(title: "New"), CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("signing failed", exception.Message, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
        await inner.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!, default).ConfigureAwait(true);
    }

    /// <summary>workflow.todo.update restores the full typed-client snapshot when commit fails after mutation.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCommitFailsAfterMutation_RestoresSnapshot()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        var mutation = CreateMutation(CreateItem("MCP-TODO-001", "Updated"));
        inner.UpdateAsync("MCP-TODO-001", Arg.Any<ITodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(mutation);
        using var handler = new RecordingTodoHandler();
        handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(CreateFlatItem("MCP-TODO-001", "Original")));
        handler.EnqueueJson(HttpStatusCode.OK, """{"success":true,"item":{"id":"MCP-TODO-001","title":"Original","section":"Backlog","priority":"high","done":false}}""");
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.UpdateAsync("MCP-TODO-001", UpdateRequest(title: "Updated"), CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.EndsWith("/mcpserver/todo/MCP-TODO-001", handler.Requests[1].Path, StringComparison.Ordinal);
        Assert.Contains("\"reference\":\"review-123\"", handler.Requests[1].Body, StringComparison.Ordinal);
        Assert.Contains("\"phase\":\"phase-a\"", handler.Requests[1].Body, StringComparison.Ordinal);
    }

    /// <summary>workflow.todo.updateSelected uses the selected id and reselects it after rollback.</summary>
    [Fact]
    public async Task UpdateSelectedAsync_WhenCommitFailsAfterMutation_RestoresAndReselects()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        var selection = CreateSelection("MCP-TODO-001");
        var mutation = CreateMutation(CreateItem("MCP-TODO-001", "Updated"));
        inner.CurrentSelection().Returns(selection);
        inner.UpdateAsync(Arg.Any<ITodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(mutation);
        using var handler = new RecordingTodoHandler();
        handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(CreateFlatItem("MCP-TODO-001", "Original")));
        handler.EnqueueJson(HttpStatusCode.OK, """{"success":true,"item":{"id":"MCP-TODO-001","title":"Original","section":"Backlog","priority":"high","done":false}}""");
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.UpdateAsync(UpdateRequest(title: "Updated"), CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Equal("workflow.todo.updateSelected", coordinator.Request?.OperationName);
        await inner.Received(1).UpdateAsync(Arg.Any<ITodoUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).SelectAsync("MCP-TODO-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>workflow.todo.delete pre-mutation coordinator rejection avoids snapshot reads and inner deletion.</summary>
    [Fact]
    public async Task DeleteAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotReadOrMutate()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        using var handler = new RecordingTodoHandler();
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TxnFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.DeleteAsync("MCP-TODO-DELETE-001", CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("signing failed", exception.Message, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
        await inner.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default).ConfigureAwait(true);
    }

    /// <summary>workflow.todo.delete recreates the typed-client snapshot when commit fails after deletion.</summary>
    [Fact]
    public async Task DeleteAsync_WhenCommitFailsAfterMutation_RecreatesSnapshot()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        inner.DeleteAsync("MCP-TODO-DELETE-001", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using var handler = new RecordingTodoHandler();
        handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(CreateFlatItem("MCP-TODO-DELETE-001", "Original")));
        handler.EnqueueJson(HttpStatusCode.Created, """{"success":true,"item":{"id":"MCP-TODO-DELETE-001","title":"Original","section":"Backlog","priority":"high","done":false}}""");
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.DeleteAsync("MCP-TODO-DELETE-001", CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("Rollback completed", exception.Message, StringComparison.Ordinal);
        Assert.Equal("workflow.todo.delete", coordinator.Request?.OperationName);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.EndsWith("/mcpserver/todo", handler.Requests[1].Path, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"MCP-TODO-DELETE-001\"", handler.Requests[1].Body, StringComparison.Ordinal);
        await inner.Received(1).DeleteAsync("MCP-TODO-DELETE-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>workflow.todo.deleteSelected recreates and reselects the selected TODO when rollback succeeds.</summary>
    [Fact]
    public async Task DeleteSelectedAsync_WhenCommitFailsAfterMutation_RecreatesAndReselects()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        var selection = CreateSelection("MCP-TODO-DELETE-SEL-001");
        inner.CurrentSelection().Returns(selection);
        inner.DeleteAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using var handler = new RecordingTodoHandler();
        handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(CreateFlatItem("MCP-TODO-DELETE-SEL-001", "Original")));
        handler.EnqueueJson(HttpStatusCode.Created, """{"success":true,"item":{"id":"MCP-TODO-DELETE-SEL-001","title":"Original","section":"Backlog","priority":"high","done":false}}""");
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var sut = new TransactionalTodoWorkflow(inner, CreateTodoClient(http), coordinator);

        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.DeleteAsync(CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Equal("workflow.todo.deleteSelected", coordinator.Request?.OperationName);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        await inner.Received(1).DeleteAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.Received(1).SelectAsync("MCP-TODO-DELETE-SEL-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>workflow.todo.repairProjection fails closed before the inner workflow while required transactions are active.</summary>
    [Fact]
    public async Task RepairProjectionAsync_WhenRequiredTransactionsActive_FailsClosedBeforeInnerWorkflow()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        using var handler = new RecordingTodoHandler();
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator();
        var sut = new TransactionalTodoWorkflow(
            inner,
            CreateTodoClient(http),
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.RepairProjectionAsync("MCP-TODO-REPAIR-001", CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("not transaction compensated", exception.Message, StringComparison.Ordinal);
        Assert.Null(coordinator.Request);
        Assert.Empty(handler.Requests);
        await inner.DidNotReceive()
            .RepairProjectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>workflow.todo.repairProjection delegates when mutation transactions are not required.</summary>
    [Fact]
    public async Task RepairProjectionAsync_WhenTransactionsNotRequired_DelegatesToInnerWorkflow()
    {
        var inner = Substitute.For<ITodoWorkflow>();
        using var handler = new RecordingTodoHandler();
        using var http = new HttpClient(handler);
        var coordinator = new CapturingCoordinator();
        var sut = new TransactionalTodoWorkflow(
            inner,
            CreateTodoClient(http),
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = false }));

        await sut.RepairProjectionAsync("MCP-TODO-REPAIR-002", CancellationToken.None).ConfigureAwait(true);

        Assert.Null(coordinator.Request);
        Assert.Empty(handler.Requests);
        await inner.Received(1)
            .RepairProjectionAsync("MCP-TODO-REPAIR-002", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static TodoClient CreateTodoClient(HttpClient http)
        => new(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        });

    private static ITodoCreateRequest CreateRequest(string id, string title)
    {
        var request = Substitute.For<ITodoCreateRequest>();
        request.Id.Returns(id);
        request.Title.Returns(title);
        request.Section.Returns("Backlog");
        request.Priority.Returns("high");
        request.Description.Returns(["description"]);
        request.TechnicalDetails.Returns(["technical"]);
        request.DependsOn.Returns([]);
        request.FunctionalRequirements.Returns([]);
        request.TechnicalRequirements.Returns([]);
        return request;
    }

    private static ITodoUpdateRequest UpdateRequest(string? title = null)
    {
        var request = Substitute.For<ITodoUpdateRequest>();
        request.Title.Returns(title);
        return request;
    }

    private static ITodoItem CreateItem(string id, string title)
    {
        var item = Substitute.For<ITodoItem>();
        item.Id.Returns(id);
        item.Title.Returns(title);
        item.Section.Returns("Backlog");
        item.Priority.Returns("high");
        item.Done.Returns(false);
        item.Description.Returns(["description"]);
        item.TechnicalDetails.Returns(["technical"]);
        item.ImplementationTasks.Returns([]);
        item.DependsOn.Returns([]);
        item.FunctionalRequirements.Returns([]);
        item.TechnicalRequirements.Returns([]);
        return item;
    }

    private static ITodoMutationResult CreateMutation(ITodoItem item)
    {
        var result = Substitute.For<ITodoMutationResult>();
        result.Success.Returns(true);
        result.Item.Returns(item);
        return result;
    }

    private static ITodoSelectionState CreateSelection(string id)
    {
        var selection = Substitute.For<ITodoSelectionState>();
        selection.Id.Returns(id);
        selection.Title.Returns("Selected");
        selection.Section.Returns("Backlog");
        selection.Priority.Returns("high");
        selection.Done.Returns(false);
        selection.SelectedAt.Returns(DateTimeOffset.UtcNow);
        return selection;
    }

    private static TodoFlatItem CreateFlatItem(string id, string title)
        => new()
        {
            Id = id,
            Title = title,
            Section = "Backlog",
            Priority = "high",
            Done = false,
            Estimate = "2h",
            Note = "note",
            Description = ["description"],
            TechnicalDetails = ["technical"],
            ImplementationTasks = [new TodoFlatTask { Task = "task", Done = true }],
            CompletedDate = null,
            DoneSummary = null,
            Remaining = "remaining",
            PriorityNote = "priority note",
            Reference = "review-123",
            Phase = "phase-a",
            DependsOn = [],
            FunctionalRequirements = ["FR-MCP-120"],
            TechnicalRequirements = ["TR-MCP-TXN-001"],
        };

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public bool InvokeMutation { get; init; } = true;

        public bool InvokeRollback { get; init; }

        public string Status { get; init; } = "committed";

        public TxnFailureReason Reason { get; init; } = TxnFailureReason.None;

        public string? Message { get; init; }

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            TurnMutationResult? mutationResult = null;
            var rollbackAttempted = false;
            var rollbackSucceeded = false;
            string? rollbackError = null;

            if (InvokeMutation)
            {
                mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
                if (InvokeRollback && mutationResult.RollbackAsync is not null)
                {
                    rollbackAttempted = true;
                    try
                    {
                        await mutationResult.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        rollbackSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        rollbackError = ex.Message;
                    }
                }
            }

            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-test",
                Status = Status,
                Reason = Reason,
                MutationApplied = InvokeMutation,
                MutationResult = mutationResult,
                Message = Message,
                RollbackAttempted = rollbackAttempted,
                RollbackSucceeded = rollbackSucceeded,
                RollbackError = rollbackError,
            };
        }

        public McpServer.TransactionSecurity.Models.TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = false,
                LastReason = TxnFailureReason.None,
                Message = "Turn transactions are available.",
            };
    }

    private sealed class RecordingTodoHandler : HttpMessageHandler, IDisposable
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public List<RecordedRequest> Requests { get; } = [];

        public void EnqueueJson(HttpStatusCode statusCode, string json)
        {
            _responses.Enqueue(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.AbsolutePath ?? string.Empty, body));

            return _responses.Count > 0
                ? _responses.Dequeue()
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"success":true}""", Encoding.UTF8, "application/json"),
                };
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string Body);
}
