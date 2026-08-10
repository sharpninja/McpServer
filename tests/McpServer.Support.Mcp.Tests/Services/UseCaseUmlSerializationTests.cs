using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Models;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-012 / FR-MCP-USECASE-013 / FR-MCP-USECASE-014 / TR-MCP-USECASE-014:
/// Pure unit goldens for UML use-case graph serialization (100% AC-013, AC-014, AC-T14).
/// </summary>
public sealed class UseCaseUmlSerializationTests
{
    private readonly IUseCaseUmlSerializationService _sut = CreateSut();

    /// <summary>AC-T14-1: Serializer is a pure service type (constructed without DbContext).</summary>
    [Fact]
    public void Serializer_IsPureService_ConstructibleWithoutDbContext()
    {
        Assert.NotNull(_sut);
        Assert.IsNotType<Microsoft.EntityFrameworkCore.DbContext>(_sut);
    }

    /// <summary>AC-013-1: Mermaid export includes schema header.</summary>
    [Fact]
    public void ToMermaid_IncludesSchemaHeader()
    {
        var mermaid = _sut.ToMermaid(SampleGraph());
        Assert.Contains("%% mcp-usecase-diagram-schema:1", mermaid, StringComparison.Ordinal);
    }

    /// <summary>AC-013-2: Golden fixture contains actors, use cases, boundary, edges.</summary>
    [Fact]
    public void ToMermaid_GoldenFixture_ContainsActorsUseCasesBoundaryAndEdges()
    {
        var mermaid = _sut.ToMermaid(SampleGraph());
        Assert.Contains("Customer", mermaid, StringComparison.Ordinal);
        Assert.Contains("Place Order", mermaid, StringComparison.Ordinal);
        Assert.Contains("System", mermaid, StringComparison.Ordinal);
        Assert.Contains("flowchart", mermaid, StringComparison.OrdinalIgnoreCase);
        // include edge present as labeled dashed style or include keyword
        Assert.True(
            mermaid.Contains("include", StringComparison.OrdinalIgnoreCase)
            || mermaid.Contains("-.->", StringComparison.Ordinal)
            || mermaid.Contains("Include", StringComparison.Ordinal),
            "Expected include relationship encoding in Mermaid export.");
    }

    /// <summary>AC-013-3: Same graph yields same Mermaid (deterministic).</summary>
    [Fact]
    public void ToMermaid_IsDeterministic()
    {
        var a = _sut.ToMermaid(SampleGraph());
        var b = _sut.ToMermaid(SampleGraph());
        Assert.Equal(a, b);
    }

    /// <summary>AC-013-4: Empty graph yields minimal valid Mermaid with schema header.</summary>
    [Fact]
    public void ToMermaid_EmptyGraph_YieldsMinimalDocumentedForm()
    {
        var mermaid = _sut.ToMermaid(new UseCaseDiagramGraphDto { SchemaVersion = 1, Kind = "uml-usecase" });
        Assert.Contains("%% mcp-usecase-diagram-schema:1", mermaid, StringComparison.Ordinal);
        Assert.Contains("flowchart", mermaid, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-014-1: PlantUML contains start/end markers.</summary>
    [Fact]
    public void ToPlantUml_ContainsStartAndEnd()
    {
        var puml = _sut.ToPlantUml(SampleGraph());
        Assert.Contains("@startuml", puml, StringComparison.Ordinal);
        Assert.Contains("@enduml", puml, StringComparison.Ordinal);
    }

    /// <summary>AC-014-2: Golden fixture actors, use cases, include/extend.</summary>
    [Fact]
    public void ToPlantUml_GoldenFixture_ContainsActorsUseCasesAndRelationships()
    {
        var puml = _sut.ToPlantUml(SampleGraph());
        Assert.Contains("Customer", puml, StringComparison.Ordinal);
        Assert.Contains("Place Order", puml, StringComparison.Ordinal);
        Assert.True(
            puml.Contains("include", StringComparison.OrdinalIgnoreCase)
            || puml.Contains(".>", StringComparison.Ordinal),
            "Expected include relationship in PlantUML.");
        Assert.Contains("Pay Invoice", puml, StringComparison.Ordinal);
        Assert.True(
            puml.Contains("extend", StringComparison.OrdinalIgnoreCase)
            || puml.Contains("..>", StringComparison.Ordinal)
            || puml.Contains("<|", StringComparison.Ordinal),
            "Expected extend relationship encoding in PlantUML.");
    }

    /// <summary>AC-014-3: PlantUML export is deterministic.</summary>
    [Fact]
    public void ToPlantUml_IsDeterministic()
    {
        var a = _sut.ToPlantUml(SampleGraph());
        var b = _sut.ToPlantUml(SampleGraph());
        Assert.Equal(a, b);
    }

    /// <summary>AC-T14-2: Both formats produced for same graph (non-empty).</summary>
    [Fact]
    public void ToMermaid_And_ToPlantUml_BothNonEmptyForSampleGraph()
    {
        var g = SampleGraph();
        Assert.False(string.IsNullOrWhiteSpace(_sut.ToMermaid(g)));
        Assert.False(string.IsNullOrWhiteSpace(_sut.ToPlantUml(g)));
    }

    private static UseCaseDiagramGraphDto SampleGraph()
        => new()
        {
            SchemaVersion = 1,
            Kind = "uml-usecase",
            SystemBoundary = new UseCaseDiagramBoundaryDto
            {
                Id = "sb1",
                Label = "System",
                X = 200,
                Y = 80,
                Width = 420,
                Height = 320,
            },
            Nodes =
            [
                new UseCaseDiagramNodeDto { Id = "a1", Type = "actor", Label = "Customer", X = 40, Y = 160 },
                new UseCaseDiagramNodeDto { Id = "uc1", Type = "usecase", Label = "Place Order", X = 320, Y = 180 },
                new UseCaseDiagramNodeDto { Id = "uc2", Type = "usecase", Label = "Pay Invoice", X = 320, Y = 260 },
            ],
            Edges =
            [
                new UseCaseDiagramEdgeDto { Id = "e1", Type = "association", Source = "a1", Target = "uc1" },
                new UseCaseDiagramEdgeDto { Id = "e2", Type = "include", Source = "uc1", Target = "uc2" },
                new UseCaseDiagramEdgeDto { Id = "e3", Type = "extend", Source = "uc2", Target = "uc1" },
            ],
        };

    private static IUseCaseUmlSerializationService CreateSut()
    {
        // BDPv4: production type required for green; missing type keeps suite red/compile-fail.
        return new UseCaseUmlSerializationService();
    }
}
