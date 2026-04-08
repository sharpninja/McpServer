using System.Text.Json;
using McpServer.McpAgent.Hosting;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.McpAgent.SessionLog;
using McpServer.McpAgent.Todo;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-090: Verifies that the McpHostedAgentToolAdapter exposes the 14 GraphRAG
/// ad-hoc management tools (FR-MCP-078/079/080, TR-GRAPHRAG-ADHOC-001/002/003) through
/// the standard <see cref="AIFunction"/> surface, and that tool invocations delegate to the
/// underlying <see cref="ContextClient"/> transport methods.
/// </summary>
public sealed class GraphRagToolAdapterTests
{
    private static readonly string TestWorkspacePath =
        Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

    /// <summary>
    /// Expected names of all 14 GraphRAG ad-hoc management tools that Phase 7 adds.
    /// </summary>
    private static readonly string[] GraphRagToolNames =
    [
        "mcp_graphrag_ingest_text",
        "mcp_graphrag_list_documents",
        "mcp_graphrag_get_document_chunks",
        "mcp_graphrag_delete_document",
        "mcp_graphrag_create_entity",
        "mcp_graphrag_list_entities",
        "mcp_graphrag_get_entity",
        "mcp_graphrag_update_entity",
        "mcp_graphrag_delete_entity",
        "mcp_graphrag_create_relationship",
        "mcp_graphrag_list_relationships",
        "mcp_graphrag_get_relationship",
        "mcp_graphrag_update_relationship",
        "mcp_graphrag_delete_relationship",
    ];

