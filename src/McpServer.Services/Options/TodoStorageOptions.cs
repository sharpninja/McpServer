namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-PLANNED-013 / TR-MCP-TODO-005 (provider-agnostic): Configuration for TODO persistence backend.
/// </summary>
/// <remarks>
/// When <see cref="Provider"/> is <c>database</c> the TODO subsystem persists through
/// the configured <c>Mcp:Database:Provider</c> via <c>McpDatabaseProviderFactory</c>
/// (see TR-MCP-CFG-007). No sqlite-specific settings are consulted in that mode.
/// </remarks>
public sealed class TodoStorageOptions
{
    /// <summary>Configuration section name under Mcp.</summary>
    public const string SectionName = "Mcp:TodoStorage";

    /// <summary>
    /// Canonical TODO storage provider value that routes through <c>McpDatabaseProviderFactory</c>.
    /// </summary>
    public const string DatabaseProvider = "database";

    /// <summary>
    /// Literal YAML-only provider; bypasses the database entirely.
    /// </summary>
    public const string YamlProvider = "yaml";

    /// <summary>
    /// Legacy provider alias retained for backward compatibility with older appsettings files.
    /// Treated as a synonym of <see cref="DatabaseProvider"/> with a one-time warning log.
    /// </summary>
    public const string LegacySqliteAlias = "sqlite";

    /// <summary>
    /// Backend provider name: <c>yaml</c> or <c>database</c> (recommended).
    /// Legacy value <c>sqlite</c> is accepted and mapped to <c>database</c> with a warning.
    /// </summary>
    public string Provider { get; set; } = DatabaseProvider;

    /// <summary>
    /// When true, on first-boot the server imports any rows from the legacy SQLite
    /// <c>mcp.db</c> TODO tables (at <see cref="SqliteDataSource"/>) into the configured
    /// authoritative database. Idempotent; subsequent starts are no-ops once the target
    /// is non-empty or the marker file is present. Defaults to true when a legacy file exists.
    /// </summary>
    public bool MigrateFromLegacySqlite { get; set; } = true;

    /// <summary>
    /// Legacy SQLite datasource path consulted only by <c>LegacyTodoSqliteMigrator</c>
    /// to locate a pre-existing <c>mcp.db</c> TODO store for one-shot import.
    /// Relative paths are resolved under the effective data folder.
    /// </summary>
    /// <remarks>Retained solely for the legacy-migration code path; not used at steady state.
    /// Will be marked <c>[Obsolete]</c> in TR-MCP-TODO-007 phase 4 once <c>LegacyTodoSqliteMigrator</c> ships.</remarks>
    public string SqliteDataSource { get; set; } = "mcp.db";
}
