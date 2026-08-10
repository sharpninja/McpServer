using Xunit;

namespace McpServer.Support.Mcp.Tests.Web;

/// <summary>
/// TEST-MCP-USECASE-015 / FR-MCP-USECASE-011 / TR-MCP-USECASE-015:
/// Structural contract tests for UML use-case drag-drop canvas (100% AC-011 / AC-T15).
/// </summary>
public sealed class UseCaseCanvasUiAssetTests
{
    /// <summary>AC-011-1 / AC-T15-1: Palette offers Actor, UseCase, SystemBoundary, Association, Include, Extend.</summary>
    [Fact]
    public void Canvas_Palette_OffersRequiredTools()
    {
        var html = Read("index.html");
        foreach (var tool in new[] { "palette-actor", "palette-usecase", "palette-boundary", "palette-association", "palette-include", "palette-extend" })
            Assert.Contains(tool, html, StringComparison.Ordinal);
    }

    /// <summary>AC-011-2 / AC-011-8: Free canvas element is primary diagram surface.</summary>
    [Fact]
    public void Canvas_HasUmlCanvasElement()
    {
        var html = Read("index.html");
        Assert.Contains("id=\"umlCanvas\"", html, StringComparison.Ordinal);
        Assert.Contains("UML use-case canvas", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-011-3..7: Editor module exposes place, connect, rename, move, boundary APIs.</summary>
    [Fact]
    public void Canvas_EditorScript_ExposesInteractionApis()
    {
        var js = Read("canvas-editor.js");
        Assert.Contains("placeNode", js, StringComparison.Ordinal);
        Assert.Contains("startConnect", js, StringComparison.Ordinal);
        Assert.Contains("completeConnect", js, StringComparison.Ordinal);
        Assert.Contains("renameSelected", js, StringComparison.Ordinal);
        Assert.Contains("moveNode", js, StringComparison.Ordinal);
        Assert.Contains("systemBoundary", js, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("association", js, StringComparison.Ordinal);
        Assert.Contains("include", js, StringComparison.Ordinal);
        Assert.Contains("extend", js, StringComparison.Ordinal);
    }

    /// <summary>AC-011-6 / AC-012: Graph layout coordinates serialize to REST body.</summary>
    [Fact]
    public void Canvas_Editor_SerializesGraphWithCoordinates()
    {
        var js = Read("canvas-editor.js");
        Assert.Contains("toGraph", js, StringComparison.Ordinal);
        Assert.Contains("fromGraph", js, StringComparison.Ordinal);
        Assert.True(js.Contains("x:", StringComparison.Ordinal) || js.Contains("\"x\"", StringComparison.Ordinal), "Expected x coordinate in graph serialize.");
        Assert.True(js.Contains("y:", StringComparison.Ordinal) || js.Contains("\"y\"", StringComparison.Ordinal), "Expected y coordinate in graph serialize.");
    }

    /// <summary>AC-011-9 / AC-T15-2: Save/load via diagram-graph REST only.</summary>
    [Fact]
    public void Canvas_AppJs_WiresDiagramGraphRest()
    {
        var js = Read("app.js");
        Assert.Contains("/diagram-graph", js, StringComparison.Ordinal);
        Assert.Contains("PUT", js, StringComparison.Ordinal);
        Assert.Contains("loadDiagramGraph", js, StringComparison.Ordinal);
        Assert.Contains("saveDiagramGraph", js, StringComparison.Ordinal);
        Assert.DoesNotContain("McpDbContext", js, StringComparison.Ordinal);
        Assert.DoesNotContain("McpDbContext", Read("canvas-editor.js"), StringComparison.Ordinal);
    }

    /// <summary>AC-T15-1: Canvas editor script is referenced from index.</summary>
    [Fact]
    public void Canvas_Index_ReferencesCanvasEditorScript()
    {
        var html = Read("index.html");
        Assert.Contains("canvas-editor.js", html, StringComparison.Ordinal);
        Assert.Contains("btnSaveGraph", html, StringComparison.Ordinal);
        Assert.Contains("btnLoadGraph", html, StringComparison.Ordinal);
    }

    private static string Read(string file)
    {
        var path = FindWwwrootFile("usecases", file);
        Assert.True(File.Exists(path), "Expected wwwroot/usecases/" + file + " at " + path);
        return File.ReadAllText(path);
    }

    private static string FindWwwrootFile(string folder, string file)
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "McpServer.Support.Mcp", "wwwroot", folder, file)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot", folder, file)),
            Path.Combine("F:", "GitHub", "McpServer", "src", "McpServer.Support.Mcp", "wwwroot", folder, file),
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}
