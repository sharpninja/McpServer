// FR-MCP-REPL-003: Command Namespace Parity - GraphRAG workflow dispatch coverage
// TR-MCP-REPL-004: Command Registry and Dispatcher - workflow.graphrag route table
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - GraphRAG operations
// TEST-MCP-REPL-001: REPL host processes GraphRAG YAML command envelopes

using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Verifies that the deprecated <c>workflow.graphrag.*</c> namespace routes to the
/// registered GraphRAG workflow instead of falling through to <c>method_not_found</c>.
/// </summary>
public sealed class GraphRagDispatcherTests
{
    /// <summary>
    /// Enumerates every GraphRAG workflow command exposed by <see cref="GraphRagCommandShapes"/>.
    /// </summary>
    /// <returns>Method names and representative parameter payloads.</returns>
    public static IEnumerable<object[]> GraphRagRouteCases()
    {
        yield return [GraphRagCommandShapes.StatusMethod, new Dictionary<string, object?>()];
        yield return [GraphRagCommandShapes.IndexMethod, new Dictionary<string, object?> { ["force"] = true }];
        yield return [GraphRagCommandShapes.QueryMethod, new Dictionary<string, object?>
        {
            ["query"] = "Which design decisions mention GraphRAG?",
            ["mode"] = "local",
            ["maxChunks"] = 5,
            ["includeContextChunks"] = false,
            ["maxEntities"] = 3,
            ["maxRelationships"] = 4,
            ["communityDepth"] = 2,
            ["responseTokenBudget"] = 900,
        }];
        yield return [GraphRagCommandShapes.IngestMethod, new Dictionary<string, object?>
        {
            ["content"] = "GraphRAG dispatcher parity note.",
            ["title"] = "Dispatcher Note",
            ["sourceType"] = "test",
            ["sourceKey"] = "graphrag-dispatcher",
            ["triggerReindex"] = true,
        }];
        yield return [GraphRagCommandShapes.DocumentsListMethod, new Dictionary<string, object?>
        {
            ["skip"] = 2,
            ["take"] = 3,
            ["sourceType"] = "repo",
        }];
        yield return [GraphRagCommandShapes.DocumentsChunksMethod, new Dictionary<string, object?> { ["documentId"] = "doc-001" }];
        yield return [GraphRagCommandShapes.DocumentsDeleteMethod, new Dictionary<string, object?> { ["documentId"] = "doc-001" }];
        yield return [GraphRagCommandShapes.EntitiesCreateMethod, new Dictionary<string, object?>
        {
            ["name"] = "GraphRAG Dispatcher",
            ["entityType"] = "component",
            ["description"] = "Routes workflow commands.",
            ["metadata"] = "{\"test\":true}",
        }];
        yield return [GraphRagCommandShapes.EntitiesListMethod, new Dictionary<string, object?>
        {
            ["skip"] = 1,
            ["take"] = 7,
            ["entityType"] = "component",
        }];
        yield return [GraphRagCommandShapes.EntitiesGetMethod, new Dictionary<string, object?> { ["entityId"] = "entity-001" }];
        yield return [GraphRagCommandShapes.EntitiesUpdateMethod, new Dictionary<string, object?>
        {
            ["entityId"] = "entity-001",
            ["name"] = "GraphRAG Dispatcher Updated",
            ["entityType"] = "component",
            ["description"] = "Routes all commands.",
            ["metadata"] = "{\"updated\":true}",
        }];
        yield return [GraphRagCommandShapes.EntitiesDeleteMethod, new Dictionary<string, object?> { ["entityId"] = "entity-001" }];
        yield return [GraphRagCommandShapes.RelationshipsCreateMethod, new Dictionary<string, object?>
        {
            ["sourceEntityId"] = "entity-001",
            ["targetEntityId"] = "entity-002",
            ["relationshipType"] = "routes-to",
            ["description"] = "Dispatcher route edge.",
            ["weight"] = 0.75,
            ["metadata"] = "{\"edge\":true}",
        }];
        yield return [GraphRagCommandShapes.RelationshipsListMethod, new Dictionary<string, object?>
        {
            ["skip"] = 4,
            ["take"] = 8,
            ["entityId"] = "entity-001",
            ["type"] = "routes-to",
        }];
        yield return [GraphRagCommandShapes.RelationshipsGetMethod, new Dictionary<string, object?> { ["relationshipId"] = "rel-001" }];
        yield return [GraphRagCommandShapes.RelationshipsUpdateMethod, new Dictionary<string, object?>
        {
            ["relationshipId"] = "rel-001",
            ["sourceEntityId"] = "entity-001",
            ["targetEntityId"] = "entity-003",
            ["relationshipType"] = "routes-to",
            ["description"] = "Updated dispatcher route edge.",
            ["weight"] = "0.95",
            ["metadata"] = "{\"updated\":true}",
        }];
        yield return [GraphRagCommandShapes.RelationshipsDeleteMethod, new Dictionary<string, object?> { ["relationshipId"] = "rel-001" }];
    }

