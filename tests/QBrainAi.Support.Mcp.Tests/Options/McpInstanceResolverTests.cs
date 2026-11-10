using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Options;

/// <summary>
/// Tests instance-name resolution and instance validation. The class runs in <see cref="ProcessEnvironmentCollection"/>
/// because two of its tests write the process environment; those two are integration tests (TR-MCP-USECASE-018
/// AC-006, TR-MCP-USECASE-019 AC-008; hostile validation round 5 finding R5-F2 and the sweep of plan section 19.3).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (GetRequestedInstanceName_PrefersQBrainAiEnvironmentVariable and GetRequestedInstanceName_FallsBackToLegacyEnvironmentVariable) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class McpInstanceResolverTests
{
    /// <summary>
    /// No requirement id applies: GetRequestedInstanceName reads the instance name alpha from the argument
    /// --instance=alpha. Data: the argument array holding --instance=alpha.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for --instance, instance name, per-instance, instance with argument, command line, override or resolve, GetRequestedInstanceName and Mcp:Instances (uncited-matches.txt, test 4 of 23); the nearest, TEST-MCP-QBRAIN-005 criterion 4, states the environment-variable precedence, not the command-line argument form. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void GetRequestedInstanceName_ReadsFromArgs_EqualsSyntax()
    {
        var value = McpInstanceResolver.GetRequestedInstanceName(["--instance=alpha"]);
        Assert.Equal("alpha", value);
    }

    /// <summary>
    /// No requirement id applies: GetRequestedInstanceName reads the instance name beta from the separated arguments
    /// --instance and beta. Data: the argument array holding --instance and beta.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for --instance, instance name, per-instance, instance with argument, command line, override or resolve, GetRequestedInstanceName and Mcp:Instances (uncited-matches.txt, test 5 of 23); the nearest, TEST-MCP-QBRAIN-005 criterion 4, states the environment-variable precedence only, not the separated argument form. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void GetRequestedInstanceName_ReadsFromArgs_SeparatedSyntax()
    {
        var value = McpInstanceResolver.GetRequestedInstanceName(["--instance", "beta"]);
        Assert.Equal("beta", value);
    }

    /// <summary>
    /// FR-MCP-001: for instance alt the effective RepoRoot is temp_test from Mcp:Instances:alt:RepoRoot, not the
    /// Mcp:RepoRoot value. Data: in-memory configuration with Mcp:RepoRoot set to . and Mcp:Instances:alt:RepoRoot set
    /// to temp_test.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void GetEffectiveMcpValue_PrefersInstanceOverride()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:RepoRoot"] = ".",
            ["Mcp:Instances:alt:RepoRoot"] = "temp_test",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var value = McpInstanceResolver.GetEffectiveMcpValue(configuration, "alt", "RepoRoot");
        Assert.Equal("temp_test", value);
    }

    /// <summary>
    /// No requirement id applies: ValidateInstances throws InvalidOperationException when instances a and b both use
    /// port 7147. Data: in-memory configuration with both instances on RepoRoot . and Port 7147.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for duplicate port, same port, instance near port, port near duplicate, conflict, collision, unique or same, ValidateInstances and Mcp:Instances (uncited-matches.txt, test 6 of 23); the hits (TR-MCP-NUKE-019 AC-6, a free port for a temporary instance, and TR-MCP-CFG-003, which has no criteria) state no instance port uniqueness. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void ValidateInstances_ThrowsOnDuplicatePorts()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:Instances:a:RepoRoot"] = ".",
            ["Mcp:Instances:a:Port"] = "7147",
            ["Mcp:Instances:b:RepoRoot"] = ".",
            ["Mcp:Instances:b:Port"] = "7147",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateInstances(configuration));
    }

    /// <summary>
    /// FR-MCP-001: ValidateInstances throws InvalidOperationException whose message says the RepoRoot does not exist
    /// when an instance names a folder that is not on disk. Data: in-memory configuration with instance a on port 7147
    /// and a RepoRoot of a never-created folder under the temporary folder.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ValidateInstances_ThrowsWhenRepoRootMissingOnDisk()
    {
        var missingRoot = Path.Combine(Path.GetTempPath(), $"mcp-missing-{Guid.NewGuid():N}");
        var data = new Dictionary<string, string?>
        {
            ["Mcp:Instances:a:RepoRoot"] = missingRoot,
            ["Mcp:Instances:a:Port"] = "7147",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateInstances(configuration));
        Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR-MCP-072; TR-MCP-TODO-005: ValidateTodoStorage rejects the unknown TODO storage provider memory with an
    /// InvalidOperationException whose message names it. Data: in-memory configuration with Mcp:TodoStorage:Provider
    /// set to memory.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-072; TR-MCP-TODO-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ValidateTodoStorage_RejectsUnknownProvider()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "memory",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateTodoStorage(configuration, null));
        Assert.Contains("memory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR-MCP-072; TR-MCP-TODO-005: ValidateTodoStorage accepts the database TODO storage provider when
    /// Mcp:Database:Provider is set, and does not throw. Data: in-memory configuration with Mcp:TodoStorage:Provider
    /// set to database and Mcp:Database:Provider set to sqlserver.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-072; TR-MCP-TODO-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ValidateTodoStorage_AcceptsDatabaseProvider_WhenDatabaseConfigured()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "database",
            ["Mcp:Database:Provider"] = "sqlserver",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        McpInstanceResolver.ValidateTodoStorage(configuration, null);
    }

    /// <summary>
    /// TR-MCP-TODO-005: The removed <c>yaml</c> TODO provider is rejected with a clear error so stale
    /// configuration fails fast rather than silently degrading. Uses an in-memory config with
    /// <c>Mcp:TodoStorage:Provider=yaml</c>.
    /// </summary>
    [Fact]
    public void ValidateTodoStorage_RejectsRemovedYamlProvider()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "yaml",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => McpInstanceResolver.ValidateTodoStorage(configuration, null));
        Assert.Contains("yaml", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("database", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR-MCP-072; TR-MCP-TODO-005: ValidateTodoStorage accepts the legacy sqlite TODO storage provider as an alias of
    /// database when Mcp:Database:Provider is set, and does not throw. Data: in-memory configuration with
    /// Mcp:TodoStorage:Provider set to sqlite and Mcp:Database:Provider set to sqlite.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-072; TR-MCP-TODO-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ValidateTodoStorage_AliasesLegacySqliteToDatabase_WhenDatabaseConfigured()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "sqlite",
            ["Mcp:Database:Provider"] = "sqlite",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        McpInstanceResolver.ValidateTodoStorage(configuration, null);
    }

    /// <summary>
    /// FR-MCP-072; TR-MCP-TODO-005: ValidateTodoStorage rejects the database TODO storage provider when
    /// Mcp:Database:Provider is missing, with an InvalidOperationException whose message names Mcp:Database:Provider.
    /// Data: in-memory configuration with only Mcp:TodoStorage:Provider, set to database.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-072; TR-MCP-TODO-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ValidateTodoStorage_RequiresMcpDatabaseProvider_WhenProviderIsDatabase()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "database",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateTodoStorage(configuration, null));
        Assert.Contains("Mcp:Database:Provider", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-MCP-072; TR-MCP-TODO-005: ValidateTodoStorage rejects the legacy sqlite TODO storage provider with an
    /// InvalidOperationException when Mcp:Database:Provider is missing. Data: in-memory configuration with only
    /// Mcp:TodoStorage:Provider, set to sqlite.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-072; TR-MCP-TODO-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ValidateTodoStorage_LegacySqliteAlias_FailsWhenDatabaseProviderMissing()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "sqlite",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateTodoStorage(configuration, null));
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-005: When both product sections set RepoRoot, QBrainAi wins.
    /// Uses an in-memory configuration with Mcp:RepoRoot=legacy and QBrainAi:RepoRoot=canonical.
    /// </summary>
    [Fact]
    public void GetEffectiveMcpValue_PrefersQBrainAiSectionWhenBothPresent()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:RepoRoot"] = "legacy",
            ["QBrainAi:RepoRoot"] = "canonical",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var value = McpInstanceResolver.GetEffectiveMcpValue(configuration, null, "RepoRoot");

        Assert.Equal("canonical", value);
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-005: A 1.x configuration that only has the Mcp section still resolves.
    /// Uses an in-memory configuration with Mcp:RepoRoot and no QBrainAi section.
    /// </summary>
    [Fact]
    public void GetEffectiveMcpValue_ReadsLegacySectionWhenCanonicalAbsent()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:RepoRoot"] = "legacy-only",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var value = McpInstanceResolver.GetEffectiveMcpValue(configuration, null, "RepoRoot");

        Assert.Equal("legacy-only", value);
    }
}
