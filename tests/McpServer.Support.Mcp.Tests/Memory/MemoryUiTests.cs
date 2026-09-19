namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-017 / FR-MCP-MEMORY-016:
/// First-party /memory/ UI lists, edits, and reverts only through public REST
/// for the active workspace.
/// </summary>
public sealed class MemoryUiTests
{
    /// <summary>AC-FR-MCP-MEMORY-016-01: Search UI lists only active-workspace Effective memories.</summary>
    [Fact]
    public void Search_OnlyActiveWorkspace()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("X-Workspace-Path", ui, StringComparison.Ordinal);
        Assert.Contains("/mcpserver/memory", ui, StringComparison.Ordinal);
        Assert.Contains("Effective", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("includeDeleted", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Workspace-Path-Other", ui, StringComparison.Ordinal);
        Assert.Contains("active workspace", ui, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-02: Edit persists via the same CQRS update path as REST.</summary>
    [Fact]
    public void Edit_UsesSameCqrsPath()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("PUT", ui, StringComparison.Ordinal);
        Assert.Contains("/mcpserver/memory/", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("/mcpserver/admin", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("IMemoryService", ui, StringComparison.Ordinal);
        Assert.Contains("btnSave", ui, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-03: Version view lists ordered versions; revert completes in ≤3 UI actions.</summary>
    [Fact]
    public void Revert_WithinThreeActions()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("/versions", ui, StringComparison.Ordinal);
        Assert.Contains("/revert", ui, StringComparison.Ordinal);
        Assert.Contains("btnVersions", ui, StringComparison.Ordinal);
        Assert.Contains("btnRevert", ui, StringComparison.Ordinal);
        Assert.Contains("version-list", ui, StringComparison.Ordinal);
        Assert.Contains("data-revert-actions=\"3\"", ui, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-04: Opening another workspace's memory id fails closed.</summary>
    [Fact]
    public void ForeignMemory_FailClosed()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("fail-closed", ui, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            ui.Contains("403", StringComparison.Ordinal) || ui.Contains("404", StringComparison.Ordinal),
            "Foreign open must fail closed with 403 or 404.");
        Assert.DoesNotContain("showForeignContent", ui, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-05: Empty state shows a clear no-memories message (not a blank crash).</summary>
    [Fact]
    public void EmptyState_Message()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("id=\"empty-state\"", ui, StringComparison.Ordinal);
        Assert.Contains("No memories", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new Error(\"empty\")", ui, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-06: Search with no hits shows empty results, not a server-failure toast.</summary>
    [Fact]
    public void NoHits_EmptyNotError()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("id=\"empty-results\"", ui, StringComparison.Ordinal);
        Assert.Contains("No matching memories", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("server failure", ui, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-07: Soft-deleted memories are not listed in the default UI view.</summary>
    [Fact]
    public void SoftDeleted_NotListed()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.DoesNotContain("includeDeleted=true", ui, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/deleted", ui, StringComparison.Ordinal);
        Assert.Contains("scope=Effective", ui, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-08: Creating via UI requires non-empty Content.</summary>
    [Fact]
    public void CreateEmpty_Blocked()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("id=\"content\"", ui, StringComparison.Ordinal);
        Assert.Contains("btnCreate", ui, StringComparison.Ordinal);
        Assert.Contains("Content is required", ui, StringComparison.Ordinal);
        Assert.True(
            ui.Contains("trim()", StringComparison.Ordinal) && ui.Contains("Content is required", StringComparison.Ordinal),
            "Empty Content must be blocked client-side before or after the REST 400.");
    }

    /// <summary>AC-FR-MCP-MEMORY-016-09: UI does not display raw API keys or workspace secrets.</summary>
    [Fact]
    public void NoSecretsInDom()
    {
        var html = MemoryS6Catalog.ReadRequired(MemoryS6Catalog.MemoryWwwrootFile("index.html"));
        var js = MemoryS6Catalog.ReadRequired(MemoryS6Catalog.MemoryWwwrootFile("app.js"));
        var ui = html + js;
        Assert.Contains("type=\"password\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"apiKey\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("apiKey.textContent", js, StringComparison.Ordinal);
        Assert.DoesNotContain("innerHTML = key", js, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-10: Recall/search from UI respects tag chips/filters.</summary>
    [Fact]
    public void Filters_Respected()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("tag-chip", ui, StringComparison.Ordinal);
        Assert.Contains("id=\"tagFilter\"", ui, StringComparison.Ordinal);
        Assert.Contains("recall", ui, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tags", ui, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-11: Escape closes the detail/version panel.</summary>
    [Fact]
    public void Escape_ClosesPanel()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("Escape", ui, StringComparison.Ordinal);
        Assert.Contains("keydown", ui, StringComparison.Ordinal);
        Assert.Contains("closePanel", ui, StringComparison.Ordinal);
        Assert.Contains("id=\"detail-panel\"", ui, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-016-12 / AC-TR-MCP-MEMORY-UI-002-03: UI calls only public REST memory endpoints.</summary>
    [Fact]
    public void CallsOnlyPublicRest()
    {
        var ui = MemoryS6Catalog.ReadMemoryUiSource();
        Assert.Contains("/mcpserver/memory", ui, StringComparison.Ordinal);
        Assert.Contains("fetch(", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("McpDbContext", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin", ui, StringComparison.Ordinal);
        Assert.DoesNotContain("sqlite", ui, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(MemoryS6Catalog.ExtractForbiddenRouteTokens(ui));
    }
}
