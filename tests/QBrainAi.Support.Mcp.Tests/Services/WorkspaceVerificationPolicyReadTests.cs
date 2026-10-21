// SPDX-License-Identifier: Apache-2.0
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using QBrainAi.Client;
using QBrainAi.Client.Models;
using QBrainAi.Repl.Core;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Middleware;
using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-QB-011/012: configured server policy, scoped reads, controller failures and typed-client authentication.</summary>
public sealed class WorkspaceVerificationPolicyReadTests
{
    private const string Workspace = "/tests/verification-policy";
    private static readonly string Challenge = new('A', 64);

    private static WorkspaceVerificationPolicyDocument Document() => new()
    {
        WorkspacePath = Workspace,
        Policy = new()
        {
            SchemaVersion = 1, ContinuationLimit = 3,
            Checks = [new() { Id = "build", Argv = ["bun", "run", "build:sdk"], Cwd = ".", Triggers = ["code"], Required = true, SuiteKind = "build", Scope = "turn" }],
            PhaseGates = [new() { TodoId = "QB-AGENT-001", TaskPrefixes = ["P1:"], OnDone = true }],
        },
    };

    private static ConfigurationRoot Configuration(params WorkspaceVerificationPolicyDocument[] documents)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { QBrainAi = new { VerificationPolicies = documents } });
        return (ConfigurationRoot)new ConfigurationBuilder().AddJsonStream(new MemoryStream(bytes)).Build();
    }

    private static (WorkspaceVerificationPolicyController Controller, WorkspaceTokenService Tokens, WorkspaceContext Workspace) Controller(IConfiguration configuration)
    {
        var tokens = new WorkspaceTokenService();
        tokens.GenerateToken(Workspace);
        var workspace = new WorkspaceContext { WorkspacePath = Workspace };
        var controller = new WorkspaceVerificationPolicyController(configuration, workspace, tokens)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        return (controller, tokens, workspace);
    }

    /// <summary>The typed client verifies a fresh server response through its actual HTTP serialization boundary.</summary>
    [Fact]
    public async Task TypedClient_ReadsAuthenticatedConfiguredPolicy()
    {
        using var configuration = Configuration(Document());
        var fixture = Controller(configuration);
        using var handler = new PolicyHandler(fixture.Controller);
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://policy.invalid"), WorkspacePath = Workspace, ApiKey = fixture.Tokens.GetToken(Workspace),
        });
        var result = await client.GetVerificationPolicyAsync(TestContext.Current.CancellationToken);
        Assert.Equal(Workspace, result.Document.WorkspacePath);
        Assert.Equal(["bun", "run", "build:sdk"], result.Document.Policy.Checks[0].Argv);
        Assert.True(result.Document.Policy.PhaseGates[0].OnDone);
        Assert.Equal(64, result.PolicyDigest.Length);
        Assert.Equal(Workspace, handler.RequestWorkspace);
    }

    /// <summary>The supported REPL passthrough unwraps the actual typed task and preserves authenticated data.</summary>
    [Fact]
    public async Task ReplPassthrough_ReturnsVerifiedSnapshotWithFreshChallenges()
    {
        using var configuration = Configuration(Document());
        var fixture = Controller(configuration);
        using var handler = new PolicyHandler(fixture.Controller);
        using var http = new HttpClient(handler);
        var client = new QBrainAiClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://policy.invalid"), WorkspacePath = Workspace, ApiKey = fixture.Tokens.GetToken(Workspace),
        });
        var repl = new GenericClientPassthrough(client);
        var first = Assert.IsType<WorkspaceVerificationPolicySnapshot>(await repl.InvokeAsync(
            "Workspace", "GetVerificationPolicyAsync", [], TestContext.Current.CancellationToken));
        var second = Assert.IsType<WorkspaceVerificationPolicySnapshot>(await repl.InvokeAsync(
            "Workspace", "GetVerificationPolicyAsync", [], TestContext.Current.CancellationToken));
        Assert.Equal(Workspace, first.Document.WorkspacePath);
        Assert.Equal(first.PolicyDigest, second.PolicyDigest);
        Assert.NotEqual(first.Attestation.Challenge, second.Attestation.Challenge);
        Assert.Equal("QB-AGENT-001", second.Document.Policy.PhaseGates[0].TodoId);
    }

    /// <summary>A tampered successful HTTP response is rejected through the supported REPL read surface.</summary>
    [Fact]
    public async Task ReplPassthrough_RejectsTamperedHttpResponse()
    {
        using var configuration = Configuration(Document());
        var fixture = Controller(configuration);
        using var handler = new PolicyHandler(fixture.Controller, envelope => envelope with { Signature = new string('0', 64) });
        using var http = new HttpClient(handler);
        var client = new QBrainAiClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://policy.invalid"), WorkspacePath = Workspace, ApiKey = fixture.Tokens.GetToken(Workspace),
        });
        var repl = new GenericClientPassthrough(client);
        await Assert.ThrowsAsync<ArgumentException>(() => repl.InvokeAsync(
            "Workspace", "GetVerificationPolicyAsync", [], TestContext.Current.CancellationToken));
    }
    /// <summary>An explicitly empty canonical section must not activate a legacy policy.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LegacyFallback_RequiresCanonicalSectionToBeAbsent(bool canonicalPresent)
    {
        using var source = Configuration(Document());
        var legacy = source.AsEnumerable().Where(entry => entry.Value is not null)
            .ToDictionary(entry => entry.Key.Replace("QBrainAi:", "Mcp:", StringComparison.Ordinal), entry => entry.Value);
        using var configuration = canonicalPresent ? Configuration()
            : (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection().Build();
        foreach (var entry in legacy) configuration[entry.Key] = entry.Value;
        var fixture = Controller(configuration);
        var result = Assert.IsAssignableFrom<ObjectResult>(fixture.Controller.Get(Challenge).Result);
        Assert.Equal(canonicalPresent ? 503 : 200, result.StatusCode);
    }
    /// <summary>Reads observe updated operator configuration and do not return a stale signed policy.</summary>
    [Fact]
    public void ChangedConfiguration_ChangesDigest()
    {
        using var configuration = Configuration(Document());
        var fixture = Controller(configuration);
        var first = Assert.IsType<WorkspaceVerificationPolicyEnvelope>(Assert.IsType<OkObjectResult>(fixture.Controller.Get(Challenge).Result).Value);
        configuration["QBrainAi:VerificationPolicies:0:Policy:Checks:0:Argv:2"] = "types";
        var second = Assert.IsType<WorkspaceVerificationPolicyEnvelope>(Assert.IsType<OkObjectResult>(fixture.Controller.Get(Challenge).Result).Value);
        Assert.NotEqual(first.PolicyDigest, second.PolicyDigest);
    }

    /// <summary>Missing, invalid and duplicate scoped policy entries produce an unavailable result, never defaults.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("invalid")]
    [InlineData("other-workspace")]
    public void InvalidConfiguration_FailsClosed(string failure)
    {
        var document = Document();
        using var configuration = Configuration(failure switch
        {
            "missing" => [],
            "duplicate" => [document, document],
            "invalid" => [document with { Policy = document.Policy with { ContinuationLimit = 0 } }],
            _ => [document with { WorkspacePath = "/tests/another-workspace" }],
        });
        var fixture = Controller(configuration);
        var result = Assert.IsType<ObjectResult>(fixture.Controller.Get(Challenge).Result);
        Assert.Equal(503, result.StatusCode);
    }

    /// <summary>Anonymous tokens are refused whether detected by context or by auth middleware after explicit routing.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnonymousPolicyRead_IsForbidden(bool contextFlag)
    {
        using var configuration = Configuration(Document());
        var fixture = Controller(configuration);
        if (contextFlag) fixture.Workspace.IsDefaultKey = true;
        else fixture.Controller.HttpContext.Items[WorkspaceAuthMiddleware.IsDefaultKeyItem] = true;
        Assert.Equal(403, Assert.IsType<ObjectResult>(fixture.Controller.Get(Challenge).Result).StatusCode);
    }

    /// <summary>A malformed request challenge is rejected independently of policy availability.</summary>
    [Fact]
    public void InvalidChallenge_IsBadRequest()
    {
        using var configuration = Configuration();
        var fixture = Controller(configuration);
        Assert.IsType<BadRequestObjectResult>(fixture.Controller.Get("not-a-challenge").Result);
    }

    /// <summary>Unknown configuration keys cannot silently disappear from the signed administrator policy.</summary>
    [Fact]
    public void UnknownPolicyField_IsUnavailable()
    {
        using var configuration = Configuration(Document());
        configuration["QBrainAi:VerificationPolicies:0:Policy:UncheckedOverride"] = "true";
        var fixture = Controller(configuration);
        Assert.Equal(503, Assert.IsType<ObjectResult>(fixture.Controller.Get(Challenge).Result).StatusCode);
    }

    private sealed class PolicyHandler(WorkspaceVerificationPolicyController controller, Func<WorkspaceVerificationPolicyEnvelope, WorkspaceVerificationPolicyEnvelope>? transform = null) : HttpMessageHandler
    {
        public string? RequestWorkspace { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/qbrainai/workspace/verification-policy", request.RequestUri!.AbsolutePath);
            RequestWorkspace = request.Headers.GetValues("X-Workspace-Path").Single();
            var challenge = Uri.UnescapeDataString(request.RequestUri.Query["?challenge=".Length..]);
            var result = Assert.IsType<OkObjectResult>(controller.Get(challenge).Result);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(transform is null ? Assert.IsType<WorkspaceVerificationPolicyEnvelope>(result.Value) : transform(Assert.IsType<WorkspaceVerificationPolicyEnvelope>(result.Value))),
            });
        }
    }
}
