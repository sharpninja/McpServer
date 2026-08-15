namespace McpServer.Support.Mcp.UseCases.Models;

/// <summary>
/// FR-MCP-USECASE-012 / TR-MCP-USECASE-011: Persisted UML use-case diagram graph (schema v1).
/// Source of truth for canvas layout and Mermaid/PlantUML export (not sequence-from-steps).
/// </summary>
public sealed class UseCaseDiagramGraphDto
{
    /// <summary>Schema version; must be 1 for mcp-usecase-diagram-schema:1.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Graph kind discriminator.</summary>
    public string Kind { get; set; } = "uml-usecase";

    /// <summary>Optional system boundary rectangle.</summary>
    public UseCaseDiagramBoundaryDto? SystemBoundary { get; set; }

    /// <summary>Actor and use-case nodes.</summary>
    public List<UseCaseDiagramNodeDto> Nodes { get; set; } = [];

    /// <summary>Relationships between nodes.</summary>
    public List<UseCaseDiagramEdgeDto> Edges { get; set; } = [];
}

/// <summary>FR-MCP-USECASE-012: System boundary box.</summary>
public sealed class UseCaseDiagramBoundaryDto
{
    /// <summary>Boundary id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label.</summary>
    public string Label { get; set; } = "System";

    /// <summary>Canvas X.</summary>
    public double X { get; set; }

    /// <summary>Canvas Y.</summary>
    public double Y { get; set; }

    /// <summary>Width.</summary>
    public double Width { get; set; } = 400;

    /// <summary>Height.</summary>
    public double Height { get; set; } = 300;
}

/// <summary>FR-MCP-USECASE-012: Actor or use-case node.</summary>
public sealed class UseCaseDiagramNodeDto
{
    /// <summary>Stable node id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Node type: actor or usecase.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Canvas X.</summary>
    public double X { get; set; }

    /// <summary>Canvas Y.</summary>
    public double Y { get; set; }
}

/// <summary>FR-MCP-USECASE-012: Edge between nodes.</summary>
public sealed class UseCaseDiagramEdgeDto
{
    /// <summary>Stable edge id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Edge type: association, include, extend, generalization.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Source node id.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Target node id.</summary>
    public string Target { get; set; } = string.Empty;
}
