namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Options controlling the prompt template embedded in <c>AGENTS-README-FIRST.yaml</c> marker files.
/// Bound from the <c>Mcp</c> configuration section.
/// </summary>
public sealed class MarkerPromptOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp";

    /// <summary>
    /// Global markdown prompt template written into every marker file.
    /// Supports <c>{baseUrl}</c> placeholder for runtime substitution.
    /// When <see langword="null"/> or empty, the built-in default prompt is used.
    /// </summary>
    public string? MarkerPromptTemplate { get; set; }
}
