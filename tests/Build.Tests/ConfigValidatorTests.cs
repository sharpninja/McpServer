namespace NukeBuild.Tests;

/// <summary>
/// TEST-NUKE-003: Verifies ConfigValidator correctly parses YAML appsettings
/// and validates MCP instance configuration including port conflicts,
/// missing fields, and provider settings.
/// </summary>
public sealed class ConfigValidatorTests
{
    private static string[] ValidYaml
    {
        get
        {
            using var resource = typeof(ConfigValidatorTests).Assembly
                .GetManifestResourceStream("Build.Tests.Fixtures.valid-instances.yaml")
                ?? throw new InvalidOperationException("Missing embedded valid-instances.yaml.");
            using var reader = new StreamReader(resource);
            return reader.ReadToEnd()
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n');
        }
    }

    [Fact]
    public void ParseInstances_ValidYaml_ReturnsTwoInstances()
    {
        var instances = ConfigValidator.ParseInstances(ValidYaml);
        Assert.NotNull(instances);
        Assert.Equal(2, instances.Count);
        Assert.True(instances.ContainsKey("default"));
        Assert.True(instances.ContainsKey("alt-local"));
    }

    [Fact]
    public void ParseInstances_ValidYaml_ParsesRepoRootAndPort()
    {
        var instances = ConfigValidator.ParseInstances(ValidYaml)!;
        Assert.Equal(@"Q:\__mcp_unit_test__\QBrainAi", instances["default"].RepoRoot);
        Assert.Equal(7147, instances["default"].Port);
    }

    [Fact]
    public void ParseInstances_ValidYaml_ParsesTodoStorage()
    {
        var instances = ConfigValidator.ParseInstances(ValidYaml)!;
        Assert.Equal("database", instances["default"].TodoProvider);
        Assert.Equal("sqlite", instances["alt-local"].TodoProvider);
        Assert.Equal("todo.db", instances["alt-local"].SqliteDataSource);
    }

    /// <summary>
    /// TR-MCP-NUKE-008 AC-001 (BUG-BUILD-005): instances under the canonical QBrainAi: root parse like those under the legacy Mcp:
    /// root; the live appsettings.yaml uses QBrainAi: since the rename. Inline YAML; no fixture file.
    /// </summary>
    [Fact]
    public void ParseInstances_QBrainAiRoot_ReturnsInstances()
    {
        string[] yaml = ["QBrainAi:", "  Instances:", "    default:", "      RepoRoot: .", "      Port: 7147"];

        var instances = ConfigValidator.ParseInstances(yaml);

        Assert.NotNull(instances);
        Assert.Equal(7147, instances!["default"].Port);
        Assert.Equal(".", instances["default"].RepoRoot);
    }

    /// <summary>
    /// TR-MCP-NUKE-008 AC-003 (BUG-BUILD-005, HV round 1 finding F-B5): with both roots in one file, the canonical QBrainAi: root
    /// wins per key whichever root comes first, as CanonicalSectionProjectionProvider does within a file layer; keys and instances
    /// only under the legacy Mcp: root are kept; and a sibling key after one root's Instances block does not stop parsing of the
    /// other root. Inline YAML in both orders; no fixture file.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ParseInstances_BothRoots_CanonicalWinsPerKeyInEitherOrder(bool canonicalFirst)
    {
        string[] canonical = ["QBrainAi:", "  Instances:", "    default:", "      RepoRoot: /canonical", "      Port: 7147", "  Federation:", "    Enabled: true"];
        string[] legacy = ["Mcp:", "  Instances:", "    default:", "      RepoRoot: /legacy", "      Port: 7999", "      TodoStorage:", "        Provider: sqlite", "    alt-local:", "      RepoRoot: /alt", "      Port: 7148"];
        string[] yaml = canonicalFirst ? [.. canonical, .. legacy] : [.. legacy, .. canonical];

        var instances = ConfigValidator.ParseInstances(yaml);

        Assert.NotNull(instances);
        Assert.Equal(7147, instances!["default"].Port);
        Assert.Equal("/canonical", instances["default"].RepoRoot);
        Assert.Equal("sqlite", instances["default"].TodoProvider);
        Assert.Equal(7148, instances["alt-local"].Port);
        Assert.Equal("/alt", instances["alt-local"].RepoRoot);
    }

