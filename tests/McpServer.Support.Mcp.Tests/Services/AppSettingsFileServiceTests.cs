using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-091: Validates flattened configuration reads and YAML patch persistence for the
/// admin configuration management workflow using temporary on-disk appsettings documents.
/// The tests use a real <see cref="ConfigurationBuilder"/> with YAML-backed reload behavior so the
/// service exercises the same file-binding and reload path used by the HTTP endpoints.
/// </summary>
public sealed class AppSettingsFileServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    /// <summary>Initializes a new isolated temporary workspace for each test.</summary>
    public AppSettingsFileServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// TEST-MCP-091: Verifies that the service exposes the effective configuration as flattened keys
    /// after loading a YAML appsettings document with nested sections.
    /// The fixture uses nested voice settings because the new admin endpoints must return the same
    /// flattened key format accepted by the patch endpoint.
    /// </summary>
    [Fact]
    public void GetConfigurationValues_ReturnsFlattenedKeys()
    {
        var yamlPath = Path.Combine(_tempDirectory, "appsettings.yaml");
        File.WriteAllText(
            yamlPath,
            """
            VoiceConversation:
              CopilotModel: gpt-5.3-codex
              DefaultExecutionStrategy: hosted-agentframework
            """);

        var configuration = BuildConfiguration(yamlPath);
        var service = CreateService(configuration);

        var values = service.GetConfigurationValues();

        Assert.Equal("gpt-5.3-codex", values["VoiceConversation:CopilotModel"]);
        Assert.Equal("hosted-agentframework", values["VoiceConversation:DefaultExecutionStrategy"]);
    }

    /// <summary>
    /// TEST-MCP-091: Verifies that flattened updates patch the YAML document, persist to disk, and
    /// reload the active configuration root seen by subsequent reads.
    /// The test patches both an existing value and a newly added nested value so the admin endpoint
    /// contract covers update and additive behaviors in one round-trip.
    /// </summary>
    [Fact]
    public async Task PatchYamlConfigurationAsync_UpdatesYamlAndReloadsConfiguration()
    {
        var yamlPath = Path.Combine(_tempDirectory, "appsettings.yaml");
        await File.WriteAllTextAsync(
            yamlPath,
            """
            VoiceConversation:
              CopilotModel: gpt-5.3-codex
            """).ConfigureAwait(true);

        var configuration = BuildConfiguration(yamlPath);
        var service = CreateService(configuration);

        var updated = await service.PatchYamlConfigurationAsync(
            new Dictionary<string, string?>
            {
                ["VoiceConversation:CopilotModel"] = "gpt-5.4",
                ["VoiceConversation:ModelApiKeyEnvironmentVariableName"] = "AZURE_OPENAI_API_KEY",
            },
            CancellationToken.None).ConfigureAwait(true);

        var yamlText = await File.ReadAllTextAsync(yamlPath).ConfigureAwait(true);

        Assert.Equal("gpt-5.4", configuration["VoiceConversation:CopilotModel"]);
        Assert.Equal("AZURE_OPENAI_API_KEY", configuration["VoiceConversation:ModelApiKeyEnvironmentVariableName"]);
        Assert.Equal("gpt-5.4", updated["VoiceConversation:CopilotModel"]);
        Assert.Equal(
            "AZURE_OPENAI_API_KEY",
            updated["VoiceConversation:ModelApiKeyEnvironmentVariableName"]);
        Assert.Contains("CopilotModel: gpt-5.4", yamlText, StringComparison.Ordinal);
        Assert.Contains(
            "ModelApiKeyEnvironmentVariableName: AZURE_OPENAI_API_KEY",
            yamlText,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-091: Verifies that null values remove dictionary-backed YAML keys instead of leaving
    /// stale configuration in the persisted file.
    /// The test starts with both voice keys present so the patch can prove targeted removal without
    /// disturbing sibling settings in the same section.
    /// </summary>
    [Fact]
    public async Task PatchYamlConfigurationAsync_NullValueRemovesDictionaryKey()
    {
        var yamlPath = Path.Combine(_tempDirectory, "appsettings.yaml");
        await File.WriteAllTextAsync(
            yamlPath,
            """
            VoiceConversation:
              CopilotModel: gpt-5.3-codex
              ModelApiKeyEnvironmentVariableName: OPENAI_API_KEY
            """).ConfigureAwait(true);

        var configuration = BuildConfiguration(yamlPath);
        var service = CreateService(configuration);

        var updated = await service.PatchYamlConfigurationAsync(
            new Dictionary<string, string?> { ["VoiceConversation:ModelApiKeyEnvironmentVariableName"] = null },
            CancellationToken.None).ConfigureAwait(true);

        var yamlText = await File.ReadAllTextAsync(yamlPath).ConfigureAwait(true);

        Assert.Null(configuration["VoiceConversation:ModelApiKeyEnvironmentVariableName"]);
        Assert.False(updated.ContainsKey("VoiceConversation:ModelApiKeyEnvironmentVariableName"));
        Assert.DoesNotContain("ModelApiKeyEnvironmentVariableName", yamlText, StringComparison.Ordinal);
        Assert.Contains("CopilotModel: gpt-5.3-codex", yamlText, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private AppSettingsFileService CreateService(IConfiguration configuration)
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.ContentRootPath.Returns(_tempDirectory);
        return new AppSettingsFileService(configuration, environment);
    }

    private static IConfigurationRoot BuildConfiguration(string yamlPath)
    {
        return new ConfigurationBuilder()
            .AddYamlFile(yamlPath, optional: false, reloadOnChange: false)
            .Build();
    }
}
