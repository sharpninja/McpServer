namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-017: Shared S6 UI/packaging paths and catalog method names.
/// </summary>
internal static class MemoryS6Catalog
{
    /// <summary>FR-016 + TR-UI catalog methods (20 ACs; CallsOnlyPublicRest is shared).</summary>
    public static readonly string[] CatalogMethods =
    [
        "MemoryUiTests.Search_OnlyActiveWorkspace",
        "MemoryUiTests.Edit_UsesSameCqrsPath",
        "MemoryUiTests.Revert_WithinThreeActions",
        "MemoryUiTests.ForeignMemory_FailClosed",
        "MemoryUiTests.EmptyState_Message",
        "MemoryUiTests.NoHits_EmptyNotError",
        "MemoryUiTests.SoftDeleted_NotListed",
        "MemoryUiTests.CreateEmpty_Blocked",
        "MemoryUiTests.NoSecretsInDom",
        "MemoryUiTests.Filters_Respected",
        "MemoryUiTests.Escape_ClosesPanel",
        "MemoryUiTests.CallsOnlyPublicRest",
        "MemoryUiHostingTests.ServedAtMemoryPath",
        "MemoryUiDeployDocsTests.NamesUpdateService",
        "MemoryUiTests.CallsOnlyPublicRest",
        "MemoryUiHostingTests.Assets_InPublishOutput",
        "MemoryUiHostingTests.DeepLink_DetailOrFailClosed",
        "MemoryUiHostingTests.Auth_SameAsMcpserver",
        "MemoryUiHostingTests.Csp_MatchesSiblingUis",
        "MemoryUiHostingTests.MissingAsset_NotFalse200",
    ];

    /// <summary>Public REST prefix the UI is allowed to call.</summary>
    public const string PublicRestPrefix = "/mcpserver/memory";

    /// <summary>First-party UI path.</summary>
    public const string UiPath = "/memory/";

    /// <summary>Locates the repository root.</summary>
    public static string FindRepoRoot() => MemoryS5Catalog.FindRepoRoot();

    /// <summary>Returns the source-tree wwwroot/memory file path.</summary>
    public static string MemoryWwwrootFile(string fileName)
        => Path.Combine(FindRepoRoot(), "src", "McpServer.Support.Mcp", "wwwroot", "memory", fileName);

    /// <summary>Returns the sibling Use Case Manager wwwroot file path.</summary>
    public static string UseCasesWwwrootFile(string fileName)
        => Path.Combine(FindRepoRoot(), "src", "McpServer.Support.Mcp", "wwwroot", "usecases", fileName);

    /// <summary>Reads a required file or fails with a behavioral missing-asset message.</summary>
    public static string ReadRequired(string path)
    {
        Assert.True(File.Exists(path), "Required S6 asset missing: " + path);
        return File.ReadAllText(path);
    }

    /// <summary>Concatenates memory UI HTML + JS for contract asserts.</summary>
    public static string ReadMemoryUiSource()
        => ReadRequired(MemoryWwwrootFile("index.html")) + Environment.NewLine + ReadRequired(MemoryWwwrootFile("app.js"));

    /// <summary>Concatenates sibling Use Case UI HTML + JS.</summary>
    public static string ReadUseCaseUiSource()
        => ReadRequired(UseCasesWwwrootFile("index.html")) + Environment.NewLine + ReadRequired(UseCasesWwwrootFile("app.js"));

    /// <summary>Reads Program.cs from the host project.</summary>
    public static string ReadProgramSource()
        => ReadRequired(Path.Combine(FindRepoRoot(), "src", "McpServer.Support.Mcp", "Program.cs"));

    /// <summary>Reads the host csproj.</summary>
    public static string ReadHostCsproj()
        => ReadRequired(Path.Combine(FindRepoRoot(), "src", "McpServer.Support.Mcp", "McpServer.Support.Mcp.csproj"));

    /// <summary>Returns non-public route tokens the UI must not call.</summary>
    public static IReadOnlyList<string> ExtractForbiddenRouteTokens(string source)
    {
        var forbidden = new List<string>();
        foreach (var token in new[] { "/admin", "/internal", "McpDbContext" })
        {
            if (source.Contains(token, StringComparison.Ordinal))
                forbidden.Add(token);
        }

        if (source.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
            forbidden.Add("sqlite");

        var index = 0;
        while ((index = source.IndexOf("/mcpserver/", index, StringComparison.Ordinal)) >= 0)
        {
            var slice = source[index..Math.Min(source.Length, index + 40)];
            if (!slice.StartsWith(PublicRestPrefix, StringComparison.Ordinal))
                forbidden.Add(slice.Split('"', '\'', ' ', '`', '<', '>')[0]);
            index += "/mcpserver/".Length;
        }

        return forbidden;
    }
}
