using System.Net;
using System.Text;
using McpServer.Client;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TR-MCP-TRIAGE-003 follow-up (triage-report-f77331f9a33e4bd0ae4f55f0470743ed) /
/// TEST-MCP-REQWS-001: an explicit <c>workspacePath</c> on
/// <c>workflow.requirements.generateDocument</c> must override the repl session's bound
/// workspace instead of being silently ignored (which previously exported another
/// workspace's requirements). Uses the real RequirementsWorkflow over a RequirementsClient
/// with a capturing HTTP handler, plus a dispatcher passthrough check with a substituted
/// workflow.
/// </summary>
public sealed class RequirementsWorkflowWorkspaceOverrideTests
{
    private const string BoundWorkspace = @"F:\GitHub\MouseKeyProxy";
    private const string OverrideWorkspace = @"F:\GitHub\McpServer";

    /// <summary>An explicit workspacePath replaces the bound X-Workspace-Path header on the generate call.</summary>
    [Fact]
    public async Task GenerateDocumentAsync_WorkspacePathOverride_ReplacesWorkspaceHeader()
    {
        var handler = new CapturingHandler();
        var workflow = BuildWorkflow(handler);

        await workflow.GenerateDocumentAsync("markdown", "fr", OverrideWorkspace, TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.Contains("/mcpserver/requirements/generate", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal(OverrideWorkspace, Assert.Single(handler.LastRequest.Headers.GetValues("X-Workspace-Path")));
    }

    /// <summary>Without an override the bound workspace header is preserved.</summary>
    [Fact]
    public async Task GenerateDocumentAsync_NoOverride_KeepsBoundWorkspaceHeader()
    {
        var handler = new CapturingHandler();
        var workflow = BuildWorkflow(handler);

        await workflow.GenerateDocumentAsync("markdown", "fr", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(BoundWorkspace, Assert.Single(handler.LastRequest!.Headers.GetValues("X-Workspace-Path")));
    }

    /// <summary>The dispatcher forwards the workspacePath param to the requirements workflow.</summary>
    [Fact]
    public async Task Dispatch_GenerateDocumentWithWorkspacePath_ForwardsOverrideToWorkflow()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        workflow.GenerateDocumentAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<IDocumentGenerationResult>()));
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            requirementsWorkflow: workflow);

        var response = await sut.DispatchAsync(new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-20260714T190000Z-reqws-001",
                Method = RequirementsCommandShapes.GenerateDocumentMethod,
                Params = new Dictionary<string, object?>
                {
                    ["format"] = "wiki",
                    ["docType"] = "all",
                    ["workspacePath"] = OverrideWorkspace,
                },
            },
        }, TestContext.Current.CancellationToken);

        Assert.Equal("result", response.Type);
        await workflow.Received(1).GenerateDocumentAsync("wiki", "all", OverrideWorkspace, Arg.Any<CancellationToken>());
    }

    private static RequirementsWorkflow BuildWorkflow(HttpMessageHandler handler)
    {
        var client = new RequirementsClient(new HttpClient(handler), new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = BoundWorkspace,
        });
        return new RequirementsWorkflow(client);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("# Functional Requirements", Encoding.UTF8, "text/markdown"),
            });
        }
    }
}
