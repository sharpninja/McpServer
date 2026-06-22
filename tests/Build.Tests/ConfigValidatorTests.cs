namespace NukeBuild.Tests;

/// <summary>
/// TEST-NUKE-003: Verifies ConfigValidator correctly parses YAML appsettings
/// and validates MCP instance configuration including port conflicts,
/// missing fields, and provider settings.
/// </summary>
public sealed class ConfigValidatorTests
{
    private static readonly string[] ValidYaml =
    [
        "Mcp:",
        "  Instances:",
        "    default:",
        "      RepoRoot: F:\\GitHub\\McpServer",
        "      Port: 7147",
        "      TodoStorage:",
        "        Provider: database",
        "    alt-local:",
        "      RepoRoot: F:\\GitHub\\McpServer",
        "      Port: 7148",
        "      TodoStorage:",
        "        Provider: sqlite",
        "        SqliteDataSource: todo.db",
    ];

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
        Assert.Equal(@"F:\GitHub\McpServer", instances["default"].RepoRoot);
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
