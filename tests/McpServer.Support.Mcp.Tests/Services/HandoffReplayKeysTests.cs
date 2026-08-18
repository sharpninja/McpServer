using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-005 / TR-HANDOFF-MODES-001: replay identity is a fixed-length SHA-256
/// over a length-prefixed canonical payload, not a raw concatenated unique-index value.
/// </summary>
public sealed class HandoffReplayKeysTests
{
    /// <summary>Identity is always 64 hex characters even for a long workspace path.</summary>
    [Fact]
    public void Create_LongWorkspace_ReturnsFixedLengthSha256()
    {
        var workspace = @"F:\" + new string('w', 800);
        var identity = HandoffReplayKeys.Create(workspace, new string('a', 64), "handoff-todo-draft/v1", force: false, runId: "handoff-run-1");
        Assert.Equal(64, identity.Length);
        Assert.Matches("^[0-9A-F]{64}$", identity);
    }

    /// <summary>Unicode workspace paths are encoded unambiguously and stay 64 hex characters.</summary>
    [Fact]
    public void Create_UnicodeWorkspace_IsStableAndFixedLength()
    {
        var first = HandoffReplayKeys.Create(@"F:\工作区\handoff", "abc", "handoff-todo-draft/v1", force: false, "r1");
        var second = HandoffReplayKeys.Create(@"F:\工作区\handoff", "abc", "handoff-todo-draft/v1", force: false, "r1");
        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    /// <summary>Length-prefixing prevents delimiter collisions between workspace and hash fragments.</summary>
    [Fact]
    public void Create_DelimiterCollisionInputs_ProduceDistinctIdentities()
    {
        var left = HandoffReplayKeys.Create("ws\u001fsha", "rest", "v1", force: false, "r1");
        var right = HandoffReplayKeys.Create("ws", "sha\u001frest", "v1", force: false, "r1");
        Assert.NotEqual(left, right);
    }

    /// <summary>Force includes the run id so a new durable row is allowed; non-force ignores run id.</summary>
    [Fact]
    public void Create_ForceSemantics_PreserveDeterministicReplayAndUniqueForceRows()
    {
        var replayA = HandoffReplayKeys.Create("ws", "hash", "v1", force: false, "run-a");
        var replayB = HandoffReplayKeys.Create("ws", "hash", "v1", force: false, "run-b");
        var forceA = HandoffReplayKeys.Create("ws", "hash", "v1", force: true, "run-a");
        var forceB = HandoffReplayKeys.Create("ws", "hash", "v1", force: true, "run-b");
        Assert.Equal(replayA, replayB);
        Assert.NotEqual(forceA, forceB);
        Assert.NotEqual(replayA, forceA);
    }
}
