using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>Tests for typed brain-slot REST client routing and payload contracts.</summary>
public sealed class BrainSlotClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
        WorkspacePath = @"E:\github\McpServer",
    };

    /// <summary>ListAsync builds the expected route and deserializes brain-slot definitions.</summary>
    [Fact]
    public async System.Threading.Tasks.Task ListAsync_SendsExpectedRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            [{"slotId":"left-main","role":"LeftHemisphere","providerKind":"OpenAI","modelId":"gpt-5.4","credentialReference":"env:OPENAI_API_KEY","partyId":"brain-slot:left-hemisphere","enabled":true,"timeoutSeconds":30,"maxOutputTokens":1024,"createdAtUtc":"2026-06-15T00:00:00Z","updatedAtUtc":"2026-06-15T00:00:00Z"}]
            """);
        using var http = new HttpClient(handler);
        var client = new BrainSlotClient(http, DefaultOptions);

        var result = await client.ListAsync().ConfigureAwait(true);

        Assert.Single(result);
        Assert.Equal("LeftHemisphere", result[0].Role);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/brain-slots", handler.LastRequest.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.Contains("X-Workspace-Path"));
    }

    /// <summary>UpsertAsync sends mutable slot fields without requiring raw credentials.</summary>
    [Fact]
    public async System.Threading.Tasks.Task UpsertAsync_SerializesCredentialReferenceOnly()
    {
        var response = new BrainSlotDto
        {
            SlotId = "curiosity-main",
            Role = "CuriosityEngine",
            ProviderKind = "OpenAICompatible",
            ModelId = "external-curiosity",
            Endpoint = "https://models.example.test/v1",
            CredentialReference = "config:BrainSlots:CuriosityKey",
            PartyId = "brain-slot:curiosity-engine",
            Enabled = true,
            TimeoutSeconds = 45,
            MaxOutputTokens = 2048,
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-15T00:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-06-15T00:00:00Z"),
        };
        var handler = new MockHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        using var http = new HttpClient(handler);
        var client = new BrainSlotClient(http, DefaultOptions);

        await client.UpsertAsync("curiosity main", new UpsertBrainSlotRequest
        {
            Role = "CuriosityEngine",
            ProviderKind = "OpenAICompatible",
            ModelId = "external-curiosity",
            Endpoint = "https://models.example.test/v1",
            CredentialReference = "config:BrainSlots:CuriosityKey",
            PartyId = "brain-slot:curiosity-engine",
            Enabled = true,
            ReplaceExisting = true,
        }).ConfigureAwait(true);

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/brain-slots/curiosity%20main", handler.LastRequest.RequestUri!.OriginalString);
        Assert.Contains("\"credentialReference\":\"config:BrainSlots:CuriosityKey\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-", handler.LastRequestBody, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>InvokeAsync posts the turn-bound invocation request and reads committed output.</summary>
    [Fact]
    public async System.Threading.Tasks.Task InvokeAsync_PostsInvocationBody()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"status":"committed","reason":"None","slotId":"curiosity-main","role":"CuriosityEngine","transactionId":"txn-1","diffgramId":"diff-1","modelId":"external-curiosity","output":"committed output","startedAtUtc":"2026-06-15T00:00:00Z","completedAtUtc":"2026-06-15T00:00:01Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new BrainSlotClient(http, DefaultOptions);

        var result = await client.InvokeAsync("curiosity-main", new BrainSlotInvokeRequest
        {
            Input = "find gaps",
            TurnId = "turn-1",
            AdmitToGraphRag = true,
            Metadata = new Dictionary<string, string> { ["source"] = "test" },
        }).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.Equal("committed output", result.Output);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/brain-slots/curiosity-main/invoke", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("\"admitToGraphRag\":true", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>OrchestrateAsync posts the full Quad-Brain request to the orchestration route.</summary>
    [Fact]
    public async System.Threading.Tasks.Task OrchestrateAsync_PostsExpectedRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"status":"committed","reason":"None","output":"final","transactionId":"txn-aot","diffgramId":"diff-aot","roleResults":[],"startedAtUtc":"2026-06-15T00:00:00Z","completedAtUtc":"2026-06-15T00:00:01Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new BrainSlotClient(http, DefaultOptions);

        var result = await client.OrchestrateAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide",
            TurnId = "turn-quad",
            AdmitCuriosityToGraphRag = true,
        }).ConfigureAwait(true);

        Assert.Equal("final", result.Output);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/brain-slots/orchestrate", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("\"admitCuriosityToGraphRag\":true", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>ReconcileAotAsync posts committed role evidence to the AoT reconciliation route.</summary>
    [Fact]
    public async System.Threading.Tasks.Task ReconcileAotAsync_PostsExpectedRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"status":"committed","reason":"None","slotId":"arbiter-main","transactionId":"txn-aot","diffgramId":"diff-aot","output":"approved","startedAtUtc":"2026-06-15T00:00:00Z","completedAtUtc":"2026-06-15T00:00:01Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new BrainSlotClient(http, DefaultOptions);

        var result = await client.ReconcileAotAsync(new AotReconciliationRequest
        {
            Input = "decide",
            LeftOutput = "left",
            RightOutput = "right",
            CuriosityOutput = "curiosity",
        }).ConfigureAwait(true);

        Assert.Equal("approved", result.Output);
        Assert.Equal("http://localhost:7147/mcpserver/brain-slots/aot/reconcile", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("\"leftOutput\":\"left\"", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>UpdateWeightsAsync posts safety-gated weight updates to the weight route.</summary>
    [Fact]
    public async System.Threading.Tasks.Task UpdateWeightsAsync_PostsExpectedRoute()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"status":"committed","reason":"None","transactionId":"txn-weight","diffgramId":"diff-weight","snapshots":[],"startedAtUtc":"2026-06-15T00:00:00Z","completedAtUtc":"2026-06-15T00:00:01Z"}
            """);
        using var http = new HttpClient(handler);
        var client = new BrainSlotClient(http, DefaultOptions);

        var result = await client.UpdateWeightsAsync(new QuadBrainWeightUpdateRequest
        {
            RoleWeights = new Dictionary<string, double> { ["LeftHemisphere"] = 1.2 },
            ExpectedVersions = new Dictionary<string, int> { ["LeftHemisphere"] = 0 },
            ReasonText = "approved",
            AotApproved = true,
            AdminApproved = true,
            SafetyGatesPassed = true,
        }).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.Equal("http://localhost:7147/mcpserver/brain-slots/weights/update", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("\"roleWeights\":{\"LeftHemisphere\":1.2}", handler.LastRequestBody, StringComparison.Ordinal);
    }
}