    /// <summary>
    /// TEST-MCP-090: Verifies that the adapter exposes exactly 14 tools whose names begin
    /// with <c>mcp_graphrag_</c> and that the total tool count increased by 14 over the
    /// pre-Phase-7 baseline of 28.
    /// </summary>
    [Fact]
    public void Registration_Contains_All_GraphRag_Tools()
    {
        var (hostedAgent, _) = CreateHostedAgent();
        var allFunctions = hostedAgent.Registration.Functions;

        var graphRagFunctions = allFunctions
            .Where(static f => f.Name.StartsWith("mcp_graphrag_", StringComparison.Ordinal))
            .Select(static f => f.Name)
            .ToArray();

        Assert.Equal(14, graphRagFunctions.Length);
        Assert.Equal(GraphRagToolNames, graphRagFunctions);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the total tool count is 42 (28 pre-existing + 14 new GraphRAG tools).
    /// </summary>
    [Fact]
    public void Registration_TotalToolCount_Is_42()
    {
        var (hostedAgent, _) = CreateHostedAgent();
        Assert.Equal(42, hostedAgent.Registration.Functions.Count);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies each GraphRAG tool name starts with the <c>mcp_graphrag_</c> prefix.
    /// </summary>
    [Fact]
    public void Registration_AllGraphRagTools_HaveCorrectPrefix()
    {
        var (hostedAgent, _) = CreateHostedAgent();
        var allFunctions = hostedAgent.Registration.Functions;

        foreach (var name in GraphRagToolNames)
        {
            Assert.True(name.StartsWith("mcp_graphrag_", StringComparison.Ordinal),
                $"Tool name '{name}' does not start with 'mcp_graphrag_'.");
            Assert.Contains(allFunctions, f => f.Name == name);
        }
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_ingest_text</c> tool delegates to the
    /// underlying ContextClient.GraphRagIngestTextAsync endpoint and returns the ingested document metadata.
    /// </summary>
    [Fact]
    public async Task IngestText_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_ingest_text");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new GraphRagIngestTextRequest
                {
                    Content = "Sample ingestion text",
                    Title = "Test Doc",
                    SourceType = "adhoc-text",
                },
            },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("doc-001", json.GetProperty("documentId").GetString());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/graphrag/documents/ingest", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_list_documents</c> tool delegates to
    /// ContextClient.GraphRagListDocumentsAsync and returns the document list.
    /// </summary>
    [Fact]
    public async Task ListDocuments_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_list_documents");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments
            {
                ["skip"] = 0,
                ["take"] = 10,
            },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/documents", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_get_document_chunks</c> tool delegates
    /// to the document chunks endpoint.
    /// </summary>
    [Fact]
    public async Task GetDocumentChunks_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_get_document_chunks");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["documentId"] = "doc-001" },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("doc-001", json.GetProperty("documentId").GetString());
        Assert.Single(handler.Requests);
        Assert.Contains("/mcpserver/graphrag/documents/doc-001/chunks", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_delete_document</c> tool delegates
    /// to the DELETE documents endpoint.
    /// </summary>
    [Fact]
    public async Task DeleteDocument_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_delete_document");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["documentId"] = "doc-001" },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/documents/doc-001", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_create_entity</c> tool delegates
    /// to the POST entities endpoint.
    /// </summary>
    [Fact]
    public async Task CreateEntity_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_create_entity");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new GraphEntityRequest
                {
                    Name = "Contoso",
                    EntityType = "organization",
                },
            },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("ent-001", json.GetProperty("id").GetString());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/graphrag/entities", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_list_entities</c> tool delegates
    /// to the GET entities endpoint.
    /// </summary>
    [Fact]
    public async Task ListEntities_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_list_entities");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["skip"] = 0, ["take"] = 20 },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/entities", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_get_entity</c> tool delegates to GET entities/{id}.
    /// </summary>
    [Fact]
    public async Task GetEntity_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_get_entity");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["entityId"] = "ent-001" },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("ent-001", json.GetProperty("id").GetString());
        Assert.Single(handler.Requests);
        Assert.Contains("/mcpserver/graphrag/entities/ent-001", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_update_entity</c> tool delegates to PUT entities/{id}.
    /// </summary>
    [Fact]
    public async Task UpdateEntity_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_update_entity");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments
            {
                ["entityId"] = "ent-001",
                ["request"] = new GraphEntityRequest
                {
                    Name = "Contoso Updated",
                    EntityType = "organization",
                },
            },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("ent-001", json.GetProperty("id").GetString());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/entities/ent-001", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_delete_entity</c> tool delegates to DELETE entities/{id}.
    /// </summary>
    [Fact]
    public async Task DeleteEntity_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_delete_entity");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["entityId"] = "ent-001" },
            CancellationToken.None);

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/entities/ent-001", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_create_relationship</c> tool delegates
    /// to the POST relationships endpoint.
    /// </summary>
    [Fact]
    public async Task CreateRelationship_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_create_relationship");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new GraphRelationshipRequest
                {
                    SourceEntityId = "ent-001",
                    TargetEntityId = "ent-002",
                    RelationshipType = "works_with",
                },
            },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("rel-001", json.GetProperty("id").GetString());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/graphrag/relationships", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_list_relationships</c> tool delegates
    /// to the GET relationships endpoint.
    /// </summary>
    [Fact]
    public async Task ListRelationships_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_list_relationships");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["skip"] = 0, ["take"] = 20 },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/relationships", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_get_relationship</c> tool delegates
    /// to GET relationships/{id}.
    /// </summary>
    [Fact]
    public async Task GetRelationship_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_get_relationship");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["relationshipId"] = "rel-001" },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("rel-001", json.GetProperty("id").GetString());
        Assert.Single(handler.Requests);
        Assert.Contains("/mcpserver/graphrag/relationships/rel-001", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_update_relationship</c> tool delegates
    /// to PUT relationships/{id}.
    /// </summary>
    [Fact]
    public async Task UpdateRelationship_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_update_relationship");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments
            {
                ["relationshipId"] = "rel-001",
                ["request"] = new GraphRelationshipRequest
                {
                    SourceEntityId = "ent-001",
                    TargetEntityId = "ent-002",
                    RelationshipType = "reports_to",
                },
            },
            CancellationToken.None);

        var json = Assert.IsType<JsonElement>(result);
        Assert.Equal("rel-001", json.GetProperty("id").GetString());
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/relationships/rel-001", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-090: Verifies that the <c>mcp_graphrag_delete_relationship</c> tool delegates
    /// to DELETE relationships/{id}.
    /// </summary>
    [Fact]
    public async Task DeleteRelationship_DelegatesToContextClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var fn = hostedAgent.Registration.Functions.Single(
            static f => f.Name == "mcp_graphrag_delete_relationship");

        var result = await fn.InvokeAsync(
            new AIFunctionArguments { ["relationshipId"] = "rel-001" },
            CancellationToken.None);

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Contains("/mcpserver/graphrag/relationships/rel-001", handler.Requests[0].RequestUri.AbsolutePath);
    }

