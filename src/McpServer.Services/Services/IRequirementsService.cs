namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Service that invokes Copilot CLI to analyze a TODO item and produce
/// associated Functional and Technical Requirement IDs, then updates the
/// project docs and the TODO item itself.
/// </summary>
public interface IRequirementsService
{
    /// <summary>
    /// Analyze a TODO item, generate FR/TR entries via Copilot CLI,
    /// update docs/Project/*.md, and return the assigned FR/TR IDs.
    /// </summary>
    /// <param name="todoId">The TODO item id to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing assigned FR and TR IDs, or an error.</returns>
    Task<RequirementsAnalysisResult> AnalyzeAsync(string todoId, CancellationToken cancellationToken = default);
}

/// <summary>Result of a requirements analysis for a TODO item.</summary>
public sealed record RequirementsAnalysisResult(
    bool Success,
    IReadOnlyList<string>? FunctionalRequirements = null,
    IReadOnlyList<string>? TechnicalRequirements = null,
    string? Error = null,
    string? CopilotResponse = null);
