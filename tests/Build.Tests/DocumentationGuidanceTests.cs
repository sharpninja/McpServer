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
    /// execution to PowerShell.Mcp and names the Byrd process source document.
    /// </summary>
    [Fact]
    public async Task MarkerTemplate_DefinesPowerShellMcpAndByrdProcessGuidance()
    {
        var text = await ReadRepositoryTextAsync(Path.Combine("templates", "prompt-templates.yaml")).ConfigureAwait(true);

        Assert.Contains("PowerShell.Mcp", text, StringComparison.Ordinal);
        Assert.Contains("PSGallery", text, StringComparison.Ordinal);
        Assert.Contains("For every PowerShell Core (`pwsh`) invocation on every operating system", text, StringComparison.Ordinal);
        Assert.Contains("keep one `PowerShell.Mcp` session open for the workspace", text, StringComparison.Ordinal);
        Assert.Contains("route all `node` invocations through the open `PowerShell.Mcp` session", text, StringComparison.Ordinal);
        Assert.Contains("Do not create fresh Node sessions or one-off Node shells per Node call", text, StringComparison.Ordinal);
        Assert.Contains("`Byrd Dev Process`, `BDP`, `BPDv4`, and `Byrd Development Process`", text, StringComparison.Ordinal);
        Assert.Contains(@"F:\GitHub\McpServer\docs\Development-Process-draft-v4.md", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-147: Verifies pipeline documentation references the live
    /// Azure Pipelines and GitHub Actions files that exist in the repository.
    /// </summary>
    [Fact]
    public async Task PipelineGuidance_ReferencesExistingPipelineFiles()
    {
        var root = FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(root, "azure-pipelines.yml")), "Missing root azure-pipelines.yml.");
        Assert.True(File.Exists(Path.Combine(root, ".github", "workflows", "build.yml")), "Missing GitHub Actions workflow.");

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
        var root = FindRepositoryRoot();
        var azureRoot = Path.Combine(root, "docs", "Project", "wiki", "azure");
        var githubRoot = Path.Combine(root, "docs", "Project", "wiki", "github");

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
            Assert.True(File.Exists(Path.Combine(azureRoot, file)), $"Missing Azure wiki file: {file}");
            Assert.True(File.Exists(Path.Combine(githubRoot, file)), $"Missing GitHub wiki file: {file}");
        }

        Assert.True(File.Exists(Path.Combine(azureRoot, ".order")), "Missing Azure wiki .order file.");
        Assert.True(File.Exists(Path.Combine(githubRoot, "_Sidebar.md")), "Missing GitHub wiki _Sidebar.md file.");
        Assert.True(File.Exists(Path.Combine(githubRoot, "_Footer.md")), "Missing GitHub wiki _Footer.md file.");
    }

    private static async Task<string> ReadRepositoryTextAsync(string relativePath)
    {
        var path = Path.Combine(FindRepositoryRoot(), relativePath);
        return await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing McpServer.slnx.");
    }
}