    // ── Test infrastructure ──────────────────────────────────────────────

    private static (McpHostedAgent HostedAgent, GraphRagRecordingHandler Handler) CreateHostedAgent()
    {
        var handler = new GraphRagRecordingHandler();
        var httpClient = new HttpClient(handler);
        var client = new McpServerClient(
            httpClient,
            new McpServerClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = new Uri("http://localhost:7147"),
                WorkspacePath = TestWorkspacePath,
            });
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 09, 15, 01, 05, TimeSpan.Zero));
        var options = Options.Create(
            new McpAgentOptions
            {
                ApiKey = "test-key",
                BaseUrl = new Uri("http://localhost:7147"),
                SourceType = "Codex",
                WorkspacePath = TestWorkspacePath,
            });
        var identifiers = new McpSessionIdentifierFactory(options, timeProvider);
        var sessionLog = new McpServer.McpAgent.SessionLog.SessionLogWorkflow(client, identifiers, timeProvider);
        var todo = new McpServer.McpAgent.Todo.TodoWorkflow(client);
        var requirements = new RequirementsWorkflow(client.Requirements);
        var clientPassthrough = new GenericClientPassthrough(client);
        var replSessionLogAdapter = new SessionLogClientAdapter(client.SessionLog);
        var replSessionLog = new McpServer.Repl.Core.SessionLogWorkflow(replSessionLogAdapter, timeProvider);
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        return (
            new McpHostedAgent(
                client,
                identifiers,
                new ChatClientAgentOptions
                {
                    Description = "GraphRAG hosted MCP agent adapter.",
                    Id = "mcpserver-graphrag-agent",
                    Name = "McpServerGraphRagAgent",
                },
                options,
                sessionLog,
                todo,
                requirements,
                clientPassthrough,
                replSessionLog,
                serviceProvider),
            handler);
    }

    /// <summary>
    /// TEST-MCP-090: Provides a deterministic clock for the hosted-agent adapter tests.
    /// </summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        /// <summary>
        /// TEST-MCP-090: Initializes the deterministic test clock with a fixed UTC timestamp.
        /// </summary>
        /// <param name="utcNow">The fixed UTC timestamp returned by <see cref="GetUtcNow"/>.</param>
        public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow.ToUniversalTime();

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    /// <summary>
    /// TEST-MCP-090: Captures outbound MCP GraphRAG transport calls and returns deterministic
    /// JSON payloads that match the requested endpoint.
    /// </summary>
    internal sealed class GraphRagRecordingHandler : HttpMessageHandler
    {
        /// <summary>
        /// TEST-MCP-090: Gets the ordered request log captured during a test run.
        /// </summary>
        public List<RecordedRequest> Requests { get; } = [];

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body));

            var path = request.RequestUri!.AbsolutePath;
            var method = request.Method;

            // ── Ingest text ──
            if (path == "/mcpserver/graphrag/documents/ingest" && method == HttpMethod.Post)
                return JsonResponse("""{"documentId":"doc-001","chunkCount":3,"tokenCount":120,"sourceType":"adhoc-text","sourceKey":"test-doc","reindexTriggered":false}""");

            // ── List documents ──
            if (path == "/mcpserver/graphrag/documents" && method == HttpMethod.Get)
                return JsonResponse("""{"documents":[{"id":"doc-001","sourceType":"adhoc-text","sourceKey":"test-doc","contentHash":"abc","chunkCount":3,"totalTokens":120}],"totalCount":1}""");

            // ── Get document chunks ──
            if (path.StartsWith("/mcpserver/graphrag/documents/", StringComparison.Ordinal) &&
                path.EndsWith("/chunks", StringComparison.Ordinal) && method == HttpMethod.Get)
                return JsonResponse("""{"documentId":"doc-001","chunks":[{"id":"chunk-001","content":"Hello","tokenCount":5,"chunkIndex":0}],"totalChunks":1}""");

            // ── Delete document ──
            if (path.StartsWith("/mcpserver/graphrag/documents/", StringComparison.Ordinal) &&
                !path.EndsWith("/chunks", StringComparison.Ordinal) &&
                !path.EndsWith("/ingest", StringComparison.Ordinal) && method == HttpMethod.Delete)
                return JsonResponse("""{"documentId":"doc-001","chunksRemoved":3,"success":true}""");

            // ── Create entity ──
            if (path == "/mcpserver/graphrag/entities" && method == HttpMethod.Post)
                return JsonResponse("""{"id":"ent-001","name":"Contoso","entityType":"organization"}""",
                    System.Net.HttpStatusCode.Created);

            // ── List entities ──
            if (path == "/mcpserver/graphrag/entities" && method == HttpMethod.Get)
                return JsonResponse("""{"entities":[{"id":"ent-001","name":"Contoso","entityType":"organization"}],"totalCount":1}""");

            // ── Get entity / Update entity ──
            if (path.StartsWith("/mcpserver/graphrag/entities/", StringComparison.Ordinal) &&
                (method == HttpMethod.Get || method == HttpMethod.Put))
                return JsonResponse("""{"id":"ent-001","name":"Contoso","entityType":"organization"}""");

            // ── Delete entity ──
            if (path.StartsWith("/mcpserver/graphrag/entities/", StringComparison.Ordinal) && method == HttpMethod.Delete)
                return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);

            // ── Create relationship ──
            if (path == "/mcpserver/graphrag/relationships" && method == HttpMethod.Post)
                return JsonResponse("""{"id":"rel-001","sourceEntityId":"ent-001","targetEntityId":"ent-002","relationshipType":"works_with","weight":1.0}""",
                    System.Net.HttpStatusCode.Created);

            // ── List relationships ──
            if (path == "/mcpserver/graphrag/relationships" && method == HttpMethod.Get)
                return JsonResponse("""{"relationships":[{"id":"rel-001","sourceEntityId":"ent-001","targetEntityId":"ent-002","relationshipType":"works_with","weight":1.0}],"totalCount":1}""");

            // ── Get relationship / Update relationship ──
            if (path.StartsWith("/mcpserver/graphrag/relationships/", StringComparison.Ordinal) &&
                (method == HttpMethod.Get || method == HttpMethod.Put))
                return JsonResponse("""{"id":"rel-001","sourceEntityId":"ent-001","targetEntityId":"ent-002","relationshipType":"works_with","weight":1.0}""");

            // ── Delete relationship ──
            if (path.StartsWith("/mcpserver/graphrag/relationships/", StringComparison.Ordinal) && method == HttpMethod.Delete)
                return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);

            throw new InvalidOperationException($"Unexpected GraphRAG request: {method} {path}");
        }

        private static HttpResponseMessage JsonResponse(
            string json,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK) =>
            new(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
    }

    /// <summary>
    /// TEST-MCP-090: Captures a single recorded HTTP request.
    /// </summary>
    /// <param name="Method">The HTTP method.</param>
    /// <param name="RequestUri">The request URI.</param>
    /// <param name="Body">The serialized request body, when present.</param>
    internal sealed record RecordedRequest(HttpMethod Method, Uri RequestUri, string? Body);
}
