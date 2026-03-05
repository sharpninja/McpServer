namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-PLANNED-013: Configuration for TODO persistence backend.
/// </summary>
public sealed class TodoStorageOptions
{
    /// <summary>Configuration section name under Mcp.</summary>
    public const string SectionName = "Mcp:TodoStorage";

    /// <summary>
    /// Backend provider name: yaml or sqlite.
    /// </summary>
    public string Provider { get; set; } = "yaml";

    /// <summary>
     /// SQLite datasource path for TODO storage when provider=sqlite.
    /// Relative paths are resolved under the effective data folder.
     /// </summary>
    public string SqliteDataSource { get; set; } = "mcp.db";
}
