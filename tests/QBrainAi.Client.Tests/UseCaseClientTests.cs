using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using QBrainAi.Client.Models;
using Xunit;

namespace QBrainAi.Client.Tests;

/// <summary>
/// TEST-MCP-USECASE-004 / TR-MCP-CLIENT-001: Typed UseCaseClient route and payload contracts via MockHttpHandler.
/// </summary>
public sealed class UseCaseClientTests
{
    private static readonly QBrainAiClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
        WorkspacePath = @"E:\github\QBrainAi",
    };

    /// <summary>ListAsync builds the expected query string and deserializes summaries.</summary>
    [Fact]
    public async System.Threading.Tasks.Task ListAsync_SendsExpectedQuery()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"useCaseId":1,"title":"Login","priority":1,"createdAtUtc":"2026-08-07T00:00:00Z","updatedAtUtc":"2026-08-07T00:00:00Z"}]""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var result = await client.ListAsync("Login", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Single(result);
        Assert.Equal(1, result[0].UseCaseId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/qbrainai/usecases?title=Login", handler.LastRequest.RequestUri!.OriginalString);
        Assert.True(handler.LastRequest.Headers.Contains("X-Workspace-Path"));
    }

    /// <summary>GetAsync uses the id route segment and binds version/approval/product from server DTO.</summary>
    [Fact]
    public async System.Threading.Tasks.Task GetAsync_SendsCorrectUrl_AndBindsExpandedFields()
    {
        // Live server UseCaseDetailDto shape (versionNumber/approvalStatus/productKey).
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"useCaseId":9,"title":"Auth","workspaceId":"ws","priority":0,"versionNumber":2,"approvalStatus":"Approved","productKey":"prod-a","createdAtUtc":"2026-08-07T00:00:00Z","updatedAtUtc":"2026-08-07T00:00:00Z","actors":[],"flows":[],"frLinks":[]}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var result = await client.GetAsync(9, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(9, result.UseCaseId);
        Assert.Equal(2, result.VersionNumber);
        Assert.Equal("Approved", result.ApprovalStatus);
        Assert.Equal("prod-a", result.ProductKey);
        Assert.Contains("/qbrainai/usecases/9", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    /// <summary>CreateAsync posts the create body and deserializes detail.</summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_PostsRequestBody()
    {
        var response = new UseCaseDetail
        {
            UseCaseId = 3,
            Title = "Create user",
            WorkspaceId = @"E:\github\QBrainAi",
            CreatedAtUtc = DateTimeOffset.Parse("2026-08-07T00:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-08-07T00:00:00Z"),
        };
        var handler = new MockHttpHandler(HttpStatusCode.Created, JsonSerializer.Serialize(response));
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var result = await client.CreateAsync(new CreateUseCaseRequest
        {
            Title = "Create user",
            CreateBasicFlow = true,
            FrId = "FR-MCP-USECASE-001",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(3, result.UseCaseId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/qbrainai/usecases", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("Create user", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-USECASE-001", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>UpdateAsync uses the id route segment with PUT.</summary>
    [Fact]
    public async System.Threading.Tasks.Task UpdateAsync_UsesIdRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"useCaseId":5,"title":"Updated","workspaceId":"ws","priority":2,"createdAtUtc":"2026-08-07T00:00:00Z","updatedAtUtc":"2026-08-07T00:00:00Z","actors":[],"flows":[],"frLinks":[]}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var updated = await client.UpdateAsync(5, new UpdateUseCaseRequest { Title = "Updated" }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal("Updated", updated.Title);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/usecases/5", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>DeleteAsync uses the id route segment with DELETE.</summary>
    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_UsesIdRoute()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, string.Empty);
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        await client.DeleteAsync(5, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/usecases/5", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>LinkFrAsync posts to links route with string frId.</summary>
    [Fact]
    public async System.Threading.Tasks.Task LinkFrAsync_PostsToLinksRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.Created,
            """{"linkId":1,"useCaseId":2,"frId":"FR-MCP-001","linkType":"Realizes","linkOrder":0,"createdAtUtc":"2026-08-07T00:00:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var link = await client.LinkFrAsync(2, new LinkUseCaseToFrRequest
        {
            FrId = "FR-MCP-001",
            LinkType = "Realizes",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("FR-MCP-001", link.FrId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/usecases/2/links", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("FR-MCP-001", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>GetDiagramAsync requests mermaid format.</summary>
    [Fact]
    public async System.Threading.Tasks.Task GetDiagramAsync_SendsFormatQuery()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"useCaseId":4,"format":"mermaid","content":"sequenceDiagram"}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var diagram = await client.GetDiagramAsync(4, "mermaid", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("mermaid", diagram.Format);
        Assert.Contains("/qbrainai/usecases/4/diagram", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("format=mermaid", handler.LastRequest.RequestUri.Query);
    }

    /// <summary>CreateFromFrAsync posts to from-fr route with string frId.</summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateFromFrAsync_PostsToFromFrRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.Created,
            """{"useCaseId":8,"title":"From FR","workspaceId":"ws","priority":0,"createdAtUtc":"2026-08-07T00:00:00Z","updatedAtUtc":"2026-08-07T00:00:00Z","actors":[],"flows":[],"frLinks":[]}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var detail = await client.CreateFromFrAsync(
            "FR-MCP-USECASE-004",
            new CreateUseCaseFromFrRequest { Title = "From FR" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(8, detail.UseCaseId);
        Assert.Contains("/qbrainai/usecases/from-fr/FR-MCP-USECASE-004", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    /// <summary>
    /// GetCoverageAsync deserializes the live controller UseCaseFrCoverageDto JSON shape
    /// (not a client-only fictional shape).
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task GetCoverageAsync_BindsLiveServerCoveragePayload()
    {
        // Exact property names from usecase-rest-smoke.log COVERAGE payload / UseCaseFrCoverageDto.
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"totalUseCases":1,"totalFunctionalRequirements":1,"linkedUseCases":1,"linkedFunctionalRequirements":0,"useCasesWithoutRealizesLink":[],"functionalRequirementsWithoutRealizesUseCase":["FR-MCP-001"]}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var coverage = await client.GetCoverageAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, coverage.TotalUseCases);
        Assert.Equal(1, coverage.TotalFunctionalRequirements);
        Assert.Equal(1, coverage.LinkedUseCases);
        Assert.Equal(0, coverage.LinkedFunctionalRequirements);
        Assert.Empty(coverage.UseCasesWithoutRealizesLink);
        Assert.Single(coverage.FunctionalRequirementsWithoutRealizesUseCase);
        Assert.Equal("FR-MCP-001", coverage.FunctionalRequirementsWithoutRealizesUseCase[0]);
        Assert.Equal("http://localhost:7147/qbrainai/usecases/coverage", handler.LastRequest!.RequestUri!.ToString());
    }

    /// <summary>SetApprovalAsync posts to the approval route and returns detail with status/version.</summary>
    [Fact]
    public async System.Threading.Tasks.Task SetApprovalAsync_PostsToApprovalRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"useCaseId":3,"title":"A","workspaceId":"ws","priority":0,"versionNumber":2,"approvalStatus":"Approved","createdAtUtc":"2026-08-07T00:00:00Z","updatedAtUtc":"2026-08-07T00:00:00Z","actors":[],"flows":[],"frLinks":[]}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var detail = await client.SetApprovalAsync(
            3,
            new SetUseCaseApprovalRequest { Status = "Approved" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("Approved", detail.ApprovalStatus);
        Assert.Equal(2, detail.VersionNumber);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/usecases/3/approval", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("Approved", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>SetProductAsync posts to the product route.</summary>
    [Fact]
    public async System.Threading.Tasks.Task SetProductAsync_PostsToProductRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"useCaseId":3,"title":"A","workspaceId":"ws","priority":0,"versionNumber":1,"approvalStatus":"Draft","productKey":"prod-mcp","createdAtUtc":"2026-08-07T00:00:00Z","updatedAtUtc":"2026-08-07T00:00:00Z","actors":[],"flows":[],"frLinks":[]}""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var detail = await client.SetProductAsync(
            3,
            new SetUseCaseProductRequest { ProductKey = "prod-mcp" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("prod-mcp", detail.ProductKey);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/qbrainai/usecases/3/product", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>ListByProductAsync hits by-product route.</summary>
    [Fact]
    public async System.Threading.Tasks.Task ListByProductAsync_SendsByProductUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"useCaseId":1,"title":"P","priority":0,"createdAtUtc":"2026-08-07T00:00:00Z","updatedAtUtc":"2026-08-07T00:00:00Z"}]""");
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);

        var items = await client.ListByProductAsync("prod-mcp", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Single(items);
        Assert.Contains("/qbrainai/usecases/by-product/prod-mcp", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
    /// <summary>TEST-MCP-USECASE-021: Component update methods use stable ids and full replacement payloads.</summary>
    [Fact]
    public async System.Threading.Tasks.Task UseCaseComponentUpdateMethods_SendStableRoutesAndFullPayloads()
    {
        var actorHandler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"actorId":20,"useCaseId":125,"name":"LAB-OMARCHY TruckMate Tentacle","description":"Replacement actor","type":"Supporting","isPrimary":false}""");
        using var actorHttp = new HttpClient(actorHandler);
        var actorClient = new UseCaseClient(actorHttp, DefaultOptions);
        var actor = await actorClient.UpdateActorAsync(
            125,
            20,
            new UpdateUseCaseActorRequest
            {
                Name = "LAB-OMARCHY TruckMate Tentacle",
                Description = "Replacement actor",
                Type = "Supporting",
                IsPrimary = false,
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(20, actor.ActorId);
        Assert.Equal(125, actor.UseCaseId);
        Assert.Equal(HttpMethod.Put, actorHandler.LastRequest!.Method);
        Assert.Equal("/qbrainai/usecases/125/actors/20", actorHandler.LastRequest.RequestUri!.AbsolutePath);
        using (var actorBody = JsonDocument.Parse(actorHandler.LastRequestBody!))
        {
            Assert.Equal("LAB-OMARCHY TruckMate Tentacle", actorBody.RootElement.GetProperty("name").GetString());
            Assert.Equal("Replacement actor", actorBody.RootElement.GetProperty("description").GetString());
            Assert.Equal("Supporting", actorBody.RootElement.GetProperty("type").GetString());
            Assert.False(actorBody.RootElement.GetProperty("isPrimary").GetBoolean());
        }

        var flowHandler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"flowId":314,"useCaseId":125,"flowType":"Alternate","name":"Octopus-owned deployment","sequenceNumber":2,"steps":[]}""");
        using var flowHttp = new HttpClient(flowHandler);
        var flowClient = new UseCaseClient(flowHttp, DefaultOptions);
        var flow = await flowClient.UpdateFlowAsync(
            125,
            314,
            new UpdateUseCaseFlowRequest
            {
                FlowType = "Alternate",
                Name = "Octopus-owned deployment",
                SequenceNumber = 2,
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(314, flow.FlowId);
        Assert.Equal(125, flow.UseCaseId);
        Assert.Equal(HttpMethod.Put, flowHandler.LastRequest!.Method);
        Assert.Equal("/qbrainai/usecases/125/flows/314", flowHandler.LastRequest.RequestUri!.AbsolutePath);
        using (var flowBody = JsonDocument.Parse(flowHandler.LastRequestBody!))
        {
            Assert.Equal("Alternate", flowBody.RootElement.GetProperty("flowType").GetString());
            Assert.Equal("Octopus-owned deployment", flowBody.RootElement.GetProperty("name").GetString());
            Assert.Equal(2, flowBody.RootElement.GetProperty("sequenceNumber").GetInt32());
        }

        var stepHandler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"stepId":383,"flowId":314,"stepNumber":4,"actorId":20,"action":"Run Nuke through Octopus","systemResponse":"Deployment succeeds","dataEntities":"Release"}""");
        using var stepHttp = new HttpClient(stepHandler);
        var stepClient = new UseCaseClient(stepHttp, DefaultOptions);
        var step = await stepClient.UpdateStepAsync(
            125,
            314,
            383,
            new UpdateUseCaseStepRequest
            {
                StepNumber = 4,
                ActorId = 20,
                Action = "Run Nuke through Octopus",
                SystemResponse = "Deployment succeeds",
                DataEntities = "Release",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(383, step.StepId);
        Assert.Equal(314, step.FlowId);
        Assert.Equal(HttpMethod.Put, stepHandler.LastRequest!.Method);
        Assert.Equal("/qbrainai/usecases/125/flows/314/steps/383", stepHandler.LastRequest.RequestUri!.AbsolutePath);
        using var stepBody = JsonDocument.Parse(stepHandler.LastRequestBody!);
        Assert.Equal(4, stepBody.RootElement.GetProperty("stepNumber").GetInt32());
        Assert.Equal(20, stepBody.RootElement.GetProperty("actorId").GetInt64());
        Assert.Equal("Run Nuke through Octopus", stepBody.RootElement.GetProperty("action").GetString());
        Assert.Equal("Deployment succeeds", stepBody.RootElement.GetProperty("systemResponse").GetString());
        Assert.Equal("Release", stepBody.RootElement.GetProperty("dataEntities").GetString());
    }

    /// <summary>TEST-MCP-USECASE-021: Every component update method propagates caller cancellation.</summary>
    [Fact]
    public async System.Threading.Tasks.Task UseCaseComponentUpdateMethods_PropagateCancellation()
    {
        await AssertCancellationAsync(
            (client, token) => client.UpdateActorAsync(
                125,
                20,
                new UpdateUseCaseActorRequest { Name = "Tentacle", Type = "System" },
                token)).ConfigureAwait(true);
        await AssertCancellationAsync(
            (client, token) => client.UpdateFlowAsync(
                125,
                314,
                new UpdateUseCaseFlowRequest { FlowType = "Basic", SequenceNumber = 1 },
                token)).ConfigureAwait(true);
        await AssertCancellationAsync(
            (client, token) => client.UpdateStepAsync(
                125,
                314,
                383,
                new UpdateUseCaseStepRequest { StepNumber = 1, Action = "Deploy" },
                token)).ConfigureAwait(true);
    }

    private static async System.Threading.Tasks.Task AssertCancellationAsync(
        Func<UseCaseClient, CancellationToken, System.Threading.Tasks.Task> invoke)
    {
        var handler = new CancellationProbeHandler();
        using var http = new HttpClient(handler);
        var client = new UseCaseClient(http, DefaultOptions);
        using var cancellation = new CancellationTokenSource();

        var requestTask = invoke(client, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask).ConfigureAwait(true);
        Assert.True(handler.ObservedCancellation);
    }

    private sealed class CancellationProbeHandler : HttpMessageHandler
    {
        public bool ObservedCancellation { get; private set; }

        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ObservedCancellation = true;
                throw;
            }

            throw new InvalidOperationException("Cancellation probe unexpectedly completed.");
        }
    }
}
