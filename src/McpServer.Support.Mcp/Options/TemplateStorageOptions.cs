namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Configuration for prompt template persistence backend.
/// </summary>
public sealed class TemplateStorageOptions
{
    /// <summary>Configuration section name under Mcp.</summary>
    public const string SectionName = "Mcp:TemplateStorage";

    /// <summary>
    /// Backend provider name (currently only "yaml" is supported).
    /// </summary>
    public string Provider { get; set; } = "yaml";

    /// <summary>
    /// File path for YAML-backed template storage.
    /// Relative paths are resolved under the application base directory.
    /// </summary>
    public string FilePath { get; set; } = "templates/prompt-templates.yaml";
}
