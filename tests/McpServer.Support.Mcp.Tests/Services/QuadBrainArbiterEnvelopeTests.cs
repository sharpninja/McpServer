using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class QuadBrainArbiterEnvelopeTests
{
    [Fact]
    public void TryParse_FinalIntent_ReadsContent()
    {
        var envelope = QuadBrainArbiterEnvelope.TryParse(
            """{"intent":"final","content":"Voice chat should call QuadBrain."}""");

        Assert.NotNull(envelope);
        Assert.True(envelope!.IsFinal);
        Assert.Equal("Voice chat should call QuadBrain.", envelope.Content);
        Assert.False(QuadBrainArbiterEnvelope.RequiresAnotherRound(envelope));
    }

    [Fact]
    public void TryParse_ContinueIntent_RequiresAnotherRound()
    {
        var envelope = QuadBrainArbiterEnvelope.TryParse(
            """{"intent":"continue","content":"Locating Common Voice Chat before reconciling."}""");

        Assert.NotNull(envelope);
        Assert.True(envelope!.IsContinue);
        Assert.True(QuadBrainArbiterEnvelope.RequiresAnotherRound(envelope));
    }

    [Fact]
    public void TryParse_ToolCallsIntent_ReadsTools()
    {
        var envelope = QuadBrainArbiterEnvelope.TryParse(
            """{"intent":"tool_calls","tool_calls":[{"name":"read_file","arguments":{"path":"a.yaml"}}]}""");

        Assert.NotNull(envelope);
        Assert.Equal(QuadBrainArbiterEnvelope.IntentToolCalls, envelope!.Intent);
        Assert.Equal("read_file", Assert.Single(envelope.ToolCalls).Function.Name);
        Assert.False(envelope.IsContinue);
    }

    [Fact]
    public void TryParse_LegacyToolCallsObject_MapsToToolCallsIntent()
    {
        var envelope = QuadBrainArbiterEnvelope.TryParse(
            """{"tool_calls":[{"name":"read_file","arguments":{"path":"a.yaml"}}]}""");

        Assert.NotNull(envelope);
        Assert.Equal(QuadBrainArbiterEnvelope.IntentToolCalls, envelope!.Intent);
        Assert.Equal("read_file", Assert.Single(envelope.ToolCalls).Function.Name);
    }

    [Fact]
    public void TryParse_LiveProseWithoutEnvelope_ReturnsNull()
        => Assert.Null(QuadBrainArbiterEnvelope.TryParse(
            "Neither role inspected the repo, so I am loading the required MCP skills and locating Common Voice Chat and QuadBrain before reconciling a decision."));

    [Fact]
    public void RequiresAnotherRound_NullEnvelope_IsTrue()
        => Assert.True(QuadBrainArbiterEnvelope.RequiresAnotherRound(null));

    [Fact]
    public void FormatForDisplay_HidesEnvelope_ShowsContent()
    {
        var json = """{"intent":"final","content":"Only this should print."}""";
        Assert.Equal("Only this should print.", QuadBrainArbiterEnvelope.FormatForDisplay(json, showIntent: false));
        Assert.Equal(json, QuadBrainArbiterEnvelope.FormatForDisplay(json, showIntent: true));
    }

    [Fact]
    public void FormatForDisplay_UnparsedProse_Unchanged()
    {
        const string prose = "plain arbiter sentence";
        Assert.Equal(prose, QuadBrainArbiterEnvelope.FormatForDisplay(prose, showIntent: false));
    }
}
