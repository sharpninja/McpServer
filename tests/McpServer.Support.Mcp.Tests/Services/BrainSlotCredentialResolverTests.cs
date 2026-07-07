using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for safe brain-slot credential reference resolution. TEST-MCP-177.</summary>
public sealed class BrainSlotCredentialResolverTests
{
    /// <summary>Environment references resolve through the named variable and trim whitespace.</summary>
    [Fact]
    public async Task ResolveAsync_WithEnvironmentReference_ReturnsTrimmedSecret()
    {
        var variableName = "MCP_BRAIN_SLOT_TEST_KEY_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variableName, "  test-secret  ");
        try
        {
            var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());

            var value = await resolver.ResolveAsync("env:" + variableName, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("test-secret", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

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
