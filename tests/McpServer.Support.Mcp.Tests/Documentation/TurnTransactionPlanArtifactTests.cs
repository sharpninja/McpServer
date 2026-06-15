namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>
/// TEST-MCP-162 through TEST-MCP-185:
/// Verifies the restored turn transaction plan artifact preserves imported
/// diagram identifiers, traceability, deferred scope, and implemented
/// transaction surfaces.
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
        Assert.Contains("`mcpserver`: existing MCP Server host plus `Mcp:TurnTransactions`, compatibility keyserver/subscriber controllers under `src/McpServer.Support.Mcp`, and the shared transaction coordinator from `src/McpServer.TransactionSecurity`.", text, StringComparison.Ordinal);
        Assert.Contains("Separate keyserver host: `src/McpServer.KeyServer`.", text, StringComparison.Ordinal);
        Assert.Contains("Separate subscriber host: `src/McpServer.Subscriber`.", text, StringComparison.Ordinal);
        Assert.Contains("Shared transaction-security core: `src/McpServer.TransactionSecurity`.", text, StringComparison.Ordinal);
        Assert.Contains("Shared client contracts: existing `src/McpServer.Client`.", text, StringComparison.Ordinal);
        Assert.Contains("Focused first-slice tests: MCP support/client test projects plus `tests/McpServer.TransactionSecurity.IntegrationTests`.", text, StringComparison.Ordinal);

        AssertRepositoryFileExists(repoRoot, "src", "McpServer.TransactionSecurity", "Services", "TurnTransactionCoordinator.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.TransactionSecurity", "Services", "TransactionSecurityServices.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.KeyServer", "Program.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Subscriber", "Program.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Support.Mcp", "Controllers", "KeyServerController.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Support.Mcp", "Controllers", "SubscriberController.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Client", "KeyServerClient.cs");
        AssertRepositoryFileExists(repoRoot, "src", "McpServer.Client", "SubscriberClient.cs");
        AssertRepositoryFileExists(repoRoot, "tests", "McpServer.Support.Mcp.Tests", "Services", "TurnTransactionCoordinatorTests.cs");
        AssertRepositoryFileExists(repoRoot, "tests", "McpServer.Support.Mcp.Tests", "Controllers", "TransactionSecurityControllerTests.cs");
        AssertRepositoryFileExists(repoRoot, "tests", "McpServer.Client.Tests", "TransactionSecurityClientTests.cs");
        AssertRepositoryFileExists(repoRoot, "tests", "McpServer.TransactionSecurity.IntegrationTests", "SeparateTransactionServiceIntegrationTests.cs");
    }

    /// <summary>
    /// TEST-MCP-162, TEST-MCP-173, and TEST-MCP-174: Validates transaction-plan requirements
    /// are concrete traceability artifacts rather than placeholder backfills.
    /// </summary>
    [Fact]
    public void TransactionPlanRequirements_AreConcreteAndMapped()
    {
        var functional = ReadProjectFile("Functional-Requirements.md");
        var technical = ReadProjectFile("Technical-Requirements.md");
        var testing = ReadProjectFile("Testing-Requirements.md");
        var matrix = ReadProjectFile("Requirements-Matrix.md");
        var mapping = ReadProjectFile("TR-per-FR-Mapping.md");

        foreach (var requirementId in Enumerable.Range(118, 14)
            .Select(id => $"FR-MCP-{id}")
            .Concat(["FR-MCP-134", "FR-MCP-135"]))
        {
            var section = ExtractRequirementSection(functional, "## " + requirementId + " ");
            Assert.DoesNotContain("Placeholder requirement backfilled", section, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("**Acceptance Criteria:**", section, StringComparison.Ordinal);
            Assert.Contains("| " + requirementId + " |", matrix, StringComparison.Ordinal);
            Assert.Contains("| " + requirementId + " |", mapping, StringComparison.Ordinal);
        }

        foreach (var requirementId in new[]
        {
            "TR-MCP-KEYSERVER-001",
            "TR-MCP-CRYPTO-001",
            "TR-MCP-SUBSCRIBER-001",
            "TR-MCP-TXN-001",
            "TR-MCP-TXNAUDIT-001",
            "TR-MCP-TXNCOMPAT-001",
            "TR-MCP-TXNBYRD-001",
            "TR-MCP-TXNAIUNIT-001",
            "TR-MCP-TXNDIAGRAMS-001",
            "TR-MCP-TXNARCH-001",
            "TR-MCP-TXNDESIGN-001",
            "TR-MCP-QUAD-001",
            "TR-MCP-QUAD-002",
            "TR-MCP-QUAD-003",
            "TR-MCP-QUAD-004",
            "TR-MCP-QUAD-005",
            "TR-MCP-QUAD-006",
            "TR-MCP-QUAD-007",
        })
        {
            var section = ExtractRequirementSection(technical, "## " + requirementId);
            Assert.DoesNotContain("Placeholder requirement backfilled", section, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("**Status:**", section, StringComparison.Ordinal);
            Assert.Contains("**Covered by:**", section, StringComparison.Ordinal);
            Assert.Contains("| " + requirementId + " |", matrix, StringComparison.Ordinal);
        }

        foreach (var testId in Enumerable.Range(158, 28).Select(id => $"TEST-MCP-{id}"))
        {
            Assert.Contains("- " + testId + ":", testing, StringComparison.Ordinal);
            Assert.Contains("| " + testId + " |", matrix, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// TEST-MCP-163, TEST-MCP-170, TEST-MCP-171, TEST-MCP-172, TEST-MCP-174, and TEST-MCP-185:
    /// validates the implemented/deferred split and the two architecture/design rounds remain explicit local artifacts.
    /// </summary>
    [Fact]
    public void PlanArtifacts_PreserveDeferredScopeAndDesignRounds()
    {
        var plan = ReadTransactionPlan();
        var round1 = ReadProjectFile("TurnTransactions-Architecture-Round1.md");
        var round2 = ReadProjectFile("TurnTransactions-Design-Round2.md");
        var audit = ReadProjectFile("TurnTransactions-Mutation-Endpoint-Audit.md");

        Assert.Contains("Execute the authorized full Quad-Brain orchestration, AoT reconciliation, and safety-gated weight update slices", plan, StringComparison.Ordinal);
        Assert.Contains("AD-CURIOSITY-001-BR-FRUSTRATION", plan, StringComparison.Ordinal);
        Assert.Contains("`AD-CURIOSITY-001-BR-EXTERNAL`: implemented for individually configured, transaction-gated external brain-slot invocation", plan, StringComparison.Ordinal);
        Assert.Contains("`AD-CURIOSITY-001-BR-INJECT`: implemented only for CuriosityEngine committed-result GraphRAG/context admission", plan, StringComparison.Ordinal);
        Assert.Contains("`AD-AOT-001-BR-ACCEPT`: implemented through ArbiterOfTruth reconciliation", plan, StringComparison.Ordinal);
        Assert.Contains("`AD-WEIGHT-001-BR-GATES`: implemented through explicit safety-gated weight updates", plan, StringComparison.Ordinal);
        Assert.Contains("AD-AOT-001-BR-DISAGREE", plan, StringComparison.Ordinal);
        Assert.Contains("Deferred branches:", plan, StringComparison.Ordinal);

        Assert.Contains("keyserver", round1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subscriber", round1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("transaction coordination state", round1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rollback never deletes audit rows", round1, StringComparison.Ordinal);
        Assert.DoesNotContain("AoT reconciliation execution, and weight updates are documented but disabled by default", round1, StringComparison.Ordinal);
        Assert.DoesNotContain("keep quad execution disabled until future requirements authorize it", round1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FR-MCP-134 and FR-MCP-135 authorize full orchestration, AoT reconciliation, and safety-gated weight updates", round1, StringComparison.Ordinal);

        Assert.Contains("## Test Mapping", round2, StringComparison.Ordinal);
        Assert.Contains("## Round 2 Gap Analysis", round2, StringComparison.Ordinal);
        Assert.Contains("TEST-MCP-161", round2, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled future Quad-Model branches", round2, StringComparison.Ordinal);

        Assert.Contains("Federation control-plane mutations fail closed", audit, StringComparison.Ordinal);
        Assert.Contains("Explicitly Deferred", audit, StringComparison.Ordinal);
    }

    private static string ReadTransactionPlan()
        => File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "Project", "Quad-Model-Transactional-Diffgram-Plan.md"));

    private static string ReadProjectFile(string fileName)
        => File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "Project", fileName));

    private static string ExtractRequirementSection(string text, string heading)
    {
        var start = text.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, "Missing requirement heading " + heading + ".");

        var next = text.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? text[start..] : text[start..next];
    }

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
