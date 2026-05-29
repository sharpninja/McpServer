// Phase 0 "parity gap" test for ClaudeCode (per plan pattern)
// Must fail until the plugin adoption PR closes the gap per v4 process.
using Xunit;
namespace McpServer.AgentPluginParity.Tests.Plugins;
public class ClaudeCode_GapTests
{
    [Fact]
    public void ClaudeCode_MustCloseV4ParityGaps_ForV4Compliance()
    {
        Assert.True(false, "Phase 0 gap stub - failing until adoption");
    }
}
