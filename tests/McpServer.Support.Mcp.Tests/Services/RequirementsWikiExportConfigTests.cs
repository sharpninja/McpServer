using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using YamlDotNet.Serialization;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-WIKIEXPORT-001: docs/wiki.yaml schema and export behavior coverage.</summary>
public sealed class RequirementsWikiExportConfigTests : IDisposable
{
    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder().DisableAliases().Build();
    private readonly string _workspaceRoot = Path.Combine(Path.GetTempPath(), "mcp-wikiexport-tests-" + Guid.NewGuid().ToString("N")[..8]);

    public RequirementsWikiExportConfigTests()
    {
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "docs", "Project"));
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "docs", "wiki"));
        SeedCanonicalDocs();
    }

    public static IEnumerable<object[]> InvalidWikiYamlCases()
    {
        yield return [new Dictionary<string, object?> { ["schema"] = "wrong/v1" }, "schema"];
        yield return [CreateConfig([CreateDocument("home", "Home", "generated:unknown", "Home.md")], [CreateNavigationDocument("home")]), "generated"];
        yield return [CreateConfig([CreateDocument("home", "Home", "docs/Missing.md", "Home.md")], [CreateNavigationDocument("home")]), "source"];
        yield return [CreateConfig([CreateDocument("home", "Home", "generated:home", "../Home.md")], [CreateNavigationDocument("home")]), "target"];
        yield return [CreateConfig([CreateDocument("home", "Home", "generated:home", "Home.md", ["mobile"])], [CreateNavigationDocument("home")]), "platform"];
        yield return [CreateConfig([CreateDocument("home", "Home", "generated:home", "Home.md")], [CreateNavigationDocument("missing")]), "navigation"];
        yield return [
            CreateConfig(
                [
                    CreateDocument("home", "Home", "generated:home", "Home.md"),
                    CreateDocument("home", "Home Again", "generated:functional", "Home-Again.md")
                ],
                [CreateNavigationDocument("home")]),
            "duplicates"];
        yield return [
            CreateConfig(
                [
                    CreateDocument("home", "Home", "generated:home", "Home.md"),
                    CreateDocument("functional", "Functional", "generated:functional", "Functional.md")
                ],
                [CreateNavigationDocument("home")]),
            "does not reference document"];
        yield return [
            CreateConfig(
                [CreateDocument("home", "Home", "generated:home", "Home.md")],
                [CreateNavigationDocument("home"), CreateNavigationDocument("home")]),
            "references document"];
    }

    public static IEnumerable<object[]> InvalidDocFxWorkflowCases()
    {
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(id: "docs"), CreateDocFxWorkflow(id: "DOCS")]), "duplicates workflow id"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(executable: " ")]), "executable"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(arguments: [])]), "arguments"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(arguments: ["docfx", " "])]), "arguments"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(platforms: ["desktop"])]), "platform"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(timeoutSeconds: 0)]), "timeout"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(timeoutSeconds: 3601)]), "timeout"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(id: "api-a", targetRoot: "api"), CreateDocFxWorkflow(id: "api-b", targetRoot: "api")]), "duplicate target root"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(workingDirectory: "../outside")]), "workingDirectory"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(outputRoot: "C:/outside/docfx")]), "outputRoot"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(targetRoot: "../api")]), "targetRoot"];
        yield return [CreateConfigWithDocFx([CreateDocFxWorkflow(targetRoot: "C:/api")]), "targetRoot"];
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            foreach (var file in Directory.EnumerateFiles(_workspaceRoot, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);

            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }

    /// <summary>TEST-MCP-WIKIEXPORT-001-AC2, AC3, AC4, AC5, AC7: valid docs/wiki.yaml drives documents, home, platforms, and navigation.</summary>
    [Fact]
    public async Task GenerateWikiAsync_WithWikiYaml_ExportsConfiguredTreeForGitHubAndAzure()
    {
        File.WriteAllText(Path.Combine(_workspaceRoot, "docs", "Architecture.md"), "# Architecture\n\nSystem overview.\n");
        File.WriteAllText(Path.Combine(_workspaceRoot, "docs", "wiki", "Home.template.md"), "# Custom Home\n\n{{navigation}}\n\n{{documents}}\n\n{{generatedAtUtc}}\n");
        WriteWikiYamlObject(CreateConfig(
            [
                CreateDocument("home", "Home", "generated:home", "Home.md"),
                CreateDocument("functional", "Functional Requirements", "generated:functional", "Requirements/Functional-Requirements.md"),
                CreateDocument("technical", "Technical Requirements", "generated:technical", "Requirements/Technical-Requirements.md", ["github"]),
                CreateDocument("architecture", "Architecture", "docs/Architecture.md", "Architecture.md")
            ],
            [
                CreateNavigationDocument("home"),
                CreateNavigationSection("Requirements", "Requirements", [CreateNavigationDocument("functional"), CreateNavigationDocument("technical")]),
                CreateNavigationDocument("architecture")
            ],
            new Dictionary<string, object?> { ["document"] = "home", ["template"] = "docs/wiki/Home.template.md" }));

        var service = CreateService();
        var exportRoot = Path.Combine(_workspaceRoot, "docs", "Project", "wiki");
        var export = await service.GenerateWikiAsync(exportRoot, DateTimeOffset.Parse("2026-07-04T16:00:00Z"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(export.Success);
        Assert.True(File.Exists(Path.Combine(exportRoot, "github", "Requirements", "Functional-Requirements.md")));
        Assert.True(File.Exists(Path.Combine(exportRoot, "azure", "Requirements", "Functional-Requirements.md")));
        Assert.True(File.Exists(Path.Combine(exportRoot, "github", "Requirements", "Technical-Requirements.md")));
        Assert.False(File.Exists(Path.Combine(exportRoot, "azure", "Requirements", "Technical-Requirements.md")));
        Assert.Equal("# Architecture\n\nSystem overview.\n", File.ReadAllText(Path.Combine(exportRoot, "github", "Architecture.md")));

        var home = File.ReadAllText(Path.Combine(exportRoot, "github", "Home.md"));
        Assert.Contains("# Custom Home", home);
        Assert.Contains("[Functional Requirements](Requirements/Functional-Requirements)", home);
        Assert.Contains("2026-07-04T16:00:00.0000000+00:00", home);

        var sidebar = File.ReadAllText(Path.Combine(exportRoot, "github", "_Sidebar.md"));
        Assert.Contains("- [Home](Home)", sidebar);
        Assert.Contains("- Requirements", sidebar);
        Assert.Contains("  - [Functional Requirements](Requirements/Functional-Requirements)", sidebar);

        Assert.Equal("Home\nRequirements\nArchitecture\n", File.ReadAllText(Path.Combine(exportRoot, "azure", ".order")));
        Assert.Equal("Functional-Requirements\n", File.ReadAllText(Path.Combine(exportRoot, "azure", "Requirements", ".order")));

        var githubManifest = File.ReadAllText(Path.Combine(exportRoot, "github", RequirementsWikiDocumentRenderer.ManifestFileName));
        Assert.Contains("Requirements/Functional-Requirements.md", githubManifest);
        Assert.Contains("Requirements/Technical-Requirements.md", githubManifest);
        var azureManifest = File.ReadAllText(Path.Combine(exportRoot, "azure", RequirementsWikiDocumentRenderer.ManifestFileName));
        Assert.Contains("Requirements/Functional-Requirements.md", azureManifest);
        Assert.DoesNotContain("Requirements/Technical-Requirements.md", azureManifest);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-001-AC5: generated home content falls back to the navigation tree when no home template is configured.</summary>
    [Fact]
    public async Task GenerateWikiAsync_WithWikiYamlWithoutHomeTemplate_UsesDefaultGeneratedHome()
    {
        WriteWikiYamlObject(CreateConfig(
            [
                CreateDocument("home", "Home", "generated:home", "Home.md"),
                CreateDocument("functional", "Functional Requirements", "generated:functional", "Functional-Requirements.md")
            ],
            [
                CreateNavigationDocument("home"),
                CreateNavigationDocument("functional")
            ],
            new Dictionary<string, object?> { ["document"] = "home" }));

        var service = CreateService();
        var exportRoot = Path.Combine(_workspaceRoot, "docs", "Project", "wiki");

        await service.GenerateWikiAsync(exportRoot, DateTimeOffset.Parse("2026-07-04T16:00:00Z"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var home = File.ReadAllText(Path.Combine(exportRoot, "github", "Home.md"));
        Assert.Contains("# Requirements", home);
        Assert.Contains("[Functional Requirements](Functional-Requirements)", home);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-001-AC2: absence of docs/wiki.yaml preserves canonical wiki output.</summary>
    [Fact]
    public async Task GenerateWikiAsync_WithoutWikiYaml_PreservesCanonicalOutput()
    {
        var service = CreateService();
        var exportRoot = Path.Combine(_workspaceRoot, "docs", "Project", "wiki");

        await service.GenerateWikiAsync(exportRoot, DateTimeOffset.Parse("2026-07-04T16:00:00Z"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(File.Exists(Path.Combine(exportRoot, "github", "Functional-Requirements.md")));
        Assert.True(File.Exists(Path.Combine(exportRoot, "azure", "Functional-Requirements.md")));
        Assert.True(File.Exists(Path.Combine(exportRoot, "github", "_Sidebar.md")));
        Assert.True(File.Exists(Path.Combine(exportRoot, "github", "_Footer.md")));
        Assert.True(File.Exists(Path.Combine(exportRoot, "azure", ".order")));
        Assert.False(Directory.Exists(Path.Combine(exportRoot, "github", "Requirements")));
    }

    /// <summary>TEST-MCP-WIKIEXPORT-001-AC1, AC6: invalid config reports actionable errors and does not modify existing exports.</summary>
    [Fact]
    public async Task GenerateWikiAsync_InvalidWikiYaml_FailsBeforeWriting()
    {
        var service = CreateService();
        var exportRoot = Path.Combine(_workspaceRoot, "docs", "Project", "wiki");
        Directory.CreateDirectory(Path.Combine(exportRoot, "github"));
        var existing = Path.Combine(exportRoot, "github", "Home.md");
        File.WriteAllText(existing, "existing");
        var before = File.GetLastWriteTimeUtc(existing);
        WriteWikiYamlObject(CreateConfig(
            [
                CreateDocument("home", "Home", "generated:home", "Home.md"),
                CreateDocument("duplicate", "Duplicate", "generated:functional", "Home.md")
            ],
            [CreateNavigationDocument("home")]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateWikiAsync(exportRoot, ct: TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("duplicate target", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("existing", File.ReadAllText(existing));
        Assert.Equal(before, File.GetLastWriteTimeUtc(existing));
    }

    /// <summary>TEST-MCP-WIKIEXPORT-001-AC1: validation covers bad schema, paths, platforms, sources, and navigation.</summary>
    [Theory]
    [MemberData(nameof(InvalidWikiYamlCases))]
    public void Load_InvalidWikiYaml_ThrowsActionableValidation(Dictionary<string, object?> document, string expectedMessage)
    {
        WriteWikiYamlObject(document);

        var ex = Assert.Throws<InvalidOperationException>(() => RequirementsWikiExportConfigLoader.Load(_workspaceRoot, CreateOptions()));

        Assert.Contains(expectedMessage, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TEST-MCP-DOCFXWIKI-001: omitted DocFX configuration is backward compatible.</summary>
    [Fact]
    public void Load_WithoutDocFxSection_ReturnsEmptyDocFxWorkflows()
    {
        WriteWikiYamlObject(CreateConfig([CreateDocument("home", "Home", "generated:home", "Home.md")], [CreateNavigationDocument("home")]));

        var config = RequirementsWikiExportConfigLoader.Load(_workspaceRoot, CreateOptions());

        Assert.NotNull(config);
        Assert.Empty(config.DocFxWorkflows);
    }

    /// <summary>TEST-MCP-DOCFXWIKI-001: valid DocFX workflow config is normalized and retained.</summary>
    [Fact]
    public void Load_WithValidDocFxWorkflow_ReturnsNormalizedWorkflow()
    {
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "docs", "docfx"));
        WriteWikiYamlObject(CreateConfigWithDocFx([CreateDocFxWorkflow(platforms: ["GitHub"])]));

        var config = RequirementsWikiExportConfigLoader.Load(_workspaceRoot, CreateOptions());

        Assert.NotNull(config);
        var workflow = Assert.Single(config.DocFxWorkflows);
        Assert.Equal("docs", workflow.Id);
        Assert.Equal("dotnet", workflow.Executable);
        Assert.Equal(["docfx", "docfx.json"], workflow.Arguments);
        Assert.Equal(Path.GetFullPath(Path.Combine(_workspaceRoot, "docs", "docfx")), workflow.WorkingDirectoryPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(_workspaceRoot, "docs", "docfx", "_site")), workflow.OutputRootPath);
        Assert.Equal("api", workflow.TargetRoot);
        Assert.Contains("github", workflow.Platforms, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("azure", workflow.Platforms, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(120, workflow.TimeoutSeconds);
    }

    /// <summary>TEST-MCP-DOCFXWIKI-001: invalid DocFX workflow config reports actionable validation messages.</summary>
    [Theory]
    [MemberData(nameof(InvalidDocFxWorkflowCases))]
    public void Load_InvalidDocFxWorkflow_ThrowsActionableValidation(Dictionary<string, object?> document, string expectedMessage)
    {
        WriteWikiYamlObject(document);

        var ex = Assert.Throws<InvalidOperationException>(() => RequirementsWikiExportConfigLoader.Load(_workspaceRoot, CreateOptions()));

        Assert.Contains(expectedMessage, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TEST-MCP-DOCFXWIKI-001: DocFX workflow paths cannot escape the workspace through reparse points.</summary>
    [Fact]
    public void Load_DocFxWorkflowThroughReparsePoint_ThrowsActionableValidation()
    {
        var outsideRoot = Path.Combine(Path.GetTempPath(), "mcp-docfx-outside-" + Guid.NewGuid().ToString("N"));
        var linkPath = Path.Combine(_workspaceRoot, "docs", "docfx-link");
        Directory.CreateDirectory(outsideRoot);
        try
        {
            Directory.CreateSymbolicLink(linkPath, outsideRoot);
            WriteWikiYamlObject(CreateConfigWithDocFx([CreateDocFxWorkflow(workingDirectory: "docs/docfx-link", outputRoot: "docs/docfx-link/_site")]));

            var ex = Assert.Throws<InvalidOperationException>(() => RequirementsWikiExportConfigLoader.Load(_workspaceRoot, CreateOptions()));

            Assert.Contains("reparse", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(linkPath))
                Directory.Delete(linkPath);
            if (Directory.Exists(outsideRoot))
                Directory.Delete(outsideRoot, recursive: true);
        }
    }

    private RequirementsDocumentService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(CreateOptions()), NullLogger<RequirementsDocumentService>.Instance);

    private RequirementsOptions CreateOptions() => new()
    {
        FunctionalRequirementsPath = Path.Combine(_workspaceRoot, "docs", "Project", "Functional-Requirements.md"),
        TechnicalRequirementsPath = Path.Combine(_workspaceRoot, "docs", "Project", "Technical-Requirements.md"),
        TestingRequirementsPath = Path.Combine(_workspaceRoot, "docs", "Project", "Testing-Requirements.md"),
        MappingPath = Path.Combine(_workspaceRoot, "docs", "Project", "TR-per-FR-Mapping.md"),
        MatrixPath = Path.Combine(_workspaceRoot, "docs", "Project", "Requirements-Matrix.md"),
    };

    private void WriteWikiYamlObject(Dictionary<string, object?> document) =>
        File.WriteAllText(Path.Combine(_workspaceRoot, "docs", "wiki.yaml"), s_yamlSerializer.Serialize(document));

    private static Dictionary<string, object?> CreateConfig(
        IReadOnlyList<Dictionary<string, object?>> documents,
        IReadOnlyList<Dictionary<string, object?>> navigation,
        Dictionary<string, object?>? home = null)
    {
        var config = new Dictionary<string, object?>
        {
            ["schema"] = "mcp-wiki-export/v1",
            ["documents"] = documents,
            ["navigation"] = navigation
        };
        if (home is not null)
            config["home"] = home;
        return config;
    }

    private static Dictionary<string, object?> CreateConfigWithDocFx(IReadOnlyList<Dictionary<string, object?>> workflows)
    {
        var config = CreateConfig(
            [CreateDocument("home", "Home", "generated:home", "Home.md")],
            [CreateNavigationDocument("home")]);
        config["docfx"] = new Dictionary<string, object?> { ["workflows"] = workflows };
        return config;
    }

    private static Dictionary<string, object?> CreateDocFxWorkflow(
        string id = "docs",
        string executable = "dotnet",
        IReadOnlyList<string>? arguments = null,
        string workingDirectory = "docs/docfx",
        string outputRoot = "docs/docfx/_site",
        string targetRoot = "api",
        IReadOnlyList<string>? platforms = null,
        int timeoutSeconds = 120)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["executable"] = executable,
            ["arguments"] = arguments ?? ["docfx", "docfx.json"],
            ["workingDirectory"] = workingDirectory,
            ["outputRoot"] = outputRoot,
            ["targetRoot"] = targetRoot,
            ["platforms"] = platforms ?? ["github", "azure"],
            ["timeoutSeconds"] = timeoutSeconds
        };
    }

    private static Dictionary<string, object?> CreateDocument(
        string id,
        string title,
        string source,
        string target,
        IReadOnlyList<string>? platforms = null)
    {
        var document = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["title"] = title,
            ["source"] = source,
            ["target"] = target
        };
        if (platforms is not null)
            document["platforms"] = platforms;
        return document;
    }

    private static Dictionary<string, object?> CreateNavigationDocument(string document) =>
        new() { ["document"] = document };

    private static Dictionary<string, object?> CreateNavigationSection(
        string title,
        string path,
        IReadOnlyList<Dictionary<string, object?>> children) =>
        new()
        {
            ["title"] = title,
            ["path"] = path,
            ["children"] = children
        };

    private void SeedCanonicalDocs()
    {
        var projectDir = Path.Combine(_workspaceRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), "# Functional Requirements (MCP Server)\n\n## FR-MCP-001 Existing FR\n\nFunctional text.\n");
        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), "# Technical Requirements (MCP Server)\n\n## TR-MCP-001\n\nTechnical text.\n");
        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), "# Testing Requirements (MCP Server)\n\n## TEST-MCP\n\n### TEST-MCP-001\n\nTest text.\n");
        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), "# TR per FR Mapping (MCP Server)\n\n| FR | Primary TRs | Tests |\n| --- | --- | --- |\n| FR-MCP-001 | TR-MCP-001 | TEST-MCP-001 |\n");
        File.WriteAllText(Path.Combine(projectDir, "Requirements-Matrix.md"), "# Requirements Matrix\n\n");
    }
}
