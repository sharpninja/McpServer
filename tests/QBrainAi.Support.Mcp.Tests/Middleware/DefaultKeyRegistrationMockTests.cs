using System.Net;
using QBrainAi.Support.Mcp.TestContracts;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Middleware;

/// <summary>Proves the shared registration assertions against independent fake responses.</summary>
/// <remarks>2026-11-26: FR-MCP-009, TR-MCP-WS-004, FR-MCP-132 ac-1. Check success,
/// forbidden operations, unknown credentials and independent side-effect observations.</remarks>
public sealed class DefaultKeyRegistrationMockTests
{
    /// <summary>A valid fixture observation is accepted and each corrupt field is detected.</summary>
    [Theory]
    [InlineData(201, true)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    public void ContractDetectsIncorrectStatusRegistrationAndMarker(int status, bool registered)
    {
        var expected = (HttpStatusCode)status;
        var actual = status switch
        {
            201 => new DefaultKeyRegistrationContract.Observation(HttpStatusCode.Created, true, true),
            401 => new DefaultKeyRegistrationContract.Observation(HttpStatusCode.Unauthorized, false, false),
            _ => new DefaultKeyRegistrationContract.Observation(HttpStatusCode.Forbidden, false, false)
        };

        DefaultKeyRegistrationContract.Verify(expected, registered, actual);
        Assert.NotNull(Record.Exception(() =>
            DefaultKeyRegistrationContract.Verify(expected, registered, actual with { Status = HttpStatusCode.OK })));
        Assert.NotNull(Record.Exception(() =>
            DefaultKeyRegistrationContract.Verify(expected, registered, actual with { IsRegistered = !actual.IsRegistered })));
        Assert.NotNull(Record.Exception(() =>
            DefaultKeyRegistrationContract.Verify(expected, registered, actual with { HasMarker = !actual.HasMarker })));
    }
}
