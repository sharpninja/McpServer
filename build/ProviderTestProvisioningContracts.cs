/// <summary>
/// TR-MCP-NUKE-018: State of a Docker container as seen by the provider test provisioning.
/// </summary>
enum ProviderTestContainerState
{
    /// <summary>No container with the name exists.</summary>
    Missing,

    /// <summary>The container exists and is not running.</summary>
    Stopped,

    /// <summary>The container is running.</summary>
    Running,
}

/// <summary>
/// TR-MCP-NUKE-018: Host boundary of the Linux provider test provisioning: process environment, the test-database
/// env file, the two dedicated qbrain-test containers, and TCP reachability. Implementations never log values.
/// </summary>
interface IProviderTestHost
{
    /// <summary>Returns the process environment variable, or null when unset.</summary>
    string? GetVariable(string name);

    /// <summary>Sets a process environment variable inherited by test processes the build starts.</summary>
    void SetVariable(string name, string value);

    /// <summary>Returns the file text, or null when the file does not exist.</summary>
    string? ReadFile(string path);

    /// <summary>Returns the state of the named container.</summary>
    ProviderTestContainerState GetContainerState(string containerName);

    /// <summary>Starts the named stopped container; returns true when it is running afterwards.</summary>
    bool StartContainer(string containerName);

    /// <summary>Returns true when host:port accepts a TCP connection within the timeout.</summary>
    bool CanConnect(string host, int port, TimeSpan timeout);
}

/// <summary>
/// TR-MCP-NUKE-018: Outcome of provider test provisioning. Actions and errors carry variable names, file paths,
/// container names and host:port endpoints only, never connection-string values (AC-4).
/// </summary>
/// <param name="Actions">Ordered actions taken.</param>
/// <param name="Errors">Problems that fail the provisioning (AC-3, no skip).</param>
sealed record ProviderTestProvisioningResult(IReadOnlyList<string> Actions, IReadOnlyList<string> Errors)
{
    /// <summary>True when provisioning found no problem.</summary>
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>
/// TR-MCP-NUKE-018: Linux provisioning of the PostgreSQL and SQL Server endpoints used by the Category=Integration
/// provider tests (AC-1 to AC-4, AC-6).
/// </summary>
interface IProviderTestProvisioner
{
    /// <summary>Provisions both endpoints through the host boundary, reading the env file when needed.</summary>
    ProviderTestProvisioningResult Provision(string envFilePath, IProviderTestHost host);
}

/// <summary>
/// TR-MCP-NUKE-018: Names fixed by the operator decision of 2026-10-11 (BUG-BASELINE-005).
/// </summary>
static class ProviderTestNames
{
    /// <summary>PostgreSQL server connection string variable read by EphemeralPostgresFixture.</summary>
    public const string PostgresVariable = "MCP_TEST_POSTGRES_CONNECTION";

    /// <summary>SQL Server connection string variable read by the SQL Server provider tests.</summary>
    public const string SqlServerVariable = "MCP_TEST_SQLSERVER_CONNECTION";

    /// <summary>Dedicated PostgreSQL test container.</summary>
    public const string PostgresContainer = "qbrain-test-postgres";

    /// <summary>Dedicated SQL Server test container.</summary>
    public const string SqlServerContainer = "qbrain-test-sqlserver";

    /// <summary>Default env file, relative to the user home directory.</summary>
    public const string DefaultEnvFileRelativeToHome = ".config/qbrain-test/test-db.env";
}