    [Fact]
    public void ParseInstances_NoMcpSection_ReturnsNull()
    {
        var result = ConfigValidator.ParseInstances(["Logging:", "  Level: Debug"]);
        Assert.Null(result);
    }

    [Fact]
    public void ParseInstances_EmptyInstances_ReturnsEmptyDict()
    {
        var result = ConfigValidator.ParseInstances(["Mcp:", "  Instances:", "  Port: 7147"]);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseInstances_QuotedValues_UnquotesCorrectly()
    {
        string[] yaml = ["Mcp:", "  Instances:", "    test:", "      RepoRoot: 'C:\\test'", "      Port: \"7150\""];
        var instances = ConfigValidator.ParseInstances(yaml)!;
        Assert.Equal(@"C:\test", instances["test"].RepoRoot);
        Assert.Equal(7150, instances["test"].Port);
    }

    [Fact]
    public void Validate_DuplicatePorts_ReturnsError()
    {
        var instances = new Dictionary<string, ConfigValidator.InstanceConfig>
        {
            ["a"] = new() { RepoRoot = "C:\\test", Port = 7147 },
            ["b"] = new() { RepoRoot = "C:\\test", Port = 7147 },
        };

        var errors = ConfigValidator.Validate(instances, _ => true);
        Assert.Single(errors);
        Assert.Contains("Duplicate port", errors[0]);
    }

    [Fact]
    public void Validate_MissingRepoRoot_ReturnsError()
    {
        var instances = new Dictionary<string, ConfigValidator.InstanceConfig>
        {
            ["test"] = new() { RepoRoot = null, Port = 7147 },
        };

        var errors = ConfigValidator.Validate(instances, _ => true);
        Assert.Single(errors);
        Assert.Contains("missing RepoRoot", errors[0]);
    }

    [Fact]
    public void Validate_NonExistentRepoRoot_ReturnsError()
    {
        var instances = new Dictionary<string, ConfigValidator.InstanceConfig>
        {
            ["test"] = new() { RepoRoot = @"C:\nonexistent", Port = 7147 },
        };

        var errors = ConfigValidator.Validate(instances, _ => false);
        Assert.Single(errors);
        Assert.Contains("does not exist", errors[0]);
    }

    [Fact]
    public void Validate_InvalidProvider_ReturnsError()
    {
        var instances = new Dictionary<string, ConfigValidator.InstanceConfig>
        {
            ["test"] = new() { RepoRoot = @"C:\test", Port = 7147, TodoProvider = "mongo" },
        };

        var errors = ConfigValidator.Validate(instances, _ => true);
        Assert.Single(errors);
        Assert.Contains("unsupported TodoStorage provider", errors[0]);
    }

    [Fact]
    public void Validate_RemovedYamlProvider_ReturnsError()
    {
        var instances = new Dictionary<string, ConfigValidator.InstanceConfig>
        {
            ["test"] = new() { RepoRoot = @"C:\test", Port = 7147, TodoProvider = "yaml" },
        };

        var errors = ConfigValidator.Validate(instances, _ => true);
        Assert.Single(errors);
        Assert.Contains("removed", errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("yaml", errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ValidConfig_ReturnsNoErrors()
    {
        var instances = new Dictionary<string, ConfigValidator.InstanceConfig>
        {
            ["default"] = new() { RepoRoot = @"C:\test", Port = 7147, TodoProvider = "database" },
            ["alt"] = new() { RepoRoot = @"C:\test", Port = 7148, TodoProvider = "sqlite", SqliteDataSource = "todo.db" },
        };

        var errors = ConfigValidator.Validate(instances, _ => true);
        Assert.Empty(errors);
    }
}
