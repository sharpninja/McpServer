using McpServer.Client;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-161: Generic REPL client passthrough blocks known unsafe mutation
/// methods while required transaction gating is active.
/// </summary>
public sealed class ClientMutationPolicyTests
{
    /// <summary>
    /// TEST-MCP-161: A rejected client mutation policy prevents reflection
    /// passthrough from invoking an uncompensated mutation method.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_WhenPolicyRejectsUnsafeClientMutation_DoesNotInvokePassthrough()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var policy = Substitute.For<IClientMutationPolicy>();
        policy.Evaluate("context", "GraphRagDeleteDocumentAsync", Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(ClientMutationPolicyDecision.Reject(
                "mutation_not_transactional",
                "client.context.GraphRagDeleteDocumentAsync is blocked while required turn transactions are active."));
        var sut = new ReplCommandDispatcher(passthrough, clientMutationPolicy: policy);

        var response = await sut.DispatchAsync(
                new YamlEnvelope
                {
                    Type = "request",
                    Payload = new RequestPayload
                    {
                        RequestId = "req-policy-block",
                        Method = "client.context.GraphRagDeleteDocumentAsync",
                        Params = new Dictionary<string, object?> { ["documentId"] = "doc-1" },
                    },
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("error", response.Type);
        var error = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("req-policy-block", error.RequestId);
        Assert.Equal("mutation_not_transactional", error.Code);
        Assert.Contains("GraphRagDeleteDocumentAsync", error.Message, StringComparison.Ordinal);
        _ = passthrough.DidNotReceive().InvokeAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-161: Read-side client methods remain pass-through when the
    /// policy allows the request.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_WhenPolicyAllowsReadClientMethod_InvokesPassthrough()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough
            .InvokeAsync("context", "SearchAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new Dictionary<string, object?> { ["totalCount"] = 0 }));
        var policy = Substitute.For<IClientMutationPolicy>();
        policy.Evaluate("context", "SearchAsync", Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(ClientMutationPolicyDecision.Allow());
        var sut = new ReplCommandDispatcher(passthrough, clientMutationPolicy: policy);

        var response = await sut.DispatchAsync(
                new YamlEnvelope
                {
                    Type = "request",
                    Payload = new RequestPayload
                    {
                        RequestId = "req-policy-allow",
                        Method = "client.context.SearchAsync",
                        Params = new Dictionary<string, object?> { ["query"] = "txn" },
                    },
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("result", response.Type);
        var result = Assert.IsAssignableFrom<IResultPayload>(response.Payload);
        Assert.Equal("req-policy-allow", result.RequestId);
        _ = passthrough.Received(1).InvokeAsync(
            "context",
            "SearchAsync",
            Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-161: The typed TODO analyzer workflow is blocked before invoking
    /// the uncompensated analyzer side-effect path.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_WhenPolicyRejectsTodoAnalyzeRequirements_DoesNotInvokeWorkflow()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var todo = Substitute.For<ITodoWorkflow>();
        var policy = Substitute.For<IClientMutationPolicy>();
        policy.Evaluate("todo", "AnalyzeRequirementsAsync", Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(ClientMutationPolicyDecision.Reject(
                "mutation_not_transactional",
                "workflow.todo.analyzeRequirements is blocked while required turn transactions are active."));
        var sut = new ReplCommandDispatcher(passthrough, todoWorkflow: todo, clientMutationPolicy: policy);

        var response = await sut.DispatchAsync(
                new YamlEnvelope
                {
                    Type = "request",
                    Payload = new RequestPayload
                    {
                        RequestId = "req-policy-analyze",
                        Method = TodoCommandShapes.AnalyzeRequirementsMethod,
                        Params = new Dictionary<string, object?> { ["id"] = "PLAN-TXN-001" },
                    },
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("error", response.Type);
        var error = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("req-policy-analyze", error.RequestId);
        Assert.Equal("mutation_not_transactional", error.Code);
        Assert.Contains("analyzeRequirements", error.Message, StringComparison.Ordinal);
        await todo.DidNotReceiveWithAnyArgs().AnalyzeRequirementsAsync(default!, default).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Direct generic passthrough callers receive the same policy
    /// protection before reflection can reach an unsafe client method.
    /// </summary>
    [Fact]
    public async Task GenericClientPassthrough_WhenPolicyRejectsUnsafeMutation_ThrowsBeforeHttpRequest()
    {
        var handler = new CountingHandler();
        using var http = new HttpClient(handler);
        var client = new McpServerClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer",
        });
        var policy = Substitute.For<IClientMutationPolicy>();
        policy.Evaluate("context", "RebuildIndexAsync", Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(ClientMutationPolicyDecision.Reject(
                "mutation_not_transactional",
                "client.context.RebuildIndexAsync is blocked while required turn transactions are active."));
        var sut = new GenericClientPassthrough(client, policy);

        var exception = await Assert.ThrowsAsync<ClientMutationPolicyException>(
                () => sut.InvokeAsync(
                    "context",
                    "RebuildIndexAsync",
                    new Dictionary<string, object?>(),
                    CancellationToken.None))
            .ConfigureAwait(true);

        Assert.Equal("mutation_not_transactional", exception.ErrorCode);
        Assert.Equal("context", exception.ClientName);
        Assert.Equal("RebuildIndexAsync", exception.MethodName);
        Assert.Equal(0, handler.RequestCount);
    }

    /// <summary>
    /// TEST-MCP-161: The built-in policy classifies known unsafe context and
    /// analyzer mutations while preserving context reads.
    /// </summary>
    [Theory]
    [InlineData("context", "RebuildIndexAsync", false)]
    [InlineData("context", "IngestWebsiteAsync", false)]
    [InlineData("context", "GraphRagIndexAsync", false)]
    [InlineData("context", "GraphRagIngestTextAsync", false)]
    [InlineData("context", "GraphRagDeleteDocumentAsync", false)]
    [InlineData("context", "GraphRagCreateEntityAsync", false)]
    [InlineData("context", "GraphRagUpdateEntityAsync", false)]
    [InlineData("context", "GraphRagDeleteEntityAsync", false)]
    [InlineData("context", "GraphRagCreateRelationshipAsync", false)]
    [InlineData("context", "GraphRagUpdateRelationshipAsync", false)]
    [InlineData("context", "GraphRagDeleteRelationshipAsync", false)]
    [InlineData("todo", "AnalyzeRequirementsAsync", false)]
    [InlineData("todo", "CreateAsync", false)]
    [InlineData("todo", "RepairProjectionAsync", false)]
    [InlineData("todo", "QueryAsync", true)]
    [InlineData("todo", "GetAsync", true)]
    [InlineData("todo", "GetAuditAsync", true)]
    [InlineData("todo", "GetProjectionStatusAsync", true)]
    [InlineData("context", "SearchAsync", true)]
    [InlineData("context", "PackAsync", true)]
    [InlineData("context", "ListSourcesAsync", true)]
    [InlineData("context", "GraphRagStatusAsync", true)]
    [InlineData("context", "searchasync", true)]
    [InlineData("context", "GraphRagQueryAsync", true)]
    [InlineData("context", "NewContextMutationAsync", false)]
    [InlineData("federation", "GetStatusAsync", true)]
    [InlineData("federation", "ListTargetsAsync", true)]
    [InlineData("federation", "GetSyncItemsAsync", true)]
    [InlineData("federation", "EnableAsync", false)]
    [InlineData("federation", "DisableAsync", false)]
    [InlineData("federation", "AddTargetAsync", false)]
    [InlineData("federation", "RemoveTargetAsync", false)]
    [InlineData("federation", "SetDefaultTargetAsync", false)]
    [InlineData("federation", "ClearDefaultTargetAsync", false)]
    [InlineData("federation", "AddRouteAsync", false)]
    [InlineData("federation", "RemoveRouteAsync", false)]
    [InlineData("federation", "EnrollProxyAsync", false)]
    [InlineData("federation", "HeartbeatAsync", false)]
    [InlineData("federation", "RegisterWorkspaceAsync", false)]
    [InlineData("federation", "RecordOperationAsync", false)]
    [InlineData("federation", "RecordEnvelopeAsync", false)]
    [InlineData("federation", "AcknowledgeOperationAsync", false)]
    [InlineData("federation", "AcknowledgeSyncAsync", false)]
    [InlineData("federation", "ResolveConflictAsync", false)]
    [InlineData("federation", "GetConnectionAsync", false)]
    [InlineData("federation", "DiscoverFromTunnelsAsync", false)]
    [InlineData("federation", "PushAsync", false)]
    [InlineData("federation", "NewFederationMutationAsync", false)]
    [InlineData("keyServer", "GetManifestAsync", true)]
    [InlineData("keyServer", "GetManifestReportAsync", true)]
    [InlineData("keyServer", "GetPartyKeyAsync", true)]
    [InlineData("keyServer", "RegisterPartyAsync", false)]
    [InlineData("keyServer", "SignManifestAsync", false)]
    [InlineData("keyServer", "VerifyManifestAsync", false)]
    [InlineData("subscriber", "GetTransactionStatusAsync", true)]
    [InlineData("subscriber", "CommitDiffgramAsync", false)]
    [InlineData("subscriber", "AbortTransactionAsync", false)]
    [InlineData("tools", "CreateAsync", true)]
    [InlineData("memory", "AddAsync", true)]
    [InlineData("workspace", "CreateAsync", false)]
    [InlineData("desktop", "LaunchAsync", false)]
    [InlineData("newClient", "NewMutationAsync", false)]
    public void Evaluate_WithRequiredTransactions_AppliesKnownUnsafeMutationBlockList(
        string clientName,
        string methodName,
        bool expectedAllowed)
    {
        var sut = new KnownUnsafeClientMutationPolicy(() => new ClientMutationPolicyState(RequiredForMutations: true));

        var decision = sut.Evaluate(clientName, methodName, new Dictionary<string, object?>());

        Assert.Equal(expectedAllowed, decision.Allowed);
        if (!expectedAllowed)
            Assert.Equal("mutation_not_transactional", decision.ErrorCode);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            });
        }
    }
}
