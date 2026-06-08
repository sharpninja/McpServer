using System.Text.Json;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-MEMORY-007: Verifies the canonical REPL YAML schema advertises
/// and constrains the workflow.memory namespace.
/// </summary>
public sealed class MemorySchemaContractTests
{
    /// <summary>The canonical schema contains memory workflow rules, scopes, and ID validation.</summary>
    [Fact]
    public void CanonicalYamlSchema_ContainsMemoryWorkflowRules()
    {
        var schemaPath = Path.Combine(FindRepoRoot(), "docs", "context", "repl-yaml-message.schema.json");
        using var document = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var root = document.RootElement;

        var methodPattern = root
            .GetProperty("properties")
            .GetProperty("payload")
            .GetProperty("properties")
            .GetProperty("method")
            .GetProperty("pattern")
            .GetString();
        Assert.Contains("memory", methodPattern, StringComparison.Ordinal);

        var defs = root.GetProperty("$defs");
        Assert.True(defs.TryGetProperty("memoryRules", out _));
        Assert.True(defs.TryGetProperty("memoryPatch", out _));

        var listScopes = defs
            .GetProperty("memoryListScope")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(["Effective", "Global", "Workspace"], listScopes);

        var mutationScopes = defs
            .GetProperty("memoryScope")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(["Global", "Workspace"], mutationScopes);

        var idPattern = defs.GetProperty("memoryId").GetProperty("pattern").GetString();
        Assert.Equal("^MEMORY-[A-Z0-9]+(?:-[A-Z0-9]+)*-[0-9]{3,}$", idPattern);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docs", "context", "repl-yaml-message.schema.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
