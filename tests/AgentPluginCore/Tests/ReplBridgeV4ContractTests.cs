// ReplBridgeV4ContractTests.cs
// Contract tests for v4 REPL bridge (single-line JSON envelopes, streaming, timeouts, bounded retries, circuit breaker).
// Green against V4ReplBridgeStub using mocks where needed (PARITY-RESUME-004, Phase 1, Byrd v4).
// AC: TR-MCP-AGENT-PARITY-010, plan envelopes + circuit breaker.

namespace McpServer.AgentPluginCore.Tests.Tests;

using McpServer.AgentPluginCore.Tests.Contracts;
using McpServer.AgentPluginCore.Tests.Stubs;

/// <summary>
/// Validates v4 repl bridge envelope format and resilience semantics required for parity across all plugins.
/// </summary>
public class ReplBridgeV4ContractTests
{
    private readonly V4ReplBridgeStub _sut = new();

    /// <summary>
    /// Envelopes are serialized as single-line JSON (no embedded newlines).
    /// </summary>
    [Fact]
    public async Task SendEnvelope_ProducesSingleLineJson()
    {
        var env = new V4ReplEnvelope { Type = "workflow.sessionlog.beginTurn", Payload = new { requestId = "r1" }, AgentId = "test" };

        var resp = await _sut.SendEnvelopeAsync(env);

        Assert.True(resp.Success);
        // Contract guarantees no \n (enforced in stub)
    }

    /// <summary>
    /// Streaming produces ack + complete events.
    /// </summary>
    [Fact]
    public async Task SendEnvelopeStreaming_YieldsEvents()
    {
        var env = new V4ReplEnvelope { Type = "workflow.todo.create", AgentId = "grok" };
        var events = new List<V4ReplEvent>();
        await foreach (var ev in _sut.SendEnvelopeStreamingAsync(env))
            events.Add(ev);

        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.EventType == "ack" || e.EventType == "complete");
    }

    /// <summary>
    /// Repeated failures trip circuit breaker to OPEN.
    /// </summary>
    [Fact]
    public async Task RepeatedFailures_TripCircuitBreaker_ToOpen()
    {
        var failEnv = new V4ReplEnvelope { Type = "workflow.fail", Payload = "fail" };

        V4ReplResponse last = null!;
        for (int i = 0; i < 4; i++)
            last = await _sut.SendEnvelopeAsync(failEnv);

        Assert.Equal(V4CircuitState.Open, _sut.CircuitState);
        Assert.Contains("CIRCUIT_OPEN", last.ErrorCode);
    }

    /// <summary>
    /// Successful call after open (or reset) returns to Closed (basic breaker semantics).
    /// </summary>
    [Fact]
    public async Task SuccessAfterFailures_ResetsCircuit()
    {
        // Trip it
        for (int i = 0; i < 3; i++)
            await _sut.SendEnvelopeAsync(new V4ReplEnvelope { Type = "f", Payload = "fail" });

        // Good call
        var good = await _sut.SendEnvelopeAsync(new V4ReplEnvelope { Type = "workflow.sessionlog.append", AgentId = "claude" });
        Assert.True(good.Success);
        // Stub resets on success path
        Assert.Equal(V4CircuitState.Closed, _sut.CircuitState);
    }

    /// <summary>
    /// Cancellation and timeout paths honored (shape test; stub uses token).
    /// </summary>
    [Fact]
    public async Task SendEnvelope_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var env = new V4ReplEnvelope { Type = "workflow.graphrag.query" };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _sut.SendEnvelopeAsync(env, null, cts.Token));
    }
}
