using McpServer.Common.AgentCli;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class LineSanitizerTests
{
    [Fact]
    public void Sanitize_RewritesDefaultPowerShellPrompt_WhenModelLabelProvided()
    {
        var line = "PS E:\\github\\McpServer>\n";

        var sanitized = LineSanitizer.Sanitize(line, "gpt-5.4");

        Assert.Equal("gpt-5.4 E:\\github\\McpServer>\n", sanitized);
    }

    [Fact]
    public void Sanitize_LeavesDefaultPowerShellPromptUnchanged_WhenModelLabelMissing()
    {
        var line = "PS E:\\github\\McpServer>\n";

        var sanitized = LineSanitizer.Sanitize(line, null);

        Assert.Equal(line, sanitized);
    }
}
