using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// Optional context pack source memories is off unless requested and stays scoped.
/// </summary>
public sealed class MemoryContextSourceTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-TR-MCP-MEMORY-API-002-07: memories source is opt-in and caller-scoped.</summary>
    [Fact]
    public async Task MemoriesSource_OptInAndScoped()
    {
        Assert.NotNull(typeof(ContextPackRequest).GetProperty("Sources"));

        var policyType = typeof(MemoryLimits).Assembly.GetTypes()
            .FirstOrDefault(type => type.Name == "MemoryContextSource");
        Assert.NotNull(policyType);
        var name = policyType!.GetField("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.GetValue(null) as string;
        Assert.Equal("memories", name);
        var defaultEnabled = policyType.GetField("DefaultEnabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.GetValue(null);
        Assert.Equal(false, defaultEnabled);

        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "s5",
            Text = "scoped memory for pack",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var listed = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var filter = policyType.GetMethod("FilterEffective", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(filter);

        var none = (IReadOnlyList<MemoryItem>)filter!.Invoke(null, [listed.Items, null])!;
        Assert.DoesNotContain(none, item => item.Id == added.Memory!.Id);

        var optedIn = (IReadOnlyList<MemoryItem>)filter.Invoke(null, [listed.Items, new[] { "memories" }])!;
        Assert.Contains(optedIn, item => item.Id == added.Memory!.Id);

        var foreignListed = await _harness.ListCompatAsync(workspacePath: _harness.WorkspaceB, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var foreign = (IReadOnlyList<MemoryItem>)filter.Invoke(null, [foreignListed.Items, new[] { "memories" }])!;
        Assert.DoesNotContain(foreign, item => item.Id == added.Memory!.Id);
    }
}
