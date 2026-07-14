using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;

namespace McpServer.Support.Mcp.Tests.Options;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies options binding contracts for warning-suppression remediation.
/// </summary>
public sealed class PairingOptionsBindingTests
{
    /// <summary>
    /// TEST-MCP-AIUNIT-002: Proves configuration binding populates the pairing-user collection.
    /// </summary>
    [Fact]
    public void ConfigurationBinder_PopulatesPairingUsersCollection()
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Mcp:ApiKey"] = "test-key",
            ["Mcp:PairingUsers:0:Username"] = "owner",
            ["Mcp:PairingUsers:0:PasswordHash"] = "abc123",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var options = new PairingOptions();

        configuration.GetSection(PairingOptions.SectionName).Bind(options);

        Assert.Equal("test-key", options.ApiKey);
        var user = Assert.Single(options.PairingUsers);
        Assert.Equal("owner", user.Username);
        Assert.Equal("abc123", user.PasswordHash);
    }
}
