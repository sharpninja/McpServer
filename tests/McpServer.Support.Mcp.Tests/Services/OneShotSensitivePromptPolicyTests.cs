using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-003 / TEST-HANDOFF-007: handoff raw source is not retained on published surfaces.</summary>
public sealed class OneShotSensitivePromptPolicyTests
{
    /// <summary>P1-5: HandoffTodoDraft publishes a redacted hash placeholder, never the raw source.</summary>
    [Fact]
    public void Publish_HandoffContext_RedactsRawSource()
    {
        const string raw = "SECRET-HANDOFF-SOURCE-TEXT";
        var published = OneShotSensitivePromptPolicy.Publish(AgentPoolOneShotContext.HandoffTodoDraft, raw);
        Assert.StartsWith(OneShotSensitivePromptPolicy.RedactedPrefix, published, StringComparison.Ordinal);
        Assert.DoesNotContain(raw, published, StringComparison.Ordinal);
        Assert.False(OneShotSensitivePromptPolicy.ContainsRawSource(published, raw));
    }

    /// <summary>P1-5: non-handoff contexts still publish the raw prompt.</summary>
    [Fact]
    public void Publish_AdHocContext_KeepsRawPrompt()
    {
        const string raw = "ordinary prompt";
        var published = OneShotSensitivePromptPolicy.Publish(AgentPoolOneShotContext.AdHoc, raw);
        Assert.Equal(raw, published);
    }
}