    /// <summary>
    /// Covers the complete GraphRAG command surface so adding or removing a method requires an
    /// intentional dispatcher update.
    /// </summary>
    [Fact]
    public void GraphRagRouteCases_CoverAllCommandShapes()
    {
        Assert.Equal(17, GraphRagRouteCases().Count());
    }

    /// <summary>
    /// Every GraphRAG command is routed through <see cref="IGraphRagWorkflow"/> and marked as
    /// deprecated to preserve the existing workflow namespace migration contract.
    /// </summary>
    /// <param name="method">The GraphRAG workflow method name.</param>
    /// <param name="parameters">Representative command parameters.</param>
    [Theory]
    [MemberData(nameof(GraphRagRouteCases))]
    public async Task Dispatcher_GraphRagWorkflowMethod_RoutesToRegisteredWorkflow(
        string method,
        Dictionary<string, object?> parameters)
    {
        var workflow = Substitute.For<IGraphRagWorkflow>();
        ConfigureWorkflow(workflow);
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            graphRagWorkflow: workflow);

        var response = await sut.DispatchAsync(BuildRequest(method, parameters), CancellationToken.None);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        Assert.True(payload.Deprecated, "workflow.graphrag.* responses must preserve the deprecated workflow marker.");
        await AssertReceivedAsync(workflow, method);
    }

    /// <summary>
    /// Update operations accept the same nested <c>request</c> payload shape used by other
    /// workflow dispatchers while keeping the entity identifier at the envelope parameter level.
    /// </summary>
    [Fact]
    public async Task Dispatcher_GraphRagEntityUpdate_AcceptsNestedRequestPayload()
    {
        var workflow = Substitute.For<IGraphRagWorkflow>();
        ConfigureWorkflow(workflow);
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            graphRagWorkflow: workflow);

        var response = await sut.DispatchAsync(BuildRequest(
            GraphRagCommandShapes.EntitiesUpdateMethod,
            new Dictionary<string, object?>
            {
                ["entityId"] = "entity-nested",
                ["request"] = new Dictionary<string, object?>
                {
                    ["name"] = "Nested GraphRAG Entity",
                    ["entityType"] = "component",
                    ["description"] = "Nested request shape.",
                    ["metadata"] = "{\"nested\":true}",
                },
            }), CancellationToken.None);

        Assert.Equal("result", response.Type);
        await workflow.Received(1).UpdateEntityAsync(
            "entity-nested",
            Arg.Is<GraphEntityRequest>(request =>
                request != null &&
                request.Name == "Nested GraphRAG Entity" &&
                request.EntityType == "component" &&
                request.Description == "Nested request shape." &&
                request.Metadata == "{\"nested\":true}"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A dispatcher without GraphRAG registration returns a clear route error for the namespace.
    /// </summary>
    [Fact]
    public async Task Dispatcher_GraphRagWorkflowNotRegistered_ReturnsMethodNotFound()
    {
        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>());

        var response = await sut.DispatchAsync(
            BuildRequest(GraphRagCommandShapes.StatusMethod, new Dictionary<string, object?>()),
            CancellationToken.None);

        Assert.Equal("error", response.Type);
        var payload = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("method_not_found", payload.Code);
        Assert.Contains("GraphRAG workflow is not registered", payload.Message, StringComparison.Ordinal);
    }

    private static YamlEnvelope BuildRequest(string method, Dictionary<string, object?> parameters) => new()
    {
        Type = "request",
        Payload = new RequestPayload
        {
            RequestId = $"req-20260616T040000Z-graphrag-{Guid.NewGuid().ToString("N")[..8]}",
            Method = method,
            Params = parameters,
        },
    };

    private static void ConfigureWorkflow(IGraphRagWorkflow workflow)
    {
        workflow.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRagStatusResult()));
        workflow.IndexAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRagStatusResult()));
        workflow.QueryAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRagQueryResult()));
        workflow.IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRagIngestTextResult()));
        workflow.ListDocumentsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRagDocumentListResult()));
        workflow.GetDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRagDocumentChunksResult()));
        workflow.DeleteDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRagDocumentDeleteResult()));
        workflow.CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphEntityResult()));
        workflow.ListEntitiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphEntityListResult()));
        workflow.GetEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphEntityResult()));
        workflow.UpdateEntityAsync(Arg.Any<string>(), Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphEntityResult()));
        workflow.DeleteEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        workflow.CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRelationshipResult()));
        workflow.ListRelationshipsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRelationshipListResult()));
        workflow.GetRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRelationshipResult()));
        workflow.UpdateRelationshipAsync(Arg.Any<string>(), Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphRelationshipResult()));
        workflow.DeleteRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    private static async Task AssertReceivedAsync(IGraphRagWorkflow workflow, string method)
    {
        switch (method)
        {
            case GraphRagCommandShapes.StatusMethod:
                await workflow.Received(1).GetStatusAsync(Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.IndexMethod:
                await workflow.Received(1).IndexAsync(true, Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.QueryMethod:
                await workflow.Received(1).QueryAsync(
                    "Which design decisions mention GraphRAG?",
                    "local",
                    5,
                    false,
                    3,
                    4,
                    2,
                    900,
                    Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.IngestMethod:
                await workflow.Received(1).IngestTextAsync(
                    Arg.Is<GraphRagIngestTextRequest>(request =>
                        request != null &&
                        request.Content == "GraphRAG dispatcher parity note." &&
                        request.Title == "Dispatcher Note" &&
                        request.SourceType == "test" &&
                        request.SourceKey == "graphrag-dispatcher" &&
                        request.TriggerReindex),
                    Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.DocumentsListMethod:
                await workflow.Received(1).ListDocumentsAsync(2, 3, "repo", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.DocumentsChunksMethod:
                await workflow.Received(1).GetDocumentChunksAsync("doc-001", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.DocumentsDeleteMethod:
                await workflow.Received(1).DeleteDocumentAsync("doc-001", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.EntitiesCreateMethod:
                await workflow.Received(1).CreateEntityAsync(
                    Arg.Is<GraphEntityRequest>(request =>
                        request != null &&
                        request.Name == "GraphRAG Dispatcher" &&
                        request.EntityType == "component" &&
                        request.Description == "Routes workflow commands." &&
                        request.Metadata == "{\"test\":true}"),
                    Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.EntitiesListMethod:
                await workflow.Received(1).ListEntitiesAsync(1, 7, "component", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.EntitiesGetMethod:
                await workflow.Received(1).GetEntityAsync("entity-001", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.EntitiesUpdateMethod:
                await workflow.Received(1).UpdateEntityAsync(
                    "entity-001",
                    Arg.Is<GraphEntityRequest>(request =>
                        request != null &&
                        request.Name == "GraphRAG Dispatcher Updated" &&
                        request.EntityType == "component" &&
                        request.Description == "Routes all commands." &&
                        request.Metadata == "{\"updated\":true}"),
                    Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.EntitiesDeleteMethod:
                await workflow.Received(1).DeleteEntityAsync("entity-001", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.RelationshipsCreateMethod:
                await workflow.Received(1).CreateRelationshipAsync(
                    Arg.Is<GraphRelationshipRequest>(request =>
                        request != null &&
                        request.SourceEntityId == "entity-001" &&
                        request.TargetEntityId == "entity-002" &&
                        request.RelationshipType == "routes-to" &&
                        request.Description == "Dispatcher route edge." &&
                        Math.Abs(request.Weight - 0.75) < 0.001 &&
                        request.Metadata == "{\"edge\":true}"),
                    Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.RelationshipsListMethod:
                await workflow.Received(1).ListRelationshipsAsync(4, 8, "entity-001", "routes-to", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.RelationshipsGetMethod:
                await workflow.Received(1).GetRelationshipAsync("rel-001", Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.RelationshipsUpdateMethod:
                await workflow.Received(1).UpdateRelationshipAsync(
                    "rel-001",
                    Arg.Is<GraphRelationshipRequest>(request =>
                        request != null &&
                        request.SourceEntityId == "entity-001" &&
                        request.TargetEntityId == "entity-003" &&
                        request.RelationshipType == "routes-to" &&
                        request.Description == "Updated dispatcher route edge." &&
                        Math.Abs(request.Weight - 0.95) < 0.001 &&
                        request.Metadata == "{\"updated\":true}"),
                    Arg.Any<CancellationToken>());
                break;
            case GraphRagCommandShapes.RelationshipsDeleteMethod:
                await workflow.Received(1).DeleteRelationshipAsync("rel-001", Arg.Any<CancellationToken>());
                break;
            default:
                throw new InvalidOperationException($"Unexpected GraphRAG route: {method}");
        }
    }
}
