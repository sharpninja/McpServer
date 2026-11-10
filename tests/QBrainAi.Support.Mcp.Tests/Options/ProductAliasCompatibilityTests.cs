using QBrainAi.Support.Mcp;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.Tests.Options;

/// <summary>
/// TR-MCP-QBRAIN-004 and TR-MCP-QBRAIN-005: product route and configuration aliases. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because one of its tests writes the process environment
/// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-QBRAIN-004; TR-MCP-QBRAIN-005; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (ProjectCanonicalSection_EnvironmentAndCommandLineBeatFileQBrainAi) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ProductAliasCompatibilityTests
{
    /// <summary>
    /// TR-MCP-QBRAIN-004: both HTTP prefixes match the same resource paths.
    /// </summary>
    [Fact]
    public void ProductApiPaths_AcceptCanonicalAndLegacyPrefixes()
    {
        Assert.True(ProductApiPaths.IsProductApi("/qbrainai/todo"));
        Assert.True(ProductApiPaths.IsProductApi("/mcpserver/todo"));
        Assert.False(ProductApiPaths.IsProductApi("/health"));
        Assert.True(ProductApiPaths.EqualsProductPath("/qbrainai/todo/", "todo"));
        Assert.True(ProductApiPaths.EqualsProductPath("/mcpserver/memory/recall", "memory/recall"));
        Assert.True(ProductApiPaths.StartsWithProductSuffix("/mcpserver/todo/ITEM-1", "todo"));
        Assert.False(ProductApiPaths.StartsWithProductSuffix("/mcpserver/todoextra", "todo"));
        Assert.True(ProductApiPaths.TryReadResourceId("/mcpserver/memory/MEMORY-A-001", "memory", out var id));
        Assert.Equal("MEMORY-A-001", id);
        Assert.True(ProductApiPaths.TryTakeResourceTail("/qbrainai/todo/PLAN-1?x=1".Split('?')[0], "todo", out var tail));
        Assert.Equal("PLAN-1", tail);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: QBrainAi keys are projected onto Mcp, and Mcp-only keys remain.
    /// </summary>
    [Fact]
    public void ProjectCanonicalSection_QBrainAiWinsAndLegacyOnlyKeysRemain()
    {
        using var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mcp:RepoRoot"] = @"Q:\fixture\McpServer",
            ["Mcp:Port"] = "7000",
            ["Mcp:TodoFilePath"] = "docs/Project/TODO.yaml",
            ["QBrainAi:Port"] = "7147",
            ["QBrainAi:Workspaces:0:WorkspacePath"] = @"Q:\fixture\McpServer",
            ["QBrainAi:Workspaces:0:Name"] = "McpServer",
        });

        McpInstanceResolver.ProjectCanonicalSectionOverLegacy(configuration);

        Assert.Equal("7147", configuration["Mcp:Port"]);
        Assert.Equal(@"Q:\fixture\McpServer", configuration["Mcp:RepoRoot"]);
        Assert.Equal("docs/Project/TODO.yaml", configuration["Mcp:TodoFilePath"]);
        Assert.Equal(@"Q:\fixture\McpServer", configuration["Mcp:Workspaces:0:WorkspacePath"]);
        Assert.Equal("McpServer", configuration["Mcp:Workspaces:0:Name"]);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: projected Mcp values follow provider reload and IOptionsMonitor refresh.
    /// </summary>
    [Fact]
    public void ProjectCanonicalSection_ReloadRefreshesProjectedMcpValue()
    {
        using var configuration = new ConfigurationManager();
        var file = new ReloadableFileStandInSource();
        ((IConfigurationBuilder)configuration).Add(file);
        file.Provider.Replace(new Dictionary<string, string?>
        {
            ["QBrainAi:RepoRoot"] = "/workspace",
            ["Mcp:RepoRoot"] = "/legacy",
        });
        McpInstanceResolver.ProjectCanonicalSectionOverLegacy(configuration);

        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<IngestionOptions>(configuration.GetSection("Mcp"));
        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<IngestionOptions>>();

        Assert.Equal("/workspace", configuration["Mcp:RepoRoot"]);
        Assert.Equal("/workspace", monitor.CurrentValue.RepoRoot);

        file.Provider.Replace(new Dictionary<string, string?>
        {
            ["QBrainAi:RepoRoot"] = "/changed",
        });

        Assert.Equal("/changed", configuration["Mcp:RepoRoot"]);
        Assert.Equal("/changed", monitor.CurrentValue.RepoRoot);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: a QBrainAi workspace list does not keep fields or extra entries from Mcp.
    /// Removing an entry is visible on the next read after the source reloads.
    /// </summary>
    [Fact]
    public void ProjectCanonicalSection_WorkspaceListDoesNotMixAndRefreshesOnReload()
    {
        using var configuration = new ConfigurationManager();
        var file = new ReloadableFileStandInSource();
        ((IConfigurationBuilder)configuration).Add(file);
        file.Provider.Replace(new Dictionary<string, string?>
        {
            ["QBrainAi:Workspaces:0:Name"] = "Canonical",
            ["QBrainAi:Workspaces:0:WorkspacePath"] = "/canonical",
            ["QBrainAi:Workspaces:1:Name"] = "Second",
            ["QBrainAi:Workspaces:1:WorkspacePath"] = "/second",
            ["Mcp:Workspaces:0:Name"] = "Legacy",
            ["Mcp:Workspaces:0:WorkspacePath"] = "/legacy",
            ["Mcp:Workspaces:2:Name"] = "Extra",
            ["Mcp:Workspaces:2:WorkspacePath"] = "/extra",
        });
        McpInstanceResolver.ProjectCanonicalSectionOverLegacy(configuration);

        var listed = McpInstanceResolver.BindEffectiveList<WorkspaceConfigEntry>(configuration, "Workspaces");
        Assert.Equal(2, listed.Count);
        Assert.Equal("Canonical", listed[0].Name);
        Assert.Equal("/canonical", listed[0].WorkspacePath);
        Assert.Equal("Second", listed[1].Name);

        file.Provider.Replace(new Dictionary<string, string?>
        {
            ["QBrainAi:Workspaces:0:Name"] = "Canonical",
            ["QBrainAi:Workspaces:0:WorkspacePath"] = "/canonical",
            ["Mcp:Workspaces:0:Name"] = "Legacy",
            ["Mcp:Workspaces:0:WorkspacePath"] = "/legacy",
        });

        var refreshed = McpInstanceResolver.BindEffectiveList<WorkspaceConfigEntry>(configuration, "Workspaces");
        Assert.Single(refreshed);
        Assert.Equal("Canonical", refreshed[0].Name);
        Assert.Equal("/canonical", refreshed[0].WorkspacePath);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: workspace projection updates QBrainAi and leaves no second Mcp section.
    /// </summary>
    [Fact]
    public async Task WorkspaceProjection_WritesCanonicalSectionAndReloads()
    {
        var root = Path.Combine(Path.GetTempPath(), "qbrain-workspaces-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var yamlPath = Path.Combine(root, "appsettings.yaml");
        await File.WriteAllTextAsync(yamlPath, """
            QBrainAi:
              RepoRoot: /workspace
              Workspaces:
                - WorkspacePath: /one
                  Name: One
                - WorkspacePath: /two
                  Name: Two
            """, TestContext.Current.CancellationToken);

        try
        {
            using var configuration = new ConfigurationManager();
            configuration.SetBasePath(root);
            configuration.AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: false);
            McpInstanceResolver.ProjectCanonicalSectionOverLegacy(configuration);
            Assert.Equal(2, McpInstanceResolver.BindEffectiveList<WorkspaceConfigEntry>(configuration, "Workspaces").Count);

            var writer = new WorkspaceProjectionWriter(configuration, new TempHostEnvironment(root));
            await writer.WriteProjectionAsync(
            [
                new WorkspaceConfigEntry { WorkspacePath = "/one", Name = "OneOnly" },
            ],
            TestContext.Current.CancellationToken);

            var written = await File.ReadAllTextAsync(yamlPath, TestContext.Current.CancellationToken);
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            var document = deserializer.Deserialize<Dictionary<string, object>>(written);
            Assert.Contains("QBrainAi", document.Keys);
            Assert.DoesNotContain("Mcp", document.Keys);

            var listed = McpInstanceResolver.BindEffectiveList<WorkspaceConfigEntry>(configuration, "Workspaces");
            Assert.Single(listed);
            Assert.Equal("OneOnly", listed[0].Name);
            Assert.Equal("/one", listed[0].WorkspacePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class ReloadableFileStandInSource : IConfigurationSource
    {
        /// <summary>Gets the provider whose data tests replace.</summary>
        public ReloadableFileStandInProvider Provider { get; } = new();

        /// <inheritdoc />
        public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
    }

    private sealed class ReloadableFileStandInProvider : ConfigurationProvider
    {
        /// <summary>Replaces this stand-in for a file provider and signals configuration reload.</summary>
        public void Replace(Dictionary<string, string?> values)
        {
            Data = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
            OnReload();
        }
    }

    private sealed class TempHostEnvironment : IHostEnvironment
    {
        public TempHostEnvironment(string contentRoot)
        {
            ContentRootPath = contentRoot;
        }

        /// <inheritdoc />
        public string EnvironmentName { get; set; } = "Test";

        /// <inheritdoc />
        public string ApplicationName { get; set; } = "projection-test";

        /// <inheritdoc />
        public string ContentRootPath { get; set; }

        /// <inheritdoc />
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    /// <summary>
    /// TR-MCP-QBRAIN-003: triage product-workspace detection accepts the checkout name and the new product name.
    /// </summary>
    [Theory]
    [InlineData("QBrainAi", @"E:\other", true)]
    [InlineData("McpServer", @"E:\other", true)]
    [InlineData("notes", @"Q:\fixture\McpServer", true)]
    [InlineData("notes", @"Q:\fixture\QBrainAi", true)]
    [InlineData("notes", @"Q:\fixture\Other", false)]
    public void ProductWorkspace_AcceptsCanonicalAndLegacyNames(string name, string path, bool expected)
    {
        Assert.Equal(expected, ProductWorkspaceIdentity.IsProductWorkspace(name, path));
    }
}
