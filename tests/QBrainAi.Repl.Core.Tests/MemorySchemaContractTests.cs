using System.Text.Json;
using QBrainAi.Tests.Shared;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-MEMORY-007: Verifies the canonical REPL YAML schema advertises
/// and constrains the workflow.memory namespace. The schema is read from the repository checkout through the shared
/// <see cref="TestRepositoryRoot"/>, so this is an integration test on both hosts (TR-MCP-USECASE-018 AC-006,
/// hostile validation round 2 finding R2-F1).
/// </summary>
[Trait("Category", "Integration")]
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

    private static string FindRepoRoot() => TestRepositoryRoot.Resolve();
}
