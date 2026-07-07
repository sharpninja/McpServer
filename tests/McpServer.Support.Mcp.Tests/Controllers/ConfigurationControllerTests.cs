using System.Reflection;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-091: Validates the admin configuration controller contract for flattened configuration reads,
/// YAML-backed patch writes, and standard JWT admin authorization metadata.
/// The tests use a real YAML-backed <see cref="ConfigurationBuilder"/> so controller actions exercise the
/// same file-persistence and reload path used by the production endpoints.
/// </summary>
public sealed class ConfigurationControllerTests : IDisposable
{
    private readonly string _tempDirectory;

    /// <summary>Initializes a new isolated temporary workspace for each controller test.</summary>
    public ConfigurationControllerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-config-controller-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// TEST-MCP-091: Verifies that the GET action returns an <see cref="OkObjectResult"/> containing the
    /// effective flattened configuration dictionary for a YAML-backed configuration file.
    /// Nested voice settings are used because the endpoint contract exposes <c>section:key</c> pairs rather
    /// than hierarchical JSON objects.
    /// </summary>
    [Fact]
    public void GetConfigurationValues_ReturnsFlattenedDictionary()
    {
        var controller = CreateController(
            """
            VoiceConversation:
              CopilotModel: gpt-5.3-codex
              DefaultExecutionStrategy: hosted-mcp-agent
            """);

        var result = controller.GetConfigurationValues();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var values = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(ok.Value);
        Assert.Equal("gpt-5.3-codex", values["VoiceConversation:CopilotModel"]);
        Assert.Equal("hosted-mcp-agent", values["VoiceConversation:DefaultExecutionStrategy"]);
    }

    /// <summary>
    /// TEST-MCP-091: Verifies that the PATCH action persists submitted flattened values to
    /// <c>appsettings.yaml</c>, reloads the active configuration, and returns the updated flattened view.
    /// The test updates one existing value and adds one new value to confirm the endpoint supports both
    /// replacement and additive patch semantics.
    /// </summary>
    [Fact]
    public async Task PatchConfigurationValuesAsync_ValidValues_ReturnsUpdatedDictionary()
    {
        var yamlPath = Path.Combine(_tempDirectory, "appsettings.yaml");
        await File.WriteAllTextAsync(
            yamlPath,
            """
            VoiceConversation:
              CopilotModel: gpt-5.3-codex
            """, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var configuration = BuildConfiguration(yamlPath);
        var controller = new ConfigurationController(CreateService(configuration));

        var result = await controller.PatchConfigurationValuesAsync(
            new Dictionary<string, string?>
            {
                ["VoiceConversation:CopilotModel"] = "gpt-5.4",
                ["VoiceConversation:ModelApiKeyEnvironmentVariableName"] = "OPENAI_API_KEY",
            },
            CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var values = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(ok.Value);
        var yamlText = await File.ReadAllTextAsync(yamlPath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("gpt-5.4", values["VoiceConversation:CopilotModel"]);
        Assert.Equal("OPENAI_API_KEY", values["VoiceConversation:ModelApiKeyEnvironmentVariableName"]);
        Assert.Equal("gpt-5.4", configuration["VoiceConversation:CopilotModel"]);
        Assert.Contains("CopilotModel: gpt-5.4", yamlText, StringComparison.Ordinal);
        Assert.Contains("ModelApiKeyEnvironmentVariableName: OPENAI_API_KEY", yamlText, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-091: Verifies that all configuration endpoints inherit standard JWT Bearer admin
    /// authorization from the controller-level <see cref="AuthorizeAttribute"/>.
    /// The reflection check ensures the HTTP surface stays wired to Bearer authentication with the
    /// <c>admin</c> role even if action implementations are refactored later.
    /// </summary>
    [Fact]
    public void ConfigurationController_RequiresJwtBearerAdminAuthorization()
    {
        var attribute = typeof(ConfigurationController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, attribute!.AuthenticationSchemes);
        Assert.Equal("admin", attribute.Roles);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private ConfigurationController CreateController(string yamlContent)
    {
        var yamlPath = Path.Combine(_tempDirectory, "appsettings.yaml");
        File.WriteAllText(yamlPath, yamlContent);
        var configuration = BuildConfiguration(yamlPath);
        return new ConfigurationController(CreateService(configuration));
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
