using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-004: consumer tests for pure draft validation and field-specific diagnostics.
/// </summary>
public sealed class HandoffTodoDraftValidatorTests
{
    /// <summary>TEST-HANDOFF-004: invalid id, priority, and requirement links produce field-specific errors.</summary>
    [Fact]
    public void Validate_InvalidFields_ProduceFieldDiagnostics()
    {
        var sut = new HandoffTodoDraftValidator();

        var result = sut.Validate(new HandoffTodoDraft
        {
            Id = "bad-id",
            Title = " ",
            Section = "",
            Priority = "urgent",
            Estimate = " ",
            Confidence = 2,
            DependsOn = ["not-an-id"],
            FunctionalRequirements = ["TR-HANDOFF-CONTRACT-001"],
            TechnicalRequirements = ["FR-HANDOFF-001"],
            ImplementationTasks = [new HandoffTodoDraftTask { Task = " " }],
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Field == "id");
        Assert.Contains(result.Diagnostics, item => item.Field == "title");
        Assert.Contains(result.Diagnostics, item => item.Field == "section");
        Assert.Contains(result.Diagnostics, item => item.Field == "priority");
        Assert.Contains(result.Diagnostics, item => item.Field == "estimate");
        Assert.Contains(result.Diagnostics, item => item.Field == "confidence");
        Assert.Contains(result.Diagnostics, item => item.Field == "dependsOn");
        Assert.Contains(result.Diagnostics, item => item.Field == "functionalRequirements");
        Assert.Contains(result.Diagnostics, item => item.Field == "technicalRequirements");
        Assert.Contains(result.Diagnostics, item => item.Field == "implementationTasks");
    }

    /// <summary>TEST-HANDOFF-004: blank description and technicalDetails produce field-specific diagnostics.</summary>
    [Fact]
    public void Validate_BlankDescriptionAndTechnicalDetails_ProduceFieldDiagnostics()
    {
        var sut = new HandoffTodoDraftValidator();

        var result = sut.Validate(new HandoffTodoDraft
        {
            Id = "MCP-HANDOFFDEMO-011",
            Title = "Demo",
            Section = "MCP Server",
            Priority = "high",
            Confidence = 0.8,
            Description = [" ", ""],
            TechnicalDetails = ["   "],
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Field == "description" && item.Code == "draft_invalid_description");
        Assert.Contains(result.Diagnostics, item => item.Field == "technicalDetails" && item.Code == "draft_invalid_technicalDetails");
    }

    /// <summary>TEST-HANDOFF-004: a valid draft normalizes priority and keeps unknown source notes.</summary>
    [Fact]
    public void Validate_ValidDraft_NormalizesValues()
    {
        var sut = new HandoffTodoDraftValidator();

        var result = sut.Validate(new HandoffTodoDraft
        {
            Id = "MCP-HANDOFFDEMO-010",
            Title = " Demo ",
            Section = " MCP Server ",
            Priority = "HIGH",
            Confidence = 0.81,
            UnknownSourceNotes = [" missing owner "],
        });

        Assert.True(result.IsValid);
        Assert.Equal("high", result.Draft!.Priority);
        Assert.Equal("Demo", result.Draft.Title);
        Assert.Contains("missing owner", result.Draft.UnknownSourceNotes);
    }

    /// <summary>TEST-HANDOFF-004: live multi-segment requirement IDs are accepted.</summary>
    [Fact]
    public void Validate_MultiSegmentRequirementIds_AreAccepted()
    {
        var sut = new HandoffTodoDraftValidator();

        var result = sut.Validate(new HandoffTodoDraft
        {
            Id = "MCP-HANDOFFDEMO-012",
            Title = "Demo",
            Section = "MCP Server",
            Priority = "high",
            Confidence = 0.8,
            FunctionalRequirements = ["FR-MCP-USECASE-001"],
            TechnicalRequirements = ["TR-HANDOFF-CONTRACT-001"],
        });

        Assert.True(result.IsValid, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        Assert.Contains("FR-MCP-USECASE-001", result.Draft!.FunctionalRequirements);
    }
}
