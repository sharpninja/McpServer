using QBrainAi.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Options;

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

    /// <summary>
    /// TEST-MCP-QBRAIN-005: QBRAINAI_INSTANCE wins over MCP_INSTANCE when both are set.
    /// Restores both variables after the assertion.
    /// </summary>
    [Fact]
    public void GetRequestedInstanceName_PrefersQBrainAiEnvironmentVariable()
    {
        var previousModern = Environment.GetEnvironmentVariable("QBRAINAI_INSTANCE");
        var previousLegacy = Environment.GetEnvironmentVariable("MCP_INSTANCE");
        try
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", "from-new");
            Environment.SetEnvironmentVariable("MCP_INSTANCE", "from-old");

            var value = McpInstanceResolver.GetRequestedInstanceName([]);

            Assert.Equal("from-new", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", previousModern);
            Environment.SetEnvironmentVariable("MCP_INSTANCE", previousLegacy);
        }
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-005: MCP_INSTANCE still selects the instance when QBRAINAI_INSTANCE is unset.
    /// Restores both variables after the assertion.
    /// </summary>
    [Fact]
    public void GetRequestedInstanceName_FallsBackToLegacyEnvironmentVariable()
    {
        var previousModern = Environment.GetEnvironmentVariable("QBRAINAI_INSTANCE");
        var previousLegacy = Environment.GetEnvironmentVariable("MCP_INSTANCE");
        try
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", null);
            Environment.SetEnvironmentVariable("MCP_INSTANCE", "from-old");

            var value = McpInstanceResolver.GetRequestedInstanceName([]);

            Assert.Equal("from-old", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", previousModern);
            Environment.SetEnvironmentVariable("MCP_INSTANCE", previousLegacy);
        }
    }
}
