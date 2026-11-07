using QBrainAi.Support.Mcp.DatabaseMaintenance;
using QBrainAi.Support.Mcp.Storage.Database;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.DatabaseMaintenance;

/// <summary>
/// Tests argument parsing and dry-run transition planning for the database-encryption maintenance command.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-077, TR-MCP-SEC-004, TR-MCP-CFG-007.
/// Test data uses deterministic provider connection strings, placeholder keys, and dry-run command arguments so
/// the maintenance workflow can be validated without requiring live SEE, pg_tde, or SQL Server TDE infrastructure.
/// The class runs in <see cref="ProcessEnvironmentCollection"/> because one of its integration tests writes the process
/// environment (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
/// <para>2026-11-07, FR-MCP-077; TR-MCP-SEC-004; TR-MCP-CFG-007; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (DatabaseMaintenance/McpDatabaseEncryptionTransitionCommandTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class McpDatabaseEncryptionTransitionCommandTests
{
    /// <summary>
    /// Verifies that the maintenance command falls back to <c>MCP_INSTANCE</c> when the caller does not pass <c>--instance</c>.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-077, TR-MCP-CFG-007; classification TR-MCP-USECASE-018 (AC-006),
    /// TR-MCP-USECASE-019 (AC-008).
    /// Test data: an environment-scoped instance name and a minimal verify command.
    /// This data is used to prove that the documented environment-variable override resolves the same effective instance selector as the explicit CLI option.
    /// The test reads and writes the process environment, so it is an integration test (operator 2026-10-19: "If it
    /// depends on the environment, it's an integration test."). The command reads the instance through
    /// <c>ProductEnvironment.GetLegacy("MCP_INSTANCE")</c>, where <c>QBRAINAI_INSTANCE</c> wins when set, so the test
    /// clears <c>QBRAINAI_INSTANCE</c> for its duration and restores both variables; found by the widened hostile
    /// environment gate (IMPL-UNITGREEN-001, runs ug-r5-hostile-wide-1 and -2), where the machine's value won.
    /// </remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public void TryParse_UsesEnvironmentInstance_WhenCliInstanceIsOmitted()
    {
        var previous = Environment.GetEnvironmentVariable("MCP_INSTANCE");
        var previousModern = Environment.GetEnvironmentVariable("QBRAINAI_INSTANCE");
        try
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", null);
            Environment.SetEnvironmentVariable("MCP_INSTANCE", "env-instance");

            var parsed = McpDatabaseEncryptionTransitionCommand.TryParse(
                [
                    "--database-encryption-transition",
                    "verify",
                ],
                out var options,
                out var error);

            Assert.True(parsed);
            Assert.Null(error);
            Assert.NotNull(options);
            Assert.Equal("env-instance", options.InstanceName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCP_INSTANCE", previous);
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", previousModern);
        }
    }
}
