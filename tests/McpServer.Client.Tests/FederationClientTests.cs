using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// Unit tests for <see cref="FederationClient"/>. Validates correct HTTP method,
/// URL construction, request/response serialization for all federation endpoints.
/// FR-MCP-077, FR-MCP-085.
/// </summary>
public sealed class FederationClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task GetStatusAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"enabled":true,"role":"Hub","configuredRole":"Hub","hubBaseUrl":"http://hub:7147","proxyId":"PAYTON-LEGION2","proxyCount":2,"hostedWorkspaceCount":5,"queueDepth":3,"fanoutDepth":4,"conflictCount":1,"staleReadStatus":"stale","targets":[],"workspaceRoutes":[]}
            """);
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetStatusAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("/mcpserver/federation/status", handler.LastRequest.RequestUri!.AbsolutePath);
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
    public async System.Threading.Tasks.Task EnableAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.EnableAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/enable", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task DisableAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":false,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.DisableAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/disable", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.False(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListTargetsAsync_SendsGet()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"name":"remote1","baseUrl":"http://r:7148","hasApiKey":false,"isDefault":true}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListTargetsAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("remote1", result[0].Name);
        Assert.True(result[0].IsDefault);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddTargetAsync_PostsBodyAndDeserializes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"name":"new-target","baseUrl":"http://r:7148","hasApiKey":true,"isDefault":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AddTargetAsync(new FederationTargetAddRequest
        {
            Name = "new-target",
            BaseUrl = "http://r:7148",
            ApiKey = "secret"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("new-target", handler.LastRequestBody);
        Assert.Equal("new-target", result.Name);
        Assert.True(result.HasApiKey);
    }

    [Fact]
    public async System.Threading.Tasks.Task RemoveTargetAsync_SendsDelete()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, "");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var status = await client.RemoveTargetAsync("old-target");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/old-target", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(HttpStatusCode.NoContent, status);
    }

    [Fact]
    public async System.Threading.Tasks.Task SetDefaultTargetAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.SetDefaultTargetAsync("primary");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/primary/set-default", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task ClearDefaultTargetAsync_SendsDelete()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ClearDefaultTargetAsync();

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/default", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddRouteAsync_PostsBodyAndDeserializes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"workspacePath":"C:\\proj","targetName":"remote1"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AddRouteAsync(new WorkspaceRouteRequest
        {
            WorkspacePath = @"C:\proj",
            TargetName = "remote1"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/routes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("remote1", result[0].TargetName);
    }

    [Fact]
    public async System.Threading.Tasks.Task RemoveRouteAsync_SendsDeleteWithBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, "");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var status = await client.RemoveRouteAsync(new WorkspaceRouteRequest
        {
            WorkspacePath = @"C:\proj",
            TargetName = "remote1"
        });

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/routes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(HttpStatusCode.NoContent, status);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetConnectionAsync_SendsWorkspaceName()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"baseUrl":"http://host:7147","port":7147,"apiKey":"ws-token"}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetConnectionAsync("MyProject");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("workspaceName=MyProject", handler.LastRequest.RequestUri!.Query);
        Assert.Equal(7147, result.Port);
        Assert.Equal("ws-token", result.ApiKey);
    }

    [Fact]
    public async System.Threading.Tasks.Task DiscoverFromTunnelsAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"discovered":1,"targets":[{"name":"ngrok","baseUrl":"https://abc.ngrok.io","hasApiKey":false,"isDefault":false}]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.DiscoverFromTunnelsAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/discover-from-tunnels", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(1, result.Discovered);
        Assert.Single(result.Targets);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListProxiesAsync_GetsProxyInventory()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"proxyId":"PAYTON-LEGION2","displayName":"PAYTON-LEGION2","role":"LocalProxy","baseUrl":"http://PAYTON-LEGION2:7147","status":"online","workspaceCount":1}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListProxiesAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/proxies", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("PAYTON-LEGION2", result[0].ProxyId);
        Assert.Equal(1, result[0].WorkspaceCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task EnrollProxyAsync_PostsEnrollmentPayload()
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
                    WorkspaceName = "McpServer",
                    WorkspacePath = @"F:\GitHub\McpServer",
                    Version = "v1",
                },
            ],
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/proxies/enroll", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2", handler.LastRequestBody);
        Assert.Contains("McpServer", handler.LastRequestBody);
        Assert.True(result.Accepted);
        Assert.Equal("PAYTON-LEGION2", result.ProxyId);
        Assert.Equal(30, result.HeartbeatSeconds);
    }

    [Fact]
    public async System.Threading.Tasks.Task HeartbeatAsync_PostsProxyHeartbeat()
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
                    WorkspaceName = "McpServer",
                    WorkspacePath = @"F:\GitHub\McpServer",
                },
            ],
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/proxies/PAYTON-LEGION2/heartbeat", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"status\":\"online\"", handler.LastRequestBody);
        Assert.Equal(2, result.QueueDepth);
        Assert.Equal(1, result.ConflictCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task RegisterWorkspaceAsync_PostsProxyWorkspace()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"globalWorkspaceId":"PAYTON-LEGION2:mcpserver","proxyId":"PAYTON-LEGION2","workspaceName":"McpServer","workspacePath":"F:\\GitHub\\McpServer","isEnabled":true,"version":"v1","lastSeenUtc":"2026-05-21T22:00:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.RegisterWorkspaceAsync("PAYTON-LEGION2", new FederationWorkspaceRegistrationRequest
        {
            GlobalWorkspaceId = "PAYTON-LEGION2:mcpserver",
            WorkspaceName = "McpServer",
            WorkspacePath = @"F:\GitHub\McpServer",
            IsEnabled = true,
            Version = "v1",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/proxies/PAYTON-LEGION2/workspaces", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2:mcpserver", handler.LastRequestBody);
        Assert.Equal("PAYTON-LEGION2:mcpserver", result.GlobalWorkspaceId);
        Assert.Equal("PAYTON-LEGION2", result.ProxyId);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListWorkspacesAsync_SendsProxyFilter()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"globalWorkspaceId":"PAYTON-LEGION2:mcpserver","proxyId":"PAYTON-LEGION2","workspaceName":"McpServer","workspacePath":"F:\\GitHub\\McpServer","isEnabled":true,"lastSeenUtc":"2026-05-21T22:00:00Z"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListWorkspacesAsync("PAYTON-LEGION2");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/workspaces", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Single(result);
        Assert.Equal("McpServer", result[0].WorkspaceName);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetQueueStatusAsync_SendsProxyFilter()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"proxyId":"PAYTON-LEGION2","queueDepth":2,"conflictCount":1,"fanoutDepth":3}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetQueueStatusAsync("PAYTON-LEGION2");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/queue", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Equal(2, result.QueueDepth);
        Assert.Equal(1, result.ConflictCount);
        Assert.Equal(3, result.FanoutDepth);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListConflictsAsync_SendsFilters()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"conflictId":"fedconf-1","operationId":"op-1","proxyId":"PAYTON-LEGION2","domain":"todo","resolutionStatus":"open","createdAtUtc":"2026-05-21T22:00:00Z"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListConflictsAsync("PAYTON-LEGION2", openOnly: true);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/conflicts", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Contains("openOnly=True", handler.LastRequest.RequestUri.Query);
        Assert.Single(result);
        Assert.Equal("fedconf-1", result[0].ConflictId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAdapterCoverageAsync_GetsCoverageDiagnostics()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"domain":"todo","covered":true,"localOnly":false,"applySupported":true}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetAdapterCoverageAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/adapters", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("todo", result[0].Domain);
        Assert.True(result[0].ApplySupported);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSyncItemsAsync_SendsProxyAndSequenceQuery()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"sequence":7,"operationId":"op-1","proxyId":"PAYTON-LEGION2","domain":"todo"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetSyncItemsAsync("PAYTON-LEGION2", 6);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/sync", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("proxyId=PAYTON-LEGION2", handler.LastRequest.RequestUri.Query);
        Assert.Contains("afterSequence=6", handler.LastRequest.RequestUri.Query);
        Assert.Single(result);
        Assert.Equal(7, result[0].Sequence);
    }

    [Fact]
    public async System.Threading.Tasks.Task AcknowledgeSyncAsync_PostsRecipientAck()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"operationId":"op-1","status":"applied","created":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AcknowledgeSyncAsync(7, new FederationSyncAckRequest
        {
            ProxyId = "PAYTON-LEGION2",
            Status = "applied",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/sync/7/ack", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2", handler.LastRequestBody);
        Assert.Equal("applied", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task RecordOperationAsync_PostsOperationIntake()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"operationId":"op-1","status":"accepted","created":true}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/operations", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("PAYTON-LEGION2", handler.LastRequestBody);
        Assert.True(result.Created);
    }

    [Fact]
    public async System.Threading.Tasks.Task AcknowledgeOperationAsync_PostsOperationAck()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"operationId":"op-1","status":"applied","created":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AcknowledgeOperationAsync("op-1", new FederationOperationAckRequest
        {
            Status = "applied",
            HubVersion = "v2",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/operations/op-1/ack", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"status\":\"applied\"", handler.LastRequestBody);
        Assert.Contains("\"hubVersion\":\"v2\"", handler.LastRequestBody);
        Assert.Equal("applied", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task RecordEnvelopeAsync_PostsSignedEnvelope()
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
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/envelopes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("env-1", handler.LastRequestBody);
        Assert.Equal("applied", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task ResolveConflictAsync_PostsResolution()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"conflictId":"fedconf-1","operationId":"op-1","proxyId":"PAYTON-LEGION2","domain":"todo","resolutionStatus":"hub_wins","createdAtUtc":"2026-05-21T00:00:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ResolveConflictAsync("fedconf-1", new FederationConflictResolutionRequest
        {
            ResolutionStatus = "hub_wins",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/conflicts/fedconf-1/resolve", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("hub_wins", handler.LastRequestBody);
        Assert.Equal("fedconf-1", result.ConflictId);
    }

    [Fact]
    public async System.Threading.Tasks.Task PushAsync_NoFilter_PostsEmptyTypes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"succeeded":5,"failed":0,"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.PushAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/push", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(5, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async System.Threading.Tasks.Task PushAsync_WithTypeFilter_PostsTypes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"succeeded":3,"failed":1,"errors":["oops"]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.PushAsync(["todos"]);

        Assert.Contains("todos", handler.LastRequestBody);
        Assert.Equal(3, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async System.Threading.Tasks.Task FederationClient_ExposedOnFacade()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":false,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var facade = new McpServerClient(http, DefaultOptions);

        Assert.NotNull(facade.Federation);

        var result = await facade.Federation.GetStatusAsync();
        Assert.False(result.Enabled);
    }
}
