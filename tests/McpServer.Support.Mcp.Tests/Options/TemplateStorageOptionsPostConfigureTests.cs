using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Options;

/// <summary>
/// Verifies instance-aware prompt template path resolution used by both HTTP and STDIO hosts.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-TPL-001, TR-MCP-TPL-003.
/// Test data uses temporary data folders plus minimal prompt-template YAML files to prove the
/// post-configuration path resolver produces the same effective template file path that
/// <see cref="PromptTemplateService"/> needs for successful list/get operations.
/// </remarks>
public sealed class TemplateStorageOptionsPostConfigureTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    /// <summary>
    /// Verifies that the default relative prompt-template file path is resolved against the effective data folder.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TR-MCP-TPL-001.
    /// Test data uses a temporary data folder and the default relative value
    /// <c>templates\prompt-templates.yaml</c> because that matches the production default used by both hosts.
    /// The test proves the shared post-configurer converts the relative path into a stable absolute path.
    /// </remarks>
    [Fact]
    public void PostConfigure_DefaultRelativePath_ResolvesAgainstEffectiveDataFolder()
    {
        var dataFolder = CreateTempDirectory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataFolder"] = dataFolder,
            })
            .Build();

        var options = new TemplateStorageOptions();
        var sut = new TemplateStorageOptionsPostConfigure(configuration, null);

        sut.PostConfigure(global::Microsoft.Extensions.Options.Options.DefaultName, options);

        var expected = Path.GetFullPath(Path.Combine(dataFolder, "templates", "prompt-templates.yaml"));
        Assert.Equal(expected, options.FilePath);
    }

    /// <summary>
    /// Verifies that an instance-specific prompt-template path override takes precedence over the global value.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TR-MCP-TPL-001, TR-MCP-TPL-003.
    /// Test data uses global and instance-specific relative paths under the same temporary data folder.
    /// This data is used to prove the shared post-configurer preserves the instance override behavior expected by STDIO and HTTP hosts.
    /// </remarks>
    [Fact]
    public void PostConfigure_InstanceOverride_PrefersInstanceSpecificTemplatePath()
    {
        var dataFolder = CreateTempDirectory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataFolder"] = dataFolder,
                ["Mcp:TemplateStorage:FilePath"] = "templates/global.yaml",
                ["Mcp:Instances:alt:TemplateStorage:FilePath"] = "templates/instance.yaml",
            })
            .Build();

        var options = new TemplateStorageOptions();
        var sut = new TemplateStorageOptionsPostConfigure(configuration, "alt");

        sut.PostConfigure(global::Microsoft.Extensions.Options.Options.DefaultName, options);

        var expected = Path.GetFullPath(Path.Combine(dataFolder, "templates", "instance.yaml"));
        Assert.Equal(expected, options.FilePath);
    }

    /// <summary>
    /// Verifies that a prompt template service can list templates after shared post-configuration resolves the YAML file path.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TR-MCP-TPL-001, TR-MCP-TPL-003.
    /// Test data uses a temporary data folder containing a minimal prompt-template YAML file with one
    /// <c>default-marker-prompt</c> entry because that mirrors the failing production scenario.
    /// The test proves the fixed configuration path is sufficient for template listing to return real items instead of an empty result.
    /// </remarks>
    [Fact]
    public async Task QueryAsync_WithResolvedTemplatePath_ReturnsTemplatesFromYaml()
    {
        var dataFolder = CreateTempDirectory();
        var templatesDirectory = Path.Combine(dataFolder, "templates");
        Directory.CreateDirectory(templatesDirectory);

        var templatePath = Path.Combine(templatesDirectory, "prompt-templates.yaml");
        await File.WriteAllTextAsync(
            templatePath,
            """
            templates:
              default-marker-prompt:
                title: Default Marker File Prompt
                category: system
                tags:
                - marker
                content: "Hello {{baseUrl}}"
            """).ConfigureAwait(true);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataFolder"] = dataFolder,
            })
            .Build();

        var options = new TemplateStorageOptions();
        new TemplateStorageOptionsPostConfigure(configuration, null)
            .PostConfigure(global::Microsoft.Extensions.Options.Options.DefaultName, options);

        var renderer = new PromptTemplateRenderer(NullLogger<PromptTemplateRenderer>.Instance);
        using var sut = new PromptTemplateService(
            global::Microsoft.Extensions.Options.Options.Create(options),
            renderer,
            NullLogger<PromptTemplateService>.Instance);

        var result = await sut.QueryAsync().ConfigureAwait(true);

        var template = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("default-marker-prompt", template.Id);
        Assert.Equal("Default Marker File Prompt", template.Title);
        Assert.Equal("system", template.Category);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup for temporary test directories.
            }
        }
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcp-template-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }
}
