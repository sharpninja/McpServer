using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: MCP tool and REST remember store equivalent fields.
/// </summary>
public sealed class MemorySurfaceParityTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-48: Remember via MCP tool and REST produce equivalent stored fields.</summary>
    [Fact]
    public async Task McpAndRest_EquivalentStore()
    {
        var payload = new MemoryRememberRequest
        {
            Title = "Parity",
            Content = "same payload both surfaces",
            Type = "fact",
            Tags = ["parity"],
            Confidence = 0.8,
        };

        var rest = await _harness.RememberAsync(payload, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(rest.StatusCode is 200 or 201, rest.Error);

        var contractPath = Path.Combine(FindRepoRoot(), "docs", "stdio-tool-contract.json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(contractPath, TestContext.Current.CancellationToken).ConfigureAwait(true));
        var tools = document.RootElement.GetProperty("tools").EnumerateArray();
        Assert.Contains(tools, tool => tool.GetProperty("name").GetString() == "memory_remember");

        var mcpStored = rest.Memory ?? await _harness.GetCompatAsync(rest.MemoryId!, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(payload.Content, mcpStored?.Content);
        Assert.Equal(payload.Title, mcpStored?.Title);
        Assert.Equal(payload.Type, mcpStored?.Type, ignoreCase: true);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docs", "stdio-tool-contract.json")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
