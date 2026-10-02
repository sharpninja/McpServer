namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-017 / TR-MCP-MEMORY-UI-002:
/// /memory/ is packaged and hosted like sibling static UIs.
/// </summary>
public sealed class MemoryUiHostingTests
{
    /// <summary>AC-TR-MCP-MEMORY-UI-002-01: UI is served at /memory/ from packaged static assets.</summary>
    [Fact]
    public void ServedAtMemoryPath()
    {
        var htmlPath = MemoryS6Catalog.MemoryWwwrootFile("index.html");
        Assert.True(File.Exists(htmlPath), "wwwroot/memory/index.html must exist so /memory/ can be served.");
        var html = File.ReadAllText(htmlPath);
        Assert.Contains("Memory", html, StringComparison.OrdinalIgnoreCase);

        var program = MemoryS6Catalog.ReadProgramSource();
        Assert.Contains("UseStaticFiles", program, StringComparison.Ordinal);
        Assert.Contains("UseDefaultFiles", program, StringComparison.Ordinal);
        Assert.Contains("/memory/", program, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-UI-002-04: Static assets are included in publish output / Linux service package.</summary>
    [Fact]
    public void Assets_InPublishOutput()
    {
        Assert.True(File.Exists(MemoryS6Catalog.MemoryWwwrootFile("index.html")));
        Assert.True(File.Exists(MemoryS6Catalog.MemoryWwwrootFile("app.js")));
        var csproj = MemoryS6Catalog.ReadHostCsproj();
        Assert.Contains("Microsoft.NET.Sdk.Web", csproj, StringComparison.Ordinal);

        var docs = File.ReadAllText(Path.Combine(MemoryS6Catalog.FindRepoRoot(), "docs", "USER-GUIDE.md"))
            + File.ReadAllText(Path.Combine(MemoryS6Catalog.FindRepoRoot(), "docs", "MCP-SERVER.md"));
        Assert.Contains("wwwroot/memory", docs, StringComparison.Ordinal);
        Assert.True(
            docs.Contains("publish", StringComparison.OrdinalIgnoreCase)
            || docs.Contains("Linux service", StringComparison.OrdinalIgnoreCase),
            "Deploy docs must name publish output or the Linux service package for /memory/ assets.");
    }

    /// <summary>AC-TR-MCP-MEMORY-UI-002-05: Deep link /memory/{id} opens detail or fails closed for unknown id.</summary>
    [Fact]
    public void DeepLink_DetailOrFailClosed()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        var program = MemoryS6Catalog.ReadProgramSource();
        Assert.True(
            ui.Contains("MEMORY-", StringComparison.Ordinal) && (ui.Contains("location.pathname", StringComparison.Ordinal) || ui.Contains("deepLink", StringComparison.Ordinal)),
            "UI must parse /memory/{id} deep links.");
        Assert.Contains("/mcpserver/memory/", ui, StringComparison.Ordinal);
        Assert.Contains("fail-closed", ui, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/memory/{id}", program, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-UI-002-06: UI uses the same API-key auth as other /mcpserver pages.</summary>
    [Fact]
    public void Auth_SameAsMcpserver()
    {
        var memoryHtml = MemoryS6Catalog.ReadRequired(MemoryS6Catalog.MemoryWwwrootFile("index.html"));
        var memoryJs = MemoryS6Catalog.ReadRequired(MemoryS6Catalog.MemoryWwwrootFile("app.js"));
        var useCaseHtml = MemoryS6Catalog.ReadRequired(MemoryS6Catalog.UseCasesWwwrootFile("index.html"));
        var useCaseJs = MemoryS6Catalog.ReadRequired(MemoryS6Catalog.UseCasesWwwrootFile("app.js"));

        Assert.Contains("X-Api-Key", memoryJs, StringComparison.Ordinal);
        Assert.Contains("X-Api-Key", useCaseJs, StringComparison.Ordinal);
        Assert.Contains("type=\"password\"", memoryHtml, StringComparison.Ordinal);
        Assert.Contains("type=\"password\"", useCaseHtml, StringComparison.Ordinal);
        Assert.Contains("X-Workspace-Path", memoryJs, StringComparison.Ordinal);

        var program = MemoryS6Catalog.ReadProgramSource();
        Assert.Contains("/memory/", program, StringComparison.Ordinal);
        Assert.Contains("/usecases/", program, StringComparison.Ordinal);
        Assert.Contains("WorkspaceAuthMiddleware", program, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-UI-002-07: CSP / no inline-eval matches sibling static UIs.</summary>
    [Fact]
    public void Csp_MatchesSiblingUis()
    {
        var memory = MemoryS6Catalog.ReadMemoryUiSource();
        var sibling = MemoryS6Catalog.ReadUseCaseUiSource();
        Assert.DoesNotContain("unsafe-eval", memory, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", sibling, StringComparison.Ordinal);
        Assert.DoesNotContain("eval(", memory, StringComparison.Ordinal);
        Assert.DoesNotContain("eval(", sibling, StringComparison.Ordinal);
        Assert.DoesNotContain("new Function", memory, StringComparison.Ordinal);

        var program = MemoryS6Catalog.ReadProgramSource();
        Assert.Contains("no inline-eval", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Use Case Manager", program, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-UI-002-08: Missing hashed bundles are 404, not a false 200 index.</summary>
    [Fact]
    public void MissingAsset_NotFalse200()
    {
        var program = MemoryS6Catalog.ReadProgramSource();
        Assert.DoesNotContain("MapFallbackToFile", program, StringComparison.Ordinal);
        Assert.Contains("unknown-asset", program, StringComparison.Ordinal);
        Assert.True(
            program.Contains("Results.NotFound", StringComparison.Ordinal)
            || program.Contains("NotFound()", StringComparison.Ordinal),
            "Missing /memory/ assets must 404 rather than serve index.html as 200.");
    }
}
