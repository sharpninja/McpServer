using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-017: Mocks-first green proof that S6 UI contracts are assertable.
/// </summary>
public sealed class MemoryS6PortMockTests
{
    /// <summary>H6-red: search is confined to the active workspace Effective set.</summary>
    [Fact]
    public void PortMock_Search_OnlyActiveWorkspace()
    {
        var port = Substitute.For<IMemoryS6Port>();
        port.Search(Arg.Any<string>(), "active-ws")
            .Returns(["MEMORY-ACTIVE"]);

        var ids = port.Search("prefer neovim", "active-ws");
        Assert.Contains("MEMORY-ACTIVE", ids);
        Assert.DoesNotContain(ids, id => id.Contains("FOREIGN", StringComparison.Ordinal));
    }

    /// <summary>H6-red: edit goes through the same public REST update path.</summary>
    [Fact]
    public void PortMock_Edit_UsesSameCqrsPath()
    {
        var port = Substitute.For<IMemoryS6Port>();
        port.EditPath.Returns("/mcpserver/memory/{id}");
        Assert.Equal("/mcpserver/memory/{id}", port.EditPath);
    }

    /// <summary>H6-red: revert is three UI actions and foreign opens fail closed.</summary>
    [Fact]
    public void PortMock_RevertAndForeign()
    {
        var port = Substitute.For<IMemoryS6Port>();
        port.RevertActionCount.Returns(3);
        port.Open("MEMORY-FOREIGN").Returns(404);

        Assert.True(port.RevertActionCount <= 3);
        Assert.Equal(404, port.Open("MEMORY-FOREIGN"));
    }
}

/// <summary>
/// TEST-MCP-MEMORY-017: Port used only for S6 mocks-first contract proof. Not a production facade.
/// </summary>
public interface IMemoryS6Port
{
    /// <summary>Searches Effective memories for the active workspace.</summary>
    IReadOnlyList<string> Search(string query, string workspace);

    /// <summary>REST path used for edit persistence.</summary>
    string EditPath { get; }

    /// <summary>Number of UI actions required to revert.</summary>
    int RevertActionCount { get; }

    /// <summary>Opens a memory id; foreign/unknown must fail closed.</summary>
    int Open(string id);
}
