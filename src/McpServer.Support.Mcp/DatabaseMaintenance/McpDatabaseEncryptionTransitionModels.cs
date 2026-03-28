using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.DatabaseMaintenance;

/// <summary>
/// TR-MCP-SEC-004, TR-MCP-CFG-007: Supported maintenance operations for database at-rest encryption transitions.
/// </summary>
internal enum McpDatabaseEncryptionTransitionOperation
{
    /// <summary>
    /// Enables provider-native at-rest encryption for the configured database.
    /// </summary>
    Enable,

    /// <summary>
    /// Disables provider-native at-rest encryption for the configured database.
    /// </summary>
    Disable,

    /// <summary>
    /// Verifies the live database encryption state against the configured runtime intent.
    /// </summary>
    Verify,
}

/// <summary>
/// TR-MCP-SEC-004, TR-MCP-CFG-007: Parsed transition options for the database-encryption maintenance command.
/// </summary>
internal sealed class McpDatabaseEncryptionTransitionOptions
{
    /// <summary>
    /// Gets or sets the requested operation.
    /// </summary>
    public required McpDatabaseEncryptionTransitionOperation Operation { get; init; }

    /// <summary>
    /// Gets or sets the optional MCP instance name.
    /// </summary>
    public string? InstanceName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the transition should mutate state instead of emitting a plan only.
    /// </summary>
    public bool Execute { get; init; }

    /// <summary>
    /// Gets or sets the optional backup path.
    /// </summary>
    public string? BackupPath { get; init; }

    /// <summary>
    /// Gets or sets the optional report path.
    /// </summary>
    public string? ReportPath { get; init; }

    /// <summary>
    /// Gets or sets the optional current SQLite key used to open an already-encrypted database.
    /// </summary>
    public string? CurrentKey { get; init; }

    /// <summary>
    /// Gets or sets the optional target SQLite key used to encrypt a plaintext database.
    /// </summary>
    public string? TargetKey { get; init; }

    /// <summary>
    /// Gets or sets the optional override path for the SEE-capable SQLite CLI.
    /// </summary>
    public string? SqliteSeeToolPath { get; init; }

    /// <summary>
    /// Gets or sets the optional override path for pg_dump.
    /// </summary>
    public string? PostgreSqlDumpToolPath { get; init; }

    /// <summary>
    /// Gets or sets the SQL Server poll timeout.
    /// </summary>
    public TimeSpan SqlServerTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets the remaining command-line arguments that should flow into configuration binding.
    /// </summary>
    public IReadOnlyList<string> ConfigurationArguments { get; init; } = [];
}

/// <summary>
/// TR-MCP-SEC-004: One provider-specific transition step recorded in the maintenance report.
/// </summary>
internal sealed class McpDatabaseEncryptionTransitionStep
{
    /// <summary>
    /// Gets or sets the human-readable step title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets or sets the detailed step description.
    /// </summary>
    public required string Detail { get; init; }

    /// <summary>
    /// Gets or sets the optional provider-native command or SQL that will be run.
    /// </summary>
    public string? CommandText { get; init; }

    /// <summary>
    /// Gets or sets the step status.
    /// </summary>
    public string Status { get; set; } = "planned";
}

/// <summary>
/// TR-MCP-SEC-004: Structured report emitted by the database-encryption maintenance command.
/// </summary>
internal sealed class McpDatabaseEncryptionTransitionReport
{
    /// <summary>
    /// Gets or sets the normalized provider name.
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Gets or sets the requested operation.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required McpDatabaseEncryptionTransitionOperation Operation { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the command ran in execution mode.
    /// </summary>
    public bool Execute { get; init; }

    /// <summary>
    /// Gets or sets the optional instance name.
    /// </summary>
    public string? InstanceName { get; init; }

    /// <summary>
    /// Gets the planned or executed steps.
    /// </summary>
    public List<McpDatabaseEncryptionTransitionStep> Steps { get; } = [];

    /// <summary>
    /// Gets the warnings raised while building or executing the transition.
    /// </summary>
    public List<string> Warnings { get; } = [];

    /// <summary>
    /// Gets the informational notes raised while building or executing the transition.
    /// </summary>
    public List<string> Notes { get; } = [];

    /// <summary>
    /// Gets or sets the summary text.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Adds a new step to the report.
    /// </summary>
    /// <param name="title">Short step title.</param>
    /// <param name="detail">Detailed step description.</param>
    /// <param name="commandText">Optional provider-native command or SQL.</param>
    /// <returns>The created step so callers can update its status later.</returns>
    public McpDatabaseEncryptionTransitionStep AddStep(string title, string detail, string? commandText = null)
    {
        var step = new McpDatabaseEncryptionTransitionStep
        {
            Title = title,
            Detail = detail,
            CommandText = commandText,
        };
        Steps.Add(step);
        return step;
    }
}
