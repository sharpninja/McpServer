using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-017:
/// REPL exposes typed memory methods and returns method-not-found without crashing.
/// </summary>
public sealed class MemoryReplTests
{
    /// <summary>AC-FR-MCP-MEMORY-017-10: REPL exposes typed methods for remember/recall/explore/consolidate/promote/revert.</summary>
    [Fact]
    public void TypedMethods_Present()
    {
        var names = typeof(IMemoryWorkflow).GetMethods().Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("RememberAsync", names);
        Assert.Contains("RecallAsync", names);
        Assert.Contains("ExploreAsync", names);
        Assert.Contains("ConsolidateAsync", names);
        Assert.Contains("PromoteAsync", names);
        Assert.Contains("RevertAsync", names);

        var shapeFields = typeof(MemoryCommandShapes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => field.GetValue(null) as string)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var method in MemoryS5Catalog.ReplMethods)
            Assert.Contains(method, shapeFields);
    }

    /// <summary>AC-FR-MCP-MEMORY-017-11: Invalid REPL memory method returns method-not-found without crashing.</summary>
    [Fact]
    public async Task InvalidMethod_NoCrash()
    {
        var workflow = Substitute.For<IMemoryWorkflow>();
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var dispatcher = new ReplCommandDispatcher(passthrough, memoryWorkflow: workflow);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-s5-invalid-memory",
                Method = "workflow.memory.notARealMethod",
                Params = new Dictionary<string, object?>(),
            },
        };

        var response = await dispatcher.DispatchAsync(envelope, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("error", response.Type);
        var error = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("method_not_found", error.Code);
    }
}
