using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// TR-MCP-USECASE-004: Pure diagram builder for use case aggregates.
/// Implementations must not open DbContext or perform I/O.
/// </summary>
public interface IUseCaseDiagramService
{
    /// <summary>
    /// TR-MCP-USECASE-004: Build Mermaid sequenceDiagram text from a loaded aggregate.
    /// </summary>
    /// <param name="useCase">Fully loaded use case detail DTO.</param>
    /// <returns>Mermaid source text.</returns>
    string GenerateMermaid(UseCaseDetailDto useCase);

    /// <summary>
    /// TR-MCP-USECASE-004: Build diagram text for a named format (mermaid, plantuml).
    /// </summary>
    /// <param name="useCase">Fully loaded use case detail DTO.</param>
    /// <param name="format">Format name (case-insensitive).</param>
    /// <returns>Diagram source text.</returns>
    /// <exception cref="ArgumentException">When the format is unsupported.</exception>
    string Generate(UseCaseDetailDto useCase, string format);
}
