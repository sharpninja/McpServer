using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests for safe brain-slot credential reference resolution. TEST-MCP-177. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because one of its tests writes the process environment
/// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-177; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (ResolveAsync_WithEnvironmentReference_ReturnsTrimmedSecret) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class BrainSlotCredentialResolverTests
{
    /// <summary>Config references resolve from IConfiguration without exposing raw secrets in storage rows.</summary>
    [Fact]
    public async Task ResolveAsync_WithConfigReference_ReturnsConfiguredSecret()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BrainSlots:LeftKey"] = "configured-secret",
            })
            .Build();
        var resolver = new BrainSlotCredentialResolver(configuration);

        var value = await resolver.ResolveAsync("config:BrainSlots:LeftKey", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("configured-secret", value);
    }

    /// <summary>Only env, config, and file credential-reference schemes are supported.</summary>
    [Theory]
    [InlineData("env:OPENAI_API_KEY", true)]
    [InlineData("config:BrainSlots:Key", true)]
    [InlineData("file:C:\\secrets\\brain-slot.key", true)]
    [InlineData("sk-raw-secret", false)]
    [InlineData("vault:future", false)]
    [InlineData("", false)]
    public void IsSupportedReference_RejectsRawOrUnknownReferences(string reference, bool expected)
    {
        var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());

        Assert.Equal(expected, resolver.IsSupportedReference(reference));
    }
}
