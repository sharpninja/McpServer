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
    [Fact]
    public void GetRequestedInstanceName_ReadsFromArgs_EqualsSyntax()
    {
        var value = McpInstanceResolver.GetRequestedInstanceName(["--instance=alpha"]);
        Assert.Equal("alpha", value);
    }

    [Fact]
    public void GetRequestedInstanceName_ReadsFromArgs_SeparatedSyntax()
    {
        var value = McpInstanceResolver.GetRequestedInstanceName(["--instance", "beta"]);
        Assert.Equal("beta", value);
    }

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

    [Fact]
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
