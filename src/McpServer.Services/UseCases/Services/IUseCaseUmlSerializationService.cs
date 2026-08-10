using McpServer.Support.Mcp.UseCases.Models;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// FR-MCP-USECASE-013 / FR-MCP-USECASE-014 / TR-MCP-USECASE-014:
/// Pure graph → Mermaid / PlantUML serialization (no DbContext).
/// </summary>
public interface IUseCaseUmlSerializationService
{
    /// <summary>AC-013-*: Export graph to Mermaid (schema v1).</summary>
    /// <param name="graph">Diagram graph.</param>
    /// <returns>Mermaid source text.</returns>
    string ToMermaid(UseCaseDiagramGraphDto graph);

    /// <summary>AC-014-*: Export graph to PlantUML use-case syntax.</summary>
    /// <param name="graph">Diagram graph.</param>
    /// <returns>PlantUML source text.</returns>
    string ToPlantUml(UseCaseDiagramGraphDto graph);
}
