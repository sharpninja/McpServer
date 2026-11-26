using System.Net;
using Xunit;

namespace QBrainAi.Support.Mcp.TestContracts;

/// <summary>
/// Observations for Director's approved markerless registration contract.
/// </summary>
/// <remarks>2026-11-26: FR-MCP-009, TR-MCP-WS-004, FR-MCP-132 ac-1. Pin route,
/// credential and network scope and require both the workspace and its marker.</remarks>
public static class DefaultKeyRegistrationContract
{
    /// <summary>Checks the response and persisted side effects independently.</summary>
    public static void Verify(HttpStatusCode expectedStatus, bool expectRegistration, Observation actual)
    {
        Assert.Equal(expectedStatus, actual.Status);
        Assert.Equal(expectRegistration, actual.IsRegistered);
        Assert.Equal(expectRegistration, actual.HasMarker);
    }

    /// <summary>HTTP and storage observations from one registration request.</summary>
    public sealed record Observation(HttpStatusCode Status, bool IsRegistered, bool HasMarker);
}
