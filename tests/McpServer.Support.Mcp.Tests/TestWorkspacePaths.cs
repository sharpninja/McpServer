namespace McpServer.Support.Mcp.Tests;

/// <summary>Provides a checkout-independent placeholder for unused ingestion dependencies.</summary>
internal static class TestWorkspacePaths
{
    /// <summary>Absolute, non-checkout path for tests that construct but do not invoke repository ingestion.</summary>
    internal static string UnusedRepoRoot => Path.Combine(
        Path.GetTempPath(), "McpServer.Tests", "unused-repo-root");
}
