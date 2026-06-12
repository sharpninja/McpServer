namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>
/// TEST-MCP-165, TEST-MCP-167, TEST-MCP-168, and TEST-MCP-169:
/// Verifies the restored turn transaction plan artifact preserves imported
/// diagram identifiers and points to the implemented transaction surfaces.
/// </summary>
public sealed class TurnTransactionPlanArtifactTests
{
    private static readonly string[] DiagramIds =
    [
        "AD-TXN-001",
        "AD-CURIOSITY-001",
        "SD-DIFFGRAM-001",
        "AD-AOT-001",
        "AD-WEIGHT-001",
        "ARCH-QUAD-001",
    ];

    /// <summary>
    /// TEST-MCP-165: Validates that the imported plan keeps all six stable
    /// Mermaid diagram IDs, one Mermaid block per ID, source-section text, and
    /// repo annotations.
    /// </summary>
    [Fact]
    public void ImportedPlan_PreservesAllSixMermaidDiagramIds()
    {
        var text = ReadTransactionPlan();

        Assert.Equal(DiagramIds.Length, CountOccurrences(text, "```mermaid"));
        foreach (var diagramId in DiagramIds)
        {
            var section = ExtractDiagramSection(text, diagramId);

            Assert.Contains("### " + diagramId + " ", section, StringComparison.Ordinal);
            Assert.Equal(1, CountOccurrences(section, "```mermaid"));
            Assert.Contains("Imported section:", section, StringComparison.Ordinal);
            Assert.Contains("Repo annotations:", section, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// TEST-MCP-167, TEST-MCP-168, and TEST-MCP-169: Validates that the plan
    /// references the implemented transaction coordinator, keyserver,
    /// subscriber, client, and focused test surfaces used by the diagram-derived
    /// transaction slice.
    /// </summary>
    [Fact]
    public void ImportedPlan_ReferencesImplementedTransactionComponentsAndTests()
    {
        var repoRoot = FindRepoRoot();
        var text = ReadTransactionPlan();

        Assert.Contains("`SD-DIFFGRAM-001-MSG-SIGN`: keyserver signs canonical transaction manifests.", text, StringComparison.Ordinal);
        Assert.Contains("`SD-DIFFGRAM-001-MSG-VERIFY-HASH`: subscriber verifies encrypted and plaintext hashes.", text, StringComparison.Ordinal);
        Assert.Contains("`SD-DIFFGRAM-001-BR-INVALID`: invalid hash/signature/decrypt path aborts and audits.", text, StringComparison.Ordinal);
        Assert.Contains("`SD-DIFFGRAM-001-BR-VALID`: valid manifest and payload commits durably.", text, StringComparison.Ordinal);
        Assert.Contains("`mcpserver`: existing MCP Server host plus `Mcp:TurnTransactions`, in-process keyserver/subscriber controllers, and transaction coordinator under `src/McpServer.Support.Mcp`.", text, StringComparison.Ordinal);
        Assert.Contains("Shared client contracts: existing `src/McpServer.Client`.", text, StringComparison.Ordinal);
        Assert.Contains("Focused first-slice tests: existing MCP support/client test projects.", text, StringComparison.Ordinal);
        Assert.Contains("Deferred projects: `src/McpServer.KeyServer`, `src/McpServer.Subscriber`, `tests/McpServer.KeyServer.Tests`, `tests/McpServer.Subscriber.Tests`, and `tests/McpServer.PlanReview.Tests`.", text, StringComparison.Ordinal);

        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Support.Mcp", "Services", "TurnTransactionCoordinator.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Support.Mcp", "Services", "TransactionSecurityServices.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Support.Mcp", "Controllers", "KeyServerController.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Support.Mcp", "Controllers", "SubscriberController.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Client", "KeyServerClient.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Client", "SubscriberClient.cs");
        AssertRepositoryFileExists(repoRoot, "tests", "McpServer.Support.Mcp.Tests", "Services", "TurnTransactionCoordinatorTests.cs");
        AssertRepositoryFileExists(repoRoot, "tests", "McpServer.Support.Mcp.Tests", "Controllers", "TransactionSecurityControllerTests.cs");
        AssertRepositoryFileExists(repoRoot, "tests", "McpServer.Client.Tests", "TransactionSecurityClientTests.cs");
    }

    private static string ReadTransactionPlan()
        => File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "Project", "Quad-Model-Transactional-Diffgram-Plan.md"));

    private static string ExtractDiagramSection(string text, string diagramId)
    {
        var heading = "### " + diagramId + " ";
        var start = text.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, "Missing diagram heading " + diagramId + ".");

        var next = text.IndexOf("\n### ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? text[start..] : text[start..next];
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var start = 0;
        while (true)
        {
            var index = text.IndexOf(value, start, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            start = index + value.Length;
        }
    }

    private static void AssertRepositoryFileExists(string repoRoot, params string[] pathParts)
    {
        var path = Path.Combine([repoRoot, .. pathParts]);
        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs", "Project")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
