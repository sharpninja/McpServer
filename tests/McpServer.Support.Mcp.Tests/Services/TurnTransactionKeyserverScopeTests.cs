using McpServer.TransactionSecurity;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-221 / FR-MCP-173 / TR-MCP-TXNKEY-001: keyserver and coordinator
/// apply only to QuadBrain brain-slot transactions, not general-agent adapters.
/// </summary>
public sealed class TurnTransactionKeyserverScopeTests
{
    /// <summary>Brain-slot publisher party ids require keyserver signing.</summary>
    [Theory]
    [InlineData("brain-slot:arbiter-of-truth")]
    [InlineData("brain-slot:creativity")]
    [InlineData("BRAIN-SLOT:logic")]
    public void RequiresKeyserver_WhenBrainSlotPublisher_ReturnsTrue(string publisherPartyId)
        => Assert.True(TurnTransactionKeyserverScope.RequiresKeyserver(publisherPartyId, "todo.update"));

    /// <summary>Brain-slot and quadbrain operation names require keyserver signing.</summary>
    [Theory]
    [InlineData("brain-slot.invoke")]
    [InlineData("brain-slot.weight-update")]
    [InlineData("quadbrain.orchestrate")]
    public void RequiresKeyserver_WhenQuadBrainOperation_ReturnsTrue(string operationName)
        => Assert.True(TurnTransactionKeyserverScope.RequiresKeyserver("mcpserver", operationName));

    /// <summary>General-agent first-party mutations must not require keyserver signing.</summary>
    [Theory]
    [InlineData("todo.update")]
    [InlineData("todo.create")]
    [InlineData("requirements.fr.update")]
    [InlineData("sessionlog.submit")]
    [InlineData("memory.add")]
    [InlineData("repo.write")]
    [InlineData("github.cli")]
    [InlineData("graphrag.mutate")]
    [InlineData("workflow.todo.update")]
    [InlineData("federation.control")]
    [InlineData("context.mutate")]
    [InlineData("requirements.ingest")]
    public void RequiresKeyserver_WhenGeneralAgentOperation_ReturnsFalse(string operationName)
        => Assert.False(TurnTransactionKeyserverScope.RequiresKeyserver("mcpserver", operationName));

    /// <summary>Adapters skip the coordinator when the operation is not QuadBrain.</summary>
    [Fact]
    public void ShouldBypassCoordinator_WhenGeneralAgentOperation_ReturnsTrue()
    {
        var coordinator = Substitute.For<ITurnTransactionCoordinator>();
        Assert.True(TurnTransactionKeyserverScope.ShouldBypassCoordinator(coordinator, "todo.update"));
        Assert.True(TurnTransactionKeyserverScope.ShouldBypassCoordinator(coordinator, "sessionlog.submit"));
        Assert.True(TurnTransactionKeyserverScope.ShouldBypassCoordinator(coordinator, "requirements.fr.add"));
    }

    /// <summary>Brain-slot operations still use the coordinator when one is registered.</summary>
    [Fact]
    public void ShouldBypassCoordinator_WhenBrainSlotOperationAndCoordinatorPresent_ReturnsFalse()
    {
        var coordinator = Substitute.For<ITurnTransactionCoordinator>();
        Assert.False(TurnTransactionKeyserverScope.ShouldBypassCoordinator(coordinator, "brain-slot.invoke"));
        Assert.False(TurnTransactionKeyserverScope.ShouldBypassCoordinator(
            coordinator,
            "todo.update",
            "brain-slot:arbiter-of-truth"));
    }

    /// <summary>A missing coordinator always bypasses.</summary>
    [Fact]
    public void ShouldBypassCoordinator_WhenCoordinatorNull_ReturnsTrue()
        => Assert.True(TurnTransactionKeyserverScope.ShouldBypassCoordinator(null, "brain-slot.invoke"));

    /// <summary>Request overload matches publisher and operation prefixes.</summary>
    [Fact]
    public void RequiresKeyserver_RequestOverload_UsesPublisherAndOperation()
    {
        var general = new TurnTransactionRequest
        {
            OperationName = "todo.update",
            PublisherPartyId = "mcpserver",
            Mutating = true,
        };
        var quadBrain = new TurnTransactionRequest
        {
            OperationName = "brain-slot.invoke",
            PublisherPartyId = "brain-slot:logic",
            Mutating = true,
        };

        Assert.False(TurnTransactionKeyserverScope.RequiresKeyserver(general));
        Assert.True(TurnTransactionKeyserverScope.RequiresKeyserver(quadBrain));
    }
}
