using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-REQSCOPE-005 / TEST-MCP-REQSCOPE-REPL-001: red tests for typed
/// REPL workflow wrappers over requirement scope layer and effective requirement
/// client endpoints.
/// </summary>
public sealed class RequirementScopeLayerWorkflowTests
{
    private static readonly McpServerClientOptions Options = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    /// <summary>
    /// TEST-MCP-REQSCOPE-005: workflow requirement layer wrappers route through
    /// the typed client and preserve layer sunset fields.
    /// </summary>
    [Fact]
    public async Task RequirementLayerWorkflow_RoutesListCreateAndUpdateThroughClient()
    {
        var listHandler = new CapturingHandler(
            HttpStatusCode.OK,
            """[{"key":"layer-1","order":1,"name":"Layer 1"},{"key":"layer-2","order":2,"name":"Layer 2","scopeEndLayerKey":"layer-3"}]""");
        var listWorkflow = CreateWorkflow(listHandler);

        var layers = await listWorkflow.ListRequirementLayersAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, listHandler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/layers", listHandler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("layer-3", layers.Items[1].ScopeEndLayerKey);

        var createHandler = new CapturingHandler(
            HttpStatusCode.Created,
            """{"key":"layer-2","order":2,"name":"Layer 2","scopeEndLayerKey":"layer-3"}""");
        var createWorkflow = CreateWorkflow(createHandler);

        var created = await createWorkflow.CreateRequirementLayerAsync(new RequirementScopeLayerCreateRequestModel
        {
            Key = "layer-2",
            Order = 2,
            Name = "Layer 2",
            ScopeEndLayerKey = "layer-3"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, createHandler.LastRequest!.Method);
        Assert.Contains("\"scopeEndLayerKey\":\"layer-3\"", createHandler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Equal("layer-2", created.Key);

        var updateHandler = new CapturingHandler(
            HttpStatusCode.OK,
            """{"key":"layer-2","order":2,"name":"Layer 2","scopeEndLayerKey":"layer-2"}""");
        var updateWorkflow = CreateWorkflow(updateHandler);

        var updated = await updateWorkflow.UpdateRequirementLayerAsync(new RequirementScopeLayerUpdateRequestModel
        {
            Key = "layer-2",
            ScopeEndLayerKey = "layer-2"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Put, updateHandler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/layers/layer-2", updateHandler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("layer-2", updated.ScopeEndLayerKey);
    }

    /// <summary>
    /// TEST-MCP-REQSCOPE-005 / TEST-MCP-REQSCOPE-REPL-001: effective requirement
    /// workflow wrappers expose current-layer results and scoped requirement metadata.
    /// </summary>
    [Fact]
    public async Task EffectiveRequirementsWorkflow_ReturnsCurrentLayerAndScopedRequirementMetadata()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            """{"currentLayer":{"key":"layer-2","order":2,"name":"Layer 2"},"functional":[{"id":"FR-MCP-901","title":"Future","body":"Body","scopeStartLayerKey":"layer-2"}],"technical":[],"testing":[],"mappings":[]}""");
        var workflow = CreateWorkflow(handler);

        var effective = await workflow.GetEffectiveRequirementsAsync("layer-2", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/effective", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("layerKey=layer-2", handler.LastRequest.RequestUri.Query);
        Assert.Equal("layer-2", effective.CurrentLayer.Key);
        Assert.Equal("layer-2", effective.Functional[0].ScopeStartLayerKey);
    }

    /// <summary>
    /// TEST-MCP-REQSCOPE-REPL-001: final acceptance exercises the requirement scope layer
    /// workflow through YAML REPL envelopes: create a layer, add a requirement starting in
    /// that layer, sunset the layer after the new layer, sunset a requirement before it,
    /// and query effective requirements before and after the new layer.
    /// </summary>
    [Fact]
    public async Task RequirementScopeLayer_ReplYamlWorkflow_ExercisesBeforeAndAfterEffectiveQueries()
    {
        var handler = new StatefulRequirementScopeHandler();
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer"
        };
        var requirementsWorkflow = new RequirementsWorkflow(new RequirementsClient(http, options));
        var passthrough = new GenericClientPassthrough(new McpServerClient(http, options));
        var dispatcher = new ReplCommandDispatcher(passthrough, requirementsWorkflow: requirementsWorkflow);
        var protocol = new AgentStdioProtocol(new YamlSerializer(), dispatcher);

        await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-create-layer-2
              method: workflow.requirements.createLayer
              params:
                key: layer-2
                order: 2
                name: Layer 2

            """);
        await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-create-layer-3
              method: workflow.requirements.createLayer
              params:
                key: layer-3
                order: 3
                name: Layer 3

            """);
        await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-create-new-fr
              method: workflow.requirements.createFr
              params:
                id: FR-MCP-REQSCOPE-951
                title: New layer requirement
                description: Applies from layer 2.
                priority: high
                area: MCP
                scopeStartLayerKey: layer-2

            """);
        await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-create-old-fr
              method: workflow.requirements.createFr
              params:
                id: FR-MCP-REQSCOPE-950
                title: Sunset before layer
                description: Starts before layer 2 and is sunset by update.
                priority: high
                area: MCP
                scopeStartLayerKey: layer-1

            """);
        await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-sunset-fr-before-layer
              method: workflow.requirements.updateFr
              params:
                id: FR-MCP-REQSCOPE-950
                scopeEndLayerKey: layer-1

            """);
        await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-sunset-layer
              method: workflow.requirements.updateLayer
              params:
                key: layer-2
                scopeEndLayerKey: layer-3

            """);

