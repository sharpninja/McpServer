using McpServer.Support.Mcp.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Options;

/// <summary>
/// TEST-MCP-TRIAGESTORE-007: validates the session-log Submit command budget before the host starts.
/// </summary>
public sealed class SessionLogSubmitOptionsValidatorTests
{
    /// <summary>The shipped 30 second default is valid.</summary>
    [Fact]
    public void Validate_ReturnsSuccess_ForDefaultOptions()
    {
        var result = new SessionLogSubmitOptionsValidator().Validate(null, new SessionLogSubmitOptions());

        Assert.True(result.Succeeded);
    }

    /// <summary>A raised deploy value inside the cap is valid.</summary>
    [Fact]
    public void Validate_ReturnsSuccess_ForOneHundredTwentySeconds()
    {
        var options = new SessionLogSubmitOptions { SubmitCommandBudgetSeconds = 120 };
        var result = new SessionLogSubmitOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
        Assert.Equal(TimeSpan.FromSeconds(120), options.GetSubmitCommandBudget());
    }
}
