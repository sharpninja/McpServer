namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Configuration for the Requirements Management feature document file paths.
/// </summary>
public sealed class RequirementsOptions
{
    /// <summary>Configuration section name under <c>Mcp</c>.</summary>
    public const string SectionName = "Mcp:Requirements";

    /// <summary>
    /// Path to <c>Functional-Requirements.md</c>. Relative paths are resolved under <c>Mcp:RepoRoot</c>.
    /// </summary>
    public string FunctionalRequirementsPath { get; set; } = Path.Combine("docs", "Project", "Functional-Requirements.md");

    /// <summary>
    /// Path to <c>Technical-Requirements.md</c>. Relative paths are resolved under <c>Mcp:RepoRoot</c>.
    /// </summary>
    public string TechnicalRequirementsPath { get; set; } = Path.Combine("docs", "Project", "Technical-Requirements.md");

    /// <summary>
    /// Path to <c>Testing-Requirements.md</c>. Relative paths are resolved under <c>Mcp:RepoRoot</c>.
    /// </summary>
    public string TestingRequirementsPath { get; set; } = Path.Combine("docs", "Project", "Testing-Requirements.md");

    /// <summary>
    /// Path to <c>TR-per-FR-Mapping.md</c>. Relative paths are resolved under <c>Mcp:RepoRoot</c>.
    /// </summary>
    public string MappingPath { get; set; } = Path.Combine("docs", "Project", "TR-per-FR-Mapping.md");

    /// <summary>
    /// Path to <c>Requirements-Matrix.md</c>. Relative paths are resolved under <c>Mcp:RepoRoot</c>.
    /// </summary>
    public string MatrixPath { get; set; } = Path.Combine("docs", "Project", "Requirements-Matrix.md");

    /// <summary>
    /// Optional wiki export definition file. Relative paths are resolved under the active workspace root.
    /// </summary>
    public string WikiConfigPath { get; set; } = Path.Combine("docs", "wiki.yaml");
}
