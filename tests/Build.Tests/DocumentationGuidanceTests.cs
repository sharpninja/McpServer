namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-DOC-001: Guards agent-facing documentation against stale REPL,
/// plugin, pipeline, and generated wiki guidance.
/// </summary>
public sealed class DocumentationGuidanceTests
{
    /// <summary>
    /// TEST-MCP-147: Verifies direct agent STDIO guidance uses the current
    /// single-line JSON envelope contract instead of stale formatted YAML.
    /// </summary>
    [Fact]
    public async Task AgentStdioGuidance_UsesSingleLineJson()
    {
        var files = new[]
        {
            "README.md",
            Path.Combine("docs", "AGENT-PLUGIN-AVAILABILITY.md"),
            Path.Combine("docs", "REPL-AGENT-GUIDE.md"),
            Path.Combine("templates", "prompt-templates.yaml"),
        };

        foreach (var relativePath in files)
        {
            var text = await ReadRepositoryTextAsync(relativePath).ConfigureAwait(true);
            Assert.Contains("single-line JSON", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("YAML envelope", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("one YAML request envelope", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("per YAML document", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// TEST-MCP-188: Verifies the marker template pins PowerShell and Node
    /// execution to PowerShell.MCP 1.14 reuse/session routing and names the
    /// Byrd process source document.
    /// </summary>
    [Fact]
    public async Task MarkerTemplate_DefinesPowerShellMcpAndByrdProcessGuidance()
    {
        var text = await ReadRepositoryTextAsync(Path.Combine("templates", "prompt-templates.yaml")).ConfigureAwait(true);

        Assert.Contains("PowerShell.MCP", text, StringComparison.Ordinal);
        Assert.Contains("PSGallery", text, StringComparison.Ordinal);
        Assert.Contains("For every PowerShell Core (`pwsh`) invocation on every operating system", text, StringComparison.Ordinal);
        Assert.Contains("execute_command", text, StringComparison.Ordinal);
        Assert.Contains("Calling `start_console` first is optional", text, StringComparison.Ordinal);
        Assert.Contains("omit `reason` when reuse is preferred", text, StringComparison.Ordinal);
        Assert.Contains("wait_for_completion", text, StringComparison.Ordinal);
        Assert.Contains("reuse one session for the workspace and route `node` invocations through it as well", text, StringComparison.Ordinal);
        Assert.Contains("`Byrd Dev Process`, `BDP`, `BPDv4`, and `Byrd Development Process`", text, StringComparison.Ordinal);
        Assert.Contains("Development-Process-draft-v4.md", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-147: Verifies pipeline documentation references the live
    /// Azure Pipelines and GitHub Actions files that exist in the repository.
    /// </summary>
    [Fact]
    public async Task PipelineGuidance_ReferencesExistingPipelineFiles()
    {
        var resources = typeof(DocumentationGuidanceTests).Assembly.GetManifestResourceNames();
        Assert.Contains("Repository.azure-pipelines.yml", resources);
        Assert.Contains("Repository.github.workflows.build.yml", resources);

        var readme = await ReadRepositoryTextAsync("README.md").ConfigureAwait(true);
        Assert.Contains("azure-pipelines.yml", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".github/workflows/build.yml", readme, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-147: Verifies generated requirements wiki outputs keep the
    /// required Azure and GitHub file sets, including platform-specific files.
    /// </summary>
    [Fact]
    public void RequirementsWiki_AzureAndGitHubOutputsHaveExpectedFiles()
    {
        var resources = typeof(DocumentationGuidanceTests).Assembly.GetManifestResourceNames();

        var requiredFiles = new[]
        {
            ".mcp-requirements-manifest.json",
            "Functional-Requirements.md",
            "Home.md",
            "Requirements-Matrix.md",
            "Technical-Requirements.md",
            "Testing-Requirements.md",
            "TR-per-FR-Mapping.md",
        };

        foreach (var file in requiredFiles)
        {
            Assert.Contains("Repository.wiki.azure." + file, resources);
            Assert.Contains("Repository.wiki.github." + file, resources);
        }

        Assert.Contains("Repository.wiki.azure..order", resources);
        Assert.Contains("Repository.wiki.github._Sidebar.md", resources);
        Assert.Contains("Repository.wiki.github._Footer.md", resources);
    }

    /// <summary>
    /// DOC-SCRATCHINTEGRATION-001: Verifies future-agent documentation covers
    /// scratch workspace SQLite seeding, server startup, marker gating, marker
    /// derived auth, cleanup, and xUnit sequencing requirements.
    /// </summary>
    [Fact]
    public async Task ScratchWorkspaceIntegrationGuide_CoversMarkerGatedSqliteHarness()
    {
        var text = await ReadRepositoryTextAsync(Path.Combine("docs", "context", "scratch-workspace-integration-tests.md"))
            .ConfigureAwait(true);

        AssertContainsAll(
            text,
            "scratch root",
            "scratch workspace",
            "docs/Project/TODO.yaml",
            "templates/prompt-templates.yaml",
            "SQLite",
            "McpDbContext",
            "McpServer.Storage.SqliteMigrations",
            "WorkspaceEntity",
            "CurrentRequirementLayerKey",
            "random high port",
            "McpServer.Support.Mcp",
            "FileSystemWatcher",
            "AGENTS-README-FIRST.yaml",
            "one minute",
            "baseUrl",
            "apiKey",
            "--agent-stdio",
            "--workspace-path",
            "--marker-file",
            "IAsyncLifetime.InitializeAsync",
            "collection fixture",
            "xUnit does not guarantee method ordering",
            "Do not use cached",
            "Do not patch production startup marker generation",
            "process tree");
    }

    private static async Task<string> ReadRepositoryTextAsync(string relativePath)
    {
        var resourceName = "Repository." + relativePath.Replace('\\', '.').Replace('/', '.');
        using var resource = typeof(DocumentationGuidanceTests).Assembly
            .GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded source resource {resourceName}.");
        using var reader = new StreamReader(resource);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static void AssertContainsAll(string text, params string[] requiredText)
    {
        foreach (var value in requiredText)
        {
            Assert.Contains(value, text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
