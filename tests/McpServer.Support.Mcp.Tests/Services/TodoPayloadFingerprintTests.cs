using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-005: one shared normalized TODO payload fingerprint.</summary>
public sealed class TodoPayloadFingerprintTests
{
    /// <summary>P1-3: equivalent payloads produce the same fingerprint.</summary>
    [Fact]
    public void AreEquivalent_ExactNormalizedPayload_IsTrue()
    {
        var request = Sample("MCP-FINGERPRINT-001", "Title");
        var item = ToItem(request);
        Assert.True(TodoPayloadFingerprint.AreEquivalent(request, item));
        Assert.Equal(TodoPayloadFingerprint.Compute(request), TodoPayloadFingerprint.Compute(item));
    }

    /// <summary>P1-3: any semantic field mismatch is not equivalent.</summary>
    [Theory]
    [InlineData("title")]
    [InlineData("section")]
    [InlineData("priority")]
    [InlineData("estimate")]
    [InlineData("description")]
    [InlineData("technical")]
    [InlineData("task")]
    [InlineData("depends")]
    [InlineData("fr")]
    [InlineData("tr")]
    public void AreEquivalent_AnySemanticMismatch_IsFalse(string field)
    {
        var request = Sample("MCP-FINGERPRINT-002", "Title");
        var mutated = field switch
        {
            "title" => request with { Title = "Other" },
            "section" => request with { Section = "Other" },
            "priority" => request with { Priority = "low" },
            "estimate" => request with { Estimate = "8h" },
            "description" => request with { Description = ["changed"] },
            "technical" => request with { TechnicalDetails = ["changed"] },
            "task" => request with { ImplementationTasks = [new TodoFlatTask("changed", false)] },
            "depends" => request with { DependsOn = ["MCP-OTHER-001"] },
            "fr" => request with { FunctionalRequirements = ["FR-OTHER-001"] },
            _ => request with { TechnicalRequirements = ["TR-OTHER-001"] },
        };

        Assert.False(TodoPayloadFingerprint.AreEquivalent(mutated, ToItem(request)));
    }

    private static TodoCreateRequest Sample(string id, string title)
        => new()
        {
            Id = id,
            Title = title,
            Section = "MCP Server",
            Priority = "high",
            Estimate = "2h",
            Description = ["Do the work"],
            TechnicalDetails = ["Use the service"],
            ImplementationTasks = [new TodoFlatTask("Write tests", false)],
            DependsOn = [],
            FunctionalRequirements = ["FR-HANDOFF-001"],
            TechnicalRequirements = ["TR-HANDOFF-CONTRACT-001"],
            IdempotencyKey = "handoff-todo:run",
        };

    private static TodoFlatItem ToItem(TodoCreateRequest request)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Section = request.Section,
            Priority = request.Priority,
            Estimate = request.Estimate,
            Description = request.Description,
            TechnicalDetails = request.TechnicalDetails,
            ImplementationTasks = request.ImplementationTasks,
            DependsOn = request.DependsOn,
            FunctionalRequirements = request.FunctionalRequirements,
            TechnicalRequirements = request.TechnicalRequirements,
            Done = false,
            IdempotencyKey = request.IdempotencyKey,
        };
}