        var beforeOutput = await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-effective-before
              method: workflow.requirements.effective
              params:
                layerKey: layer-1

            """);
        var afterOutput = await RunReplAsync(protocol, """
            type: request
            payload:
              requestId: req-reqscope-effective-after
              method: workflow.requirements.effective
              params:
                layerKey: layer-2

            """);

        Assert.Contains("type: result", beforeOutput, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-REQSCOPE-950", beforeOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-951", beforeOutput, StringComparison.Ordinal);
        Assert.Contains("type: result", afterOutput, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-REQSCOPE-951", afterOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-950", afterOutput, StringComparison.Ordinal);
        Assert.Equal("layer-3", handler.Layers.Single(x => x.Key == "layer-2").ScopeEndLayerKey);
    }

    private static RequirementsWorkflow CreateWorkflow(CapturingHandler handler)
    {
        var http = new HttpClient(handler);
        var client = new RequirementsClient(http, Options);
        return new RequirementsWorkflow(client);
    }

    private static async Task<string> RunReplAsync(AgentStdioProtocol protocol, string input)
    {
        using var reader = new StringReader(input);
        using var writer = new StringWriter();
        await protocol.RunAsync(reader, writer, CancellationToken.None).ConfigureAwait(false);
        var output = writer.ToString();
        Assert.True(output.Contains("type: result", StringComparison.Ordinal), output);
        return output;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public CapturingHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StatefulRequirementScopeHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly List<RequirementScopeLayer> _layers =
        [
            new() { Key = "layer-1", Order = 1, Name = "Layer 1" }
        ];
        private readonly List<FrEntry> _functional = [];

        public IReadOnlyList<RequirementScopeLayer> Layers => _layers;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (request.Method == HttpMethod.Post && path.EndsWith("/mcpserver/requirements/layers", StringComparison.OrdinalIgnoreCase))
            {
                var payload = await ReadAsync<RequirementScopeLayerRequest>(request, cancellationToken).ConfigureAwait(false);
                var layer = new RequirementScopeLayer
                {
                    Key = payload.Key,
                    Order = payload.Order,
                    Name = payload.Name,
                    Description = payload.Description,
                    ScopeEndLayerKey = payload.ScopeEndLayerKey
                };
                _layers.Add(layer);
                return Json(HttpStatusCode.Created, layer);
            }

            if (request.Method == HttpMethod.Put && path.Contains("/mcpserver/requirements/layers/", StringComparison.OrdinalIgnoreCase))
            {
                var key = Uri.UnescapeDataString(path.Split('/').Last());
                var payload = await ReadAsync<RequirementScopeLayerUpdate>(request, cancellationToken).ConfigureAwait(false);
                var layer = _layers.Single(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                layer.Name = payload.Name ?? layer.Name;
                layer.Description = payload.Description ?? layer.Description;
                layer.ScopeEndLayerKey = payload.ScopeEndLayerKey ?? layer.ScopeEndLayerKey;
                return Json(HttpStatusCode.OK, layer);
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/mcpserver/requirements/fr", StringComparison.OrdinalIgnoreCase))
            {
                var payload = await ReadAsync<CreateFrRequest>(request, cancellationToken).ConfigureAwait(false);
                var entry = new FrEntry
                {
                    Id = payload.Id,
                    Title = payload.Title,
                    Body = payload.Body,
                    Priority = payload.Priority ?? "medium",
                    Status = payload.Status ?? "pending",
                    Notes = payload.Notes,
                    AcceptanceCriteria = payload.AcceptanceCriteria,
                    ScopeStartLayerKey = string.IsNullOrWhiteSpace(payload.ScopeStartLayerKey) ? "layer-1" : payload.ScopeStartLayerKey,
                    ScopeEndLayerKey = payload.ScopeEndLayerKey
                };
                _functional.Add(entry);
                return Json(HttpStatusCode.Created, entry);
            }

            if (request.Method == HttpMethod.Put && path.Contains("/mcpserver/requirements/fr/", StringComparison.OrdinalIgnoreCase))
            {
                var id = Uri.UnescapeDataString(path.Split('/').Last());
                var payload = await ReadAsync<UpdateFrRequest>(request, cancellationToken).ConfigureAwait(false);
                var index = _functional.FindIndex(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                    return Json(HttpStatusCode.NotFound, new { error = id });

                var existing = _functional[index];
                _functional[index] = new FrEntry
                {
                    Id = existing.Id,
                    Title = payload.Title ?? existing.Title,
                    Body = payload.Body ?? existing.Body,
                    Priority = payload.Priority ?? existing.Priority,
                    Status = payload.Status ?? existing.Status,
                    Notes = payload.Notes ?? existing.Notes,
                    AcceptanceCriteria = payload.AcceptanceCriteria ?? existing.AcceptanceCriteria,
                    ScopeStartLayerKey = payload.ScopeStartLayerKey ?? existing.ScopeStartLayerKey,
                    ScopeEndLayerKey = payload.ScopeEndLayerKey ?? existing.ScopeEndLayerKey
                };

                return Json(HttpStatusCode.OK, _functional[index]);
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("/mcpserver/requirements/effective", StringComparison.OrdinalIgnoreCase))
            {
                var layerKey = GetQueryParameter(request.RequestUri, "layerKey") ?? "layer-1";
                var layer = _layers.Single(x => string.Equals(x.Key, layerKey, StringComparison.OrdinalIgnoreCase));
                var result = new EffectiveRequirementsResult
                {
                    CurrentLayer = layer,
                    Functional = _functional.Where(entry => IsEffective(entry, layer)).ToArray(),
                    Technical = [],
                    Testing = [],
                    Mappings = []
                };
                return Json(HttpStatusCode.OK, result);
            }

            return Json(HttpStatusCode.NotFound, new { error = path });
        }

        private bool IsEffective(FrEntry entry, RequirementScopeLayer currentLayer)
        {
            var start = _layers.Single(x => string.Equals(x.Key, entry.ScopeStartLayerKey, StringComparison.OrdinalIgnoreCase));
            if (start.Order > currentLayer.Order)
                return false;

            if (!string.IsNullOrWhiteSpace(start.ScopeEndLayerKey))
            {
                var layerEnd = _layers.Single(x => string.Equals(x.Key, start.ScopeEndLayerKey, StringComparison.OrdinalIgnoreCase));
                if (layerEnd.Order < currentLayer.Order)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(entry.ScopeEndLayerKey))
            {
                var requirementEnd = _layers.Single(x => string.Equals(x.Key, entry.ScopeEndLayerKey, StringComparison.OrdinalIgnoreCase));
                if (requirementEnd.Order < currentLayer.Order)
                    return false;
            }

            return true;
        }

        private static async Task<T> ReadAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                   ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name}.");
        }

        private static HttpResponseMessage Json(HttpStatusCode statusCode, object value)
            => new(statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json")
            };

        private static string? GetQueryParameter(Uri? uri, string name)
        {
            var query = uri?.Query.TrimStart('?');
            if (string.IsNullOrWhiteSpace(query))
                return null;

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(parts[1]);
            }

            return null;
        }
    }
}
