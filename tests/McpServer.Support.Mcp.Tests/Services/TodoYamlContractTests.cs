using System.Reflection;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies TODO YAML DTO contracts keep serialization compatibility
/// while CA1002 public <c>List&lt;T&gt;</c> exposures are removed.
/// </summary>
public sealed class TodoYamlContractTests
{
    /// <summary>
    /// TEST-MCP-AIUNIT-002: Reflects over the public TODO YAML DTO surface to prove
    /// W8 removes public <c>List&lt;T&gt;</c> properties from the compatibility models.
    /// </summary>
    [Fact]
    public void TodoYamlModels_DoNotExposePublicListProperties()
    {
        Type[] modelTypes =
        [
            typeof(TodoFile),
            typeof(TodoSection),
            typeof(TodoItem),
            typeof(LegacyTodoFlatItem),
            typeof(CodeReviewSection),
            typeof(CodeReviewPhase),
            typeof(CompletedGroup),
        ];

        var publicListProperties = modelTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.PropertyType.IsGenericType)
            .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToArray();

        Assert.Empty(publicListProperties);
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Deserializes and serializes current and legacy TODO YAML shapes
    /// so contract remediation cannot break agent-visible TODO projection compatibility.
    /// </summary>
    [Fact]
    public async Task TodoYamlFileSerializer_RoundTripsCurrentAndLegacyLists()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcpserver-todo-yaml-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "TODO.yaml");
        try
        {
            await File.WriteAllTextAsync(path, """
                mvp-support:
                  high-priority:
                    - id: TODO-001
                      title: Current shape
                      description:
                        - Keep first line
                        - "  preserve indentation"
                      technical-details:
                        - Detail one
                      implementation-tasks:
                        - task: Write compatibility tests
                          done: true
                completed:
                  - date: 2026-07-12
                    items:
                      - id: TODO-000
                        summary: Already done
                notes:
                  - General note
                """, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var file = await TodoYamlFileSerializer.ReadIfExistsAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(file);
            var section = Assert.Single(file!.Sections);
            Assert.Equal("mvp-support", section.Key);
            var item = Assert.Single(section.Value.HighPriority ?? []);
            Assert.Equal("TODO-001", item.Id);
            Assert.Contains("  preserve indentation", item.Description ?? []);
            Assert.Contains("Detail one", item.TechnicalDetails ?? []);
            Assert.NotNull(item.ImplementationTasks);
            Assert.Single(item.ImplementationTasks);
            Assert.NotNull(file.Completed);
            Assert.Single(file.Completed);
            Assert.Contains("General note", file.Notes ?? []);

            var yaml = TodoYamlFileSerializer.Serialize(file);
            Assert.Contains("high-priority", yaml, StringComparison.Ordinal);
            Assert.Contains("TODO-001", yaml, StringComparison.Ordinal);
            Assert.Contains("implementation-tasks", yaml, StringComparison.Ordinal);
            Assert.Contains("General note", yaml, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
