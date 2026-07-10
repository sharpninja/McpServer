using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>Regression tests for generated requirements Markdown rendering.</summary>
public sealed class RequirementsDocumentRendererTests
{
    /// <summary>Verifies technical requirement exports preserve the requirement status field as metadata.</summary>
    [Fact]
    public void RenderTechnical_EmitsDedicatedStatusMetadata()
    {
        var entry = new TrEntry(
            "TR-MCP-QUAD-001",
            "Brain-slot storage, DTOs, CRUD, and validation",
            "Persist brain-slot definitions and invocation audit rows.",
            Status: "completed");

        var markdown = RequirementsDocumentRenderer.RenderTechnical([entry]);

        Assert.Contains("## TR-MCP-QUAD-001", markdown, StringComparison.Ordinal);
        Assert.Contains("**Status:** completed", markdown, StringComparison.Ordinal);
        Assert.Contains("Scope: layer-1+", markdown, StringComparison.Ordinal);
    }

    /// <summary>Verifies technical requirement exports derive coverage from FR/TR/TEST mappings.</summary>
    [Fact]
    public void RenderTechnical_EmitsMappingDerivedCoverageMetadata()
    {
        var entry = new TrEntry(
            "TR-MCP-QUAD-001",
            "Brain-slot storage, DTOs, CRUD, and validation",
            "Persist brain-slot definitions and invocation audit rows.",
            Status: "completed");
        var mappings = new[]
        {
            new FrTrMapping(
                "FR-MCP-123",
                ["TR-MCP-QUAD-001"],
                ["TEST-MCP-163", "TEST-MCP-170"])
        };

        var markdown = RequirementsDocumentRenderer.RenderTechnical([entry], mappings);

        Assert.Contains("**Covered by:** FR: FR-MCP-123; TEST: TEST-MCP-163, TEST-MCP-170", markdown, StringComparison.Ordinal);
        Assert.Contains("**Status:** completed", markdown, StringComparison.Ordinal);
    }
}
