// Phase 0 "parity gap" test for Cline v2 (example; repeat pattern for each plugin)
// Must fail until the plugin adoption PR closes the gap per v4 process.
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

public class ClineV2_GapTests
{
    [Fact]
    public void ClineV2_MustCallCompleteTurnBeforeStop_ForV4Compliance()
    {
        Assert.True(false, "Phase 0 gap stub - failing until adoption");
    }
}