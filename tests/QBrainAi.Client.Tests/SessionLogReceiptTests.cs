using System.Net;
using System.Text.Json;
using QBrainAi.Client.Models;
using Xunit;

namespace QBrainAi.Client.Tests;

/// <summary>
/// TEST-MCP-SESSIONLIFE-004: Mock HTTP receipts verify that the typed client preserves
/// durable-write evidence for the REPL and does not invent success for legacy responses.
/// </summary>
public sealed class SessionLogReceiptTests
{
    /// <summary>Round-trips primary and degraded receipts without losing outcome or request identity.</summary>
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    public async Task SubmitAsync_PreservesPersistenceReceipt(bool persisted, bool degraded, bool queued)
    {
        const string requestId = "req-20261001T190000Z-receipt";
        var response = JsonSerializer.Serialize(new
        {
            id = 42, sourceType = "Codex", sessionId = "Codex-20261001T190000Z-receipt",
            requestId, persisted, degraded, queued
        });
        using var handler = new MockHttpHandler(HttpStatusCode.Created, response);
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"), ApiKey = "synthetic-test-key"
        });

        var result = await client.SubmitAsync(new UnifiedSessionLogDto
        {
            SourceType = "Codex", SessionId = "Codex-20261001T190000Z-receipt"
        }, cancellationToken: TestContext.Current.CancellationToken);

        var receipt = JsonSerializer.SerializeToElement(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(persisted, receipt.GetProperty("persisted").GetBoolean());
        Assert.Equal(degraded, receipt.GetProperty("degraded").GetBoolean());
        Assert.Equal(queued, receipt.GetProperty("queued").GetBoolean());
        Assert.Equal(requestId, receipt.GetProperty("requestId").GetString());
    }

    /// <summary>A legacy server's row ID alone must never become a durable-write acknowledgement.</summary>
    [Fact]
    public void LegacyReceipt_DoesNotImplyPersistence()
    {
        var result = JsonSerializer.Deserialize<SessionLogSubmitResult>("{\"id\":42}");
        var receipt = JsonSerializer.SerializeToElement(result);
        Assert.False(receipt.TryGetProperty("persisted", out var persisted) && persisted.GetBoolean());
    }
}
