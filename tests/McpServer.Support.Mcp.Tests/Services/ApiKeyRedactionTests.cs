using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Regression coverage for log and diagnostic redaction of API keys and <c>X-Api-Key</c> values.
/// Sample strings use synthetic fixtures only; no live keys.
/// </summary>
public sealed class ApiKeyRedactionTests
{
    private const string SampleKey = "synth-fixture-key-AAAA1111BBBB2222";

    /// <summary>Header, assignment, JSON, and query-string key material is replaced by a fixed placeholder.</summary>
    [Fact]
    public void Redact_StripsApiKeyMaterial_FromLogAndDiagnosticSamples()
    {
        var samples = new[]
        {
            $"X-Api-Key={SampleKey}",
            $"X-Api-Key: {SampleKey}",
            $"Headers: Host=payton-legion2:7147; X-Api-Key={SampleKey}; X-Workspace-Path=F:\\GitHub\\McpServer",
            $"api_key={SampleKey}",
            $"apiKey: {SampleKey}",
            $"{{\"apiKey\":\"{SampleKey}\"}}",
            $"/mcpserver/todo?api_key={SampleKey}",
            $"Bearer {SampleKey}",
        };

        foreach (var sample in samples)
        {
            var redacted = ApiKeyRedaction.Redact(sample);
            Assert.DoesNotContain(SampleKey, redacted, StringComparison.Ordinal);
            Assert.Contains(ApiKeyRedaction.Placeholder, redacted, StringComparison.Ordinal);
        }
    }

    /// <summary>Sensitive header names redact to the placeholder without echoing the value.</summary>
    [Fact]
    public void RedactHeaderValue_ReplacesSensitiveHeaders()
    {
        Assert.Equal(ApiKeyRedaction.Placeholder, ApiKeyRedaction.RedactHeaderValue("X-Api-Key", SampleKey));
        Assert.Equal(ApiKeyRedaction.Placeholder, ApiKeyRedaction.RedactHeaderValue("Authorization", "Bearer " + SampleKey));
        Assert.DoesNotContain(SampleKey, ApiKeyRedaction.RedactHeaderValue("Accept", $"api_key={SampleKey}"), StringComparison.Ordinal);
    }

    /// <summary>Brain interaction logger delegates to the shared helper.</summary>
    [Fact]
    public void BrainInteractionLogger_UsesSharedRedaction()
    {
        var redacted = BrainInteractionSessionLogger.Redact($"X-Api-Key={SampleKey}");
        Assert.DoesNotContain(SampleKey, redacted, StringComparison.Ordinal);
        Assert.Equal("Bearer [REDACTED]", BrainInteractionSessionLogger.Redact("Bearer abc.def-123"));
    }
}
