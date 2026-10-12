using System;
using System.Net;
using System.Net.Http;
using QBrainAi.Client.Models;
using Xunit;

namespace QBrainAi.Client.Tests;

/// <summary>
/// Unit tests for <see cref="FederationClient"/>. Validates correct HTTP method,
/// URL construction, request/response serialization for all federation endpoints.
/// FR-MCP-077, FR-MCP-085.
/// </summary>
public sealed class FederationClientTests
{
    private static readonly QBrainAiClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task GetStatusAsyncSendsCorrectRequest()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"enabled":true,"role":"Hub","configuredRole":"Hub","hubBaseUrl":"http://hub:7147","proxyId":"PAYTON-LEGION2","proxyCount":2,"hostedWorkspaceCount":5,"queueDepth":3,"fanoutDepth":4,"conflictCount":1,"staleReadStatus":"stale","targets":[],"workspaceRoutes":[]}
            """);
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetStatusAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("/qbrainai/federation/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
        Assert.Equal("Hub", result.Role);
        Assert.Equal("Hub", result.ConfiguredRole);
        Assert.Equal("http://hub:7147", result.HubBaseUrl);
        Assert.Equal("PAYTON-LEGION2", result.ProxyId);
        Assert.Equal(2, result.ProxyCount);
        Assert.Equal(5, result.HostedWorkspaceCount);
        Assert.Equal(3, result.QueueDepth);
        Assert.Equal(4, result.FanoutDepth);
        Assert.Equal(1, result.ConflictCount);
        Assert.Equal("stale", result.StaleReadStatus);
    }

    [Fact]
    public async System.Threading.Tasks.Task EnableAsyncPostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.EnableAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/enable", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task DisableAsyncPostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":false,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.DisableAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/disable", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.False(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListTargetsAsyncSendsGet()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"name":"remote1","baseUrl":"http://r:7148","hasApiKey":false,"isDefault":true}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListTargetsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/targets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("remote1", result[0].Name);
        Assert.True(result[0].IsDefault);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddTargetAsyncPostsBodyAndDeserializes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"name":"new-target","baseUrl":"http://r:7148","hasApiKey":true,"isDefault":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AddTargetAsync(new FederationTargetAddRequest
        {
            Name = "new-target",
            BaseUrl = "http://r:7148",
            ApiKey = "secret"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/targets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("new-target", handler.LastRequestBody);
        Assert.Equal("new-target", result.Name);
        Assert.True(result.HasApiKey);
    }

    [Fact]
    public async System.Threading.Tasks.Task RemoveTargetAsyncSendsDelete()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, "");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var status = await client.RemoveTargetAsync("old-target", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/targets/old-target", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(HttpStatusCode.NoContent, status);
    }

    [Fact]
    public async System.Threading.Tasks.Task SetDefaultTargetAsyncPostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.SetDefaultTargetAsync("primary", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/targets/primary/set-default", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task ClearDefaultTargetAsyncSendsDelete()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ClearDefaultTargetAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/targets/default", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddRouteAsyncPostsBodyAndDeserializes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"workspacePath":"C:\\proj","targetName":"remote1"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AddRouteAsync(new WorkspaceRouteRequest
        {
            WorkspacePath = @"C:\proj",
            TargetName = "remote1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/routes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("remote1", result[0].TargetName);
    }

    [Fact]
    public async System.Threading.Tasks.Task RemoveRouteAsyncSendsDeleteWithBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, "");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var status = await client.RemoveRouteAsync(new WorkspaceRouteRequest
        {
            WorkspacePath = @"C:\proj",
            TargetName = "remote1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/routes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(HttpStatusCode.NoContent, status);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetConnectionAsyncSendsWorkspaceName()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"baseUrl":"http://host:7147","port":7147,"apiKey":"ws-token"}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetConnectionAsync("MyProject", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("workspaceName=MyProject", handler.LastRequest.RequestUri!.Query);
        Assert.Equal(7147, result.Port);
        Assert.Equal("ws-token", result.ApiKey);
    }

    [Fact]
    public async System.Threading.Tasks.Task DiscoverFromTunnelsAsyncPostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"discovered":1,"targets":[{"name":"ngrok","baseUrl":"https://abc.ngrok.io","hasApiKey":false,"isDefault":false}]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.DiscoverFromTunnelsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/targets/discover-from-tunnels", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(1, result.Discovered);
        Assert.Single(result.Targets);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListProxiesAsyncGetsProxyInventory()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"proxyId":"PAYTON-LEGION2","displayName":"PAYTON-LEGION2","role":"LocalProxy","baseUrl":"http://PAYTON-LEGION2:7147","status":"online","workspaceCount":1}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListProxiesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/proxies", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("PAYTON-LEGION2", result[0].ProxyId);
        Assert.Equal(1, result[0].WorkspaceCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task EnrollProxyAsyncPostsEnrollmentPayload()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"proxyId":"PAYTON-LEGION2","accepted":true,"serverTimeUtc":"2026-05-21T22:00:00Z","heartbeatSeconds":30}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.EnrollProxyAsync(new FederationEnrollmentRequest
        {
            ProxyId = "PAYTON-LEGION2",
            DisplayName = "PAYTON-LEGION2",
            BaseUrl = "http://PAYTON-LEGION2:7147",
            EnrollmentToken = "secret",
            Workspaces =
            [
                new FederationWorkspaceRegistrationRequest
                {
                    WorkspaceName = "QBrainAi",
                    WorkspacePath = @"Q:\__mcp_unit_test__\QBrainAi",
                    Version = "v1",
                },
            ],
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/proxies/enroll", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2", handler.LastRequestBody);
        Assert.Contains("QBrainAi", handler.LastRequestBody);
        Assert.True(result.Accepted);
        Assert.Equal("PAYTON-LEGION2", result.ProxyId);
        Assert.Equal(30, result.HeartbeatSeconds);
    }

    [Fact]
    public async System.Threading.Tasks.Task HeartbeatAsyncPostsProxyHeartbeat()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"proxyId":"PAYTON-LEGION2","recordedAtUtc":"2026-05-21T22:00:00Z","queueDepth":2,"conflictCount":1}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.HeartbeatAsync("PAYTON-LEGION2", new FederationHeartbeatRequest
        {
            Status = "online",
            Workspaces =
            [
                new FederationWorkspaceRegistrationRequest
                {
                    WorkspaceName = "QBrainAi",
                    WorkspacePath = @"Q:\__mcp_unit_test__\QBrainAi",
                },
            ],
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/proxies/PAYTON-LEGION2/heartbeat", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"status\":\"online\"", handler.LastRequestBody);
        Assert.Equal(2, result.QueueDepth);
        Assert.Equal(1, result.ConflictCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task RegisterWorkspaceAsyncPostsProxyWorkspace()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"globalWorkspaceId":"PAYTON-LEGION2:mcpserver","proxyId":"PAYTON-LEGION2","workspaceName":"QBrainAi","workspacePath":"Q:\\__mcp_unit_test__\\QBrainAi","isEnabled":true,"version":"v1","lastSeenUtc":"2026-05-21T22:00:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.RegisterWorkspaceAsync("PAYTON-LEGION2", new FederationWorkspaceRegistrationRequest
        {
            GlobalWorkspaceId = "PAYTON-LEGION2:mcpserver",
            WorkspaceName = "QBrainAi",
            WorkspacePath = @"Q:\__mcp_unit_test__\QBrainAi",
            IsEnabled = true,
            Version = "v1",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/proxies/PAYTON-LEGION2/workspaces", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2:mcpserver", handler.LastRequestBody);
        Assert.Equal("PAYTON-LEGION2:mcpserver", result.GlobalWorkspaceId);
        Assert.Equal("PAYTON-LEGION2", result.ProxyId);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListWorkspacesAsyncSendsProxyFilter()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"globalWorkspaceId":"PAYTON-LEGION2:mcpserver","proxyId":"PAYTON-LEGION2","workspaceName":"QBrainAi","workspacePath":"Q:\\__mcp_unit_test__\\QBrainAi","isEnabled":true,"lastSeenUtc":"2026-05-21T22:00:00Z"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListWorkspacesAsync("PAYTON-LEGION2", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/workspaces", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Single(result);
        Assert.Equal("QBrainAi", result[0].WorkspaceName);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetQueueStatusAsyncSendsProxyFilter()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"proxyId":"PAYTON-LEGION2","queueDepth":2,"conflictCount":1,"fanoutDepth":3}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetQueueStatusAsync("PAYTON-LEGION2", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/queue", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Equal(2, result.QueueDepth);
        Assert.Equal(1, result.ConflictCount);
        Assert.Equal(3, result.FanoutDepth);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListConflictsAsyncSendsFilters()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"conflictId":"fedconf-1","operationId":"op-1","proxyId":"PAYTON-LEGION2","domain":"todo","resolutionStatus":"open","createdAtUtc":"2026-05-21T22:00:00Z"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListConflictsAsync("PAYTON-LEGION2", openOnly: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/conflicts", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Contains("openOnly=True", handler.LastRequest.RequestUri.Query);
        Assert.Single(result);
        Assert.Equal("fedconf-1", result[0].ConflictId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAdapterCoverageAsyncGetsCoverageDiagnostics()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"domain":"todo","covered":true,"localOnly":false,"applySupported":true}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetAdapterCoverageAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/adapters", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("todo", result[0].Domain);
        Assert.True(result[0].ApplySupported);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSyncItemsAsyncSendsProxyAndSequenceQuery()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"sequence":7,"operationId":"op-1","proxyId":"PAYTON-LEGION2","domain":"todo"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetSyncItemsAsync("PAYTON-LEGION2", 6, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/sync", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Contains("afterSequence=6", handler.LastRequest.RequestUri.Query);
        Assert.Single(result);
        Assert.Equal(7, result[0].Sequence);
    }

    [Fact]
    public async System.Threading.Tasks.Task AcknowledgeSyncAsyncPostsRecipientAck()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"operationId":"op-1","status":"applied","created":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AcknowledgeSyncAsync(7, new FederationSyncAckRequest
        {
            ProxyId = "PAYTON-LEGION2",
            Status = "applied",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/sync/7/ack", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2", handler.LastRequestBody);
        Assert.Equal("applied", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task RecordOperationAsyncPostsOperationIntake()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"operationId":"op-1","status":"accepted","created":true}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/operations", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2", handler.LastRequestBody);
        Assert.True(result.Created);
    }

    [Fact]
    public async System.Threading.Tasks.Task AcknowledgeOperationAsyncPostsOperationAck()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"operationId":"op-1","status":"applied","created":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AcknowledgeOperationAsync("op-1", new FederationOperationAckRequest
        {
            Status = "applied",
            HubVersion = "v2",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/operations/op-1/ack", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"status\":\"applied\"", handler.LastRequestBody);
        Assert.Contains("\"hubVersion\":\"v2\"", handler.LastRequestBody);
        Assert.Equal("applied", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task RecordEnvelopeAsyncPostsSignedEnvelope()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"operationId":"op-1","status":"applied","created":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.RecordEnvelopeAsync(new FederationExecutionEnvelope
        {
            EnvelopeId = "env-1",
            SourceProxyId = "PAYTON-LEGION2",
            Operation = new FederationOperationRequest
            {
                OperationId = "op-1",
                ProxyId = "PAYTON-LEGION2",
                Domain = "todo",
            },
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/envelopes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("env-1", handler.LastRequestBody);
        Assert.Equal("applied", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task ResolveConflictAsyncPostsResolution()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"conflictId":"fedconf-1","operationId":"op-1","proxyId":"PAYTON-LEGION2","domain":"todo","resolutionStatus":"hub_wins","createdAtUtc":"2026-05-21T00:00:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ResolveConflictAsync("fedconf-1", new FederationConflictResolutionRequest
        {
            ResolutionStatus = "hub_wins",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/conflicts/fedconf-1/resolve", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("hub_wins", handler.LastRequestBody);
        Assert.Equal("fedconf-1", result.ConflictId);
    }

    [Fact]
    public async System.Threading.Tasks.Task PushAsyncNoFilterPostsEmptyTypes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"succeeded":5,"failed":0,"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.PushAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/federation/push", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(5, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async System.Threading.Tasks.Task PushAsyncWithTypeFilterPostsTypes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"succeeded":3,"failed":1,"errors":["oops"]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.PushAsync(["todos"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("todos", handler.LastRequestBody);
        Assert.Equal(3, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async System.Threading.Tasks.Task FederationClientExposedOnFacade()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":false,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var facade = new QBrainAiClient(http, DefaultOptions);

        Assert.NotNull(facade.Federation);

        var result = await facade.Federation.GetStatusAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.Enabled);
    }
}
