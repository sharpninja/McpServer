using Xunit;

namespace McpServer.Support.Mcp.Tests.Web;

/// <summary>
/// FR-MCP-USECASE-007 / TEST-MCP-USECASE-008: First-party Use Case UI assets exist,
/// call live REST only, and provide built-in diagram view/edit plus structure management.
/// </summary>
public sealed class UseCaseUiAssetTests
{
    /// <summary>HTML UI page is present under wwwroot/usecases and references REST.</summary>
    [Fact]
    public void UseCaseUi_IndexHtml_ExistsAndReferencesRestApi()
    {
        var path = FindWwwrootFile("usecases", "index.html");
        Assert.True(File.Exists(path), "Expected wwwroot/usecases/index.html");
        var html = File.ReadAllText(path);
        Assert.Contains("/mcpserver/usecases", html, StringComparison.Ordinal);
        Assert.Contains("app.js", html, StringComparison.Ordinal);
    }

    /// <summary>app.js drives list/create/diagram/coverage via REST only.</summary>
    [Fact]
    public void UseCaseUi_AppJs_CallsLiveRestEndpoints()
    {
        var path = FindWwwrootFile("usecases", "app.js");
        Assert.True(File.Exists(path), "Expected wwwroot/usecases/app.js");
        var js = File.ReadAllText(path);
        Assert.Contains("/mcpserver/usecases", js, StringComparison.Ordinal);
        Assert.Contains("fetch(", js, StringComparison.Ordinal);
        Assert.Contains("diagram", js, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("coverage", js, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("McpDbContext", js, StringComparison.Ordinal);
    }

    /// <summary>
    /// Operator-in-scope: built-in diagram VIEW (rendered surface, not source dump alone)
    /// and EDIT path (structure editor that mutates via REST and refreshes diagram).
    /// </summary>
    [Fact]
    public void UseCaseUi_HasBuiltInDiagramViewAndEditSurface()
    {
        var htmlPath = FindWwwrootFile("usecases", "index.html");
        var jsPath = FindWwwrootFile("usecases", "app.js");
        Assert.True(File.Exists(htmlPath));
        Assert.True(File.Exists(jsPath));
        var html = File.ReadAllText(htmlPath);
        var js = File.ReadAllText(jsPath);

        // Visual render surface (mermaid or dedicated diagram stage)
        Assert.True(
            html.Contains("diagramView", StringComparison.Ordinal)
            || html.Contains("id=\"diagram-view\"", StringComparison.Ordinal)
            || html.Contains("id='diagram-view'", StringComparison.Ordinal),
            "Expected a diagram view container (diagramView / diagram-view).");
        Assert.True(
            html.Contains("mermaid", StringComparison.OrdinalIgnoreCase)
            || js.Contains("mermaid", StringComparison.OrdinalIgnoreCase),
            "Expected mermaid renderer integration for diagram view.");

        // Edit surface: step/flow/actor management that drives the diagram model
        Assert.Contains("btnAddStep", html, StringComparison.Ordinal);
        Assert.Contains("btnAddFlow", html, StringComparison.Ordinal);
        Assert.Contains("btnAttachActor", html, StringComparison.Ordinal);
        Assert.Contains("btnLinkFr", html, StringComparison.Ordinal);

        Assert.Contains("/flows", js, StringComparison.Ordinal);
        Assert.Contains("/steps", js, StringComparison.Ordinal);
        Assert.Contains("/actors", js, StringComparison.Ordinal);
        Assert.Contains("/links", js, StringComparison.Ordinal);
        Assert.Contains("renderDiagram", js, StringComparison.Ordinal);
        Assert.Contains("refreshStructure", js, StringComparison.Ordinal);
    }

    /// <summary>Structure panels exist so diagram edit is model-driven, not freehand-only.</summary>
    [Fact]
    public void UseCaseUi_ExposesStructurePanelsForActorsFlowsStepsLinks()
    {
        var html = File.ReadAllText(FindWwwrootFile("usecases", "index.html"));
        Assert.Contains("id=\"actorsPanel\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"flowsPanel\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"stepsPanel\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"linksPanel\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"stepAction\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"actorName\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"frId\"", html, StringComparison.Ordinal);
    }

    private static string FindWwwrootFile(string folder, string file)
    {
        // Prefer project source tree over bin output.
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "McpServer.Support.Mcp", "wwwroot", folder, file)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot", folder, file)),
            Path.Combine("F:", "GitHub", "McpServer", "src", "McpServer.Support.Mcp", "wwwroot", folder, file),
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}
