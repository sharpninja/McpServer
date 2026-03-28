namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Describes the resolved native at-rest encryption settings for the selected database provider.
/// </summary>
public sealed class McpDatabaseEncryptionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpDatabaseEncryptionOptions"/> class.
    /// </summary>
    /// <param name="enabled">Whether at-rest encryption is required for the selected provider.</param>
    /// <param name="sqliteKey">Resolved SQLite encryption key when one was configured.</param>
    /// <param name="sqliteSeeToolPath">Optional SEE-capable maintenance-tool path for SQLite transition workflows.</param>
    /// <param name="postgreSqlKeyProvider">Resolved PostgreSQL pg_tde key-provider identifier.</param>
    /// <param name="postgreSqlPrincipalKey">Resolved PostgreSQL pg_tde principal-key identifier.</param>
    /// <param name="sqlServerCertificateName">Resolved SQL Server TDE certificate name.</param>
    /// <param name="sqlServerDatabaseEncryptionKeyName">Resolved SQL Server database-encryption-key name.</param>
    public McpDatabaseEncryptionOptions(
        bool enabled,
        string? sqliteKey,
        string? sqliteSeeToolPath,
        string? postgreSqlKeyProvider,
        string? postgreSqlPrincipalKey,
        string? sqlServerCertificateName,
        string? sqlServerDatabaseEncryptionKeyName)
    {
        Enabled = enabled;
        SqliteKey = sqliteKey;
        SqliteSeeToolPath = sqliteSeeToolPath;
        PostgreSqlKeyProvider = postgreSqlKeyProvider;
        PostgreSqlPrincipalKey = postgreSqlPrincipalKey;
        SqlServerCertificateName = sqlServerCertificateName;
        SqlServerDatabaseEncryptionKeyName = sqlServerDatabaseEncryptionKeyName;
    }

    /// <summary>Gets a value indicating whether at-rest encryption is required.</summary>
    public bool Enabled { get; }

    /// <summary>Gets the resolved SQLite encryption key when one was configured.</summary>
    public string? SqliteKey { get; }

    /// <summary>Gets the optional SEE-capable SQLite maintenance tool path.</summary>
    public string? SqliteSeeToolPath { get; }

    /// <summary>Gets the configured PostgreSQL pg_tde key-provider identifier.</summary>
    public string? PostgreSqlKeyProvider { get; }

    /// <summary>Gets the configured PostgreSQL pg_tde principal-key identifier.</summary>
    public string? PostgreSqlPrincipalKey { get; }

    /// <summary>Gets the configured SQL Server TDE certificate name.</summary>
    public string? SqlServerCertificateName { get; }

    /// <summary>Gets the configured SQL Server database-encryption-key name.</summary>
    public string? SqlServerDatabaseEncryptionKeyName { get; }
}
