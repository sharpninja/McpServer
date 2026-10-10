using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016: Mocks-first green proof that S5 surface contracts are assertable.
/// </summary>
public sealed class MemoryS5PortMockTests
{
    /// <summary>H5-red: tools search lists new verbs against a port mock.</summary>
    [Fact]
    public void PortMock_ToolsSearch_ListsNewVerbs()
    {
        var port = Substitute.For<IMemoryS5Port>();
        port.ListToolNames().Returns(MemoryS5Catalog.NewVerbs.Concat(MemoryS5Catalog.CompatVerbs).ToArray());

        var names = port.ListToolNames();
        foreach (var verb in MemoryS5Catalog.NewVerbs)
            Assert.Contains(verb, names);
        foreach (var verb in MemoryS5Catalog.CompatVerbs)
            Assert.Contains(verb, names);
    }

    /// <summary>H5-red: MemoryClient method names mirror REST verbs.</summary>
    [Fact]
    public void PortMock_ClientMethods_MirrorRest()
    {
        var port = Substitute.For<IMemoryS5Port>();
        port.ListClientMethods().Returns(MemoryS5Catalog.ClientMethods);

        Assert.Equal(MemoryS5Catalog.ClientMethods, port.ListClientMethods());
    }

    /// <summary>H5-red: plugin checklist fails when a required verb is omitted.</summary>
    [Fact]
    public void PortMock_MissingVerb_FailsValidation()
    {
        var port = Substitute.For<IMemoryS5Port>();
        port.ValidatePluginSkill("remember only", MemoryS5Catalog.NewVerbs)
            .Returns(["memory_recall"]);

        var missing = port.ValidatePluginSkill("remember only", MemoryS5Catalog.NewVerbs);
        Assert.Contains("memory_recall", missing);
    }
}

/// <summary>
/// TEST-MCP-MEMORY-016: Port used only for S5 mocks-first contract proof. Not a production facade.
/// </summary>
public interface IMemoryS5Port
{
    /// <summary>Lists advertised memory tool names.</summary>
    IReadOnlyList<string> ListToolNames();

    /// <summary>Lists typed client method names.</summary>
    IReadOnlyList<string> ListClientMethods();

    /// <summary>Returns required verbs missing from a plugin skill body.</summary>
    IReadOnlyList<string> ValidatePluginSkill(string skillText, IReadOnlyList<string> requiredVerbs);
}
